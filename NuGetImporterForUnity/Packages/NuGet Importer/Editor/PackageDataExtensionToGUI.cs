using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class that provides extend methods to display the package information as a GUI.</para>
    /// <para>パッケージの情報をGUIとして表示する拡張メソッドを提供するクラス。</para>
    /// </summary>
    public static class PackageDataExtensionToGUI
    {
        private static HttpClient _client = new HttpClient();
        private static readonly Dictionary<string, Task> Getting = new Dictionary<string, Task>();
        private static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>();
        private static readonly List<string> IconLog = new List<string>();

        private static readonly List<Task> TimeoutSet = new List<Task>();
        private static readonly Stack<TimeSpan> TimeoutStack = new Stack<TimeSpan>();

        /// <summary>
        /// <para>Delete icon cache.</para>
        /// <para>アイコンのキャッシュを削除する。</para>
        /// </summary>
        public static void DeleteCache()
        {
            lock (IconCache)
            {
                IconCache.Clear();
                IconLog.Clear();
                Getting.Clear();
            }
        }

        /// <summary>
        /// <para>Set Timeout.</para>
        /// <para>タイムアウト時間を再設定。</para>
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static async Task SetTimeout(TimeSpan timeout)
        {
            lock (TimeoutStack)
            {
                if (TimeoutStack.Any())
                {
                    TimeoutStack.Push(timeout);
                    return;
                }

                TimeoutStack.Push(timeout);
            }

            Task task = SetWebClientTasks();
            TimeoutSet.Add(task);
            await task;
            TimeoutSet.Clear();
        }

        private static async Task SetWebClientTasks()
        {
            await Task.WhenAll(Getting.Values.ToArray());
            _client.Dispose();
            _client = new HttpClient(
                new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                }
            );
            lock (TimeoutStack)
            {
                _client.Timeout = TimeoutStack.Pop();
                TimeoutStack.Clear();
            }
        }

        /// <summary>
        /// <para>Display the package information as a GUI.</para>
        /// <para>パッケージ情報をGUIとして表示する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>パッケージ情報。</para>
        /// </param>
        /// <param name="bold">
        /// <para>Bold GUIStyle.</para>
        /// <para>太字のGUIStyle。</para>
        /// </param>
        /// <param name="window">
        /// <para>Main window of NuGet importer.</para>
        /// <para>NuGet importerのメインウィンドウ。</para>
        /// </param>
        /// <param name="selected">
        /// <para>Whether the package is selected.</para>
        /// <para>選択されたパッケージかどうか。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether only stable.</para>
        /// <para>安定版のみか。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal static async Task ToGUI(
            this Datum data,
            GUIStyle bold,
            NuGetImporterWindow window,
            bool selected,
            bool onlyStable
        )
        {
            var tasks = new List<Task>();
            var sizeScale = window.position.width / 1920;
            if (onlyStable)
            {
                if (!data.versions.Any(version => !version.version.Contains('-') && version.version[0] != '0'))
                {
                    return;
                }
            }

            Color color = GUI.color;
            if (selected)
            {
                GUI.color = Color.cyan;
            }

            using (var scope = new EditorGUILayout.HorizontalScope(
                       "Box",
                       GUILayout.MinHeight(150),
                       GUILayout.ExpandWidth(true)
                   ))
            {
                GUI.color = color;
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (scope.rect.Contains(currentEvent.mousePosition))
                    {
                        tasks.Add(window.UpdateSelected(data));
                    }
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.MinHeight(150), GUILayout.Width(150 * sizeScale)))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(
                            new GUIContent(data.icon),
                            GUILayout.Width(128 * sizeScale),
                            GUILayout.Height(128 * sizeScale)
                        );
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        GUILayoutExtention.WrapedLabel(data.id, 24);
                    }

                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        GUILayout.Label("Author : ", bold);
                        GUILayoutExtention.WrapedLabel(string.Join(", ", data.authors));
                        GUILayout.Label("Download :", bold);
                        GUILayout.Label(data.totalDownloads.ToString());
                        GUILayout.FlexibleSpace();
                        IEnumerable<string> sortedVersions = data.GetAllVersion().AsEnumerable().Reverse();
                        var version = onlyStable
                            ? sortedVersions.First(ver => !ver.Contains('-') && ver[0] != '0')
                            : sortedVersions.First();
                        if (PackageManager.Installed != null && PackageManager.Installed.Package != null)
                        {
                            ICollection<Package> installed = PackageManager.Installed.Package
                                                                           .Where(package => package.ID == data.id)
                                                                           .ToArray();
                            if (installed.Any())
                            {
                                version = installed.First().Version;
                            }
                        }

                        GUILayout.Label("v" + version);
                    }

                    GUILayoutExtention.WrapedLabel(data.summary == "" ? data.description : data.summary);
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// <para>Get the icon for this package.</para>
        /// <para>このパッケージのアイコンを取得する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>パッケージ情報。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task GetIcon(this Datum data)
        {
            if (string.IsNullOrEmpty(data.iconUrl))
            {
                data.icon = null;
                return;
            }

            // The below code is the cache process.
            lock (IconCache)
            {
                var haveIcon = IconCache.ContainsKey(data.iconUrl);
                if (haveIcon)
                {
                    IconLog.Remove(data.iconUrl);
                    IconLog.Add(data.iconUrl);
                    data.icon = IconCache[data.iconUrl];
                    return;
                }
            }

            bool isGetting;
            lock (Getting)
            {
                isGetting = Getting.ContainsKey(data.iconUrl);
            }

            var isSavemode = NuGetImporterSettings.Instance.IsNetworkSavemode;
            if (isSavemode)
            {
                data.icon = null;
                return;
            }

            if (!isGetting)
            {
                lock (Getting)
                {
                    Getting.Add(data.iconUrl, GetIcon(data.iconUrl));
                }
            }

            await Getting[data.iconUrl];
            lock (Getting)
            {
                Getting.Remove(data.iconUrl);
            }

            data.icon = IconCache[data.iconUrl];
        }

        /// <summary>
        /// <para>Display the package information as a GUI.</para>
        /// <para>パッケージ情報をGUIとして表示する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>パッケージ情報。</para>
        /// </param>
        /// <param name="bold">
        /// <para>Bold GUIStyle.</para>
        /// <para>太字のGUIStyle。</para>
        /// </param>
        /// <param name="window">
        /// <para>Main window of NuGet importer.</para>
        /// <para>NuGet importerのメインウィンドウ。</para>
        /// </param>
        /// <param name="selected">
        /// <para>Whether the package is selected.</para>
        /// <para>選択されたパッケージかどうか。</para>
        /// </param>
        /// <param name="installedVersion">
        /// <para>Installed version.</para>
        /// <para>インストールされているバージョン。</para>
        /// </param>
        internal static void ToGUI(
            this Catalog data,
            GUIStyle bold,
            NuGetImporterWindow window,
            bool selected,
            string installedVersion
        )
        {
            var sizeScale = window.position.width / 1920;
            Color color = GUI.color;
            if (selected)
            {
                GUI.color = Color.cyan;
            }

            Catalogentry catalogEntry =
                data.GetAllCatalogEntry().Where(catalog => catalog.version == installedVersion).First();
            using (var scope = new EditorGUILayout.HorizontalScope(
                       "Box",
                       GUILayout.MinHeight(150),
                       GUILayout.ExpandWidth(true)
                   ))
            {
                GUI.color = color;
                Event currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (scope.rect.Contains(currentEvent.mousePosition))
                    {
                        window.UpdateSelected(data);
                    }
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.MinHeight(150), GUILayout.Width(150 * sizeScale)))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(
                            new GUIContent(data.icon),
                            GUILayout.Width(128 * sizeScale),
                            GUILayout.Height(128 * sizeScale)
                        );
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        GUILayoutExtention.WrapedLabel(catalogEntry.id, 24);
                    }

                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        GUILayout.Label("Author : ", bold);
                        GUILayout.Label(string.Join(", ", catalogEntry.authors));
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("v" + installedVersion);
                    }

                    GUILayoutExtention.WrapedLabel(
                        catalogEntry.summary == "" ? catalogEntry.description : catalogEntry.summary
                    );
                }
            }
        }

        /// <summary>
        /// <para>Display the package information details as a GUI.</para>
        /// <para>パッケージ情報の詳細をGUIとして表示する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>パッケージ情報。</para>
        /// </param>
        /// <param name="bold">
        /// <para>Bold GUIStyle.</para>
        /// <para>太字のGUIStyle。</para>
        /// </param>
        /// <param name="selectedVersion">
        /// <para>Selected version.</para>
        /// <para>選択されているバージョン。</para>
        /// </param>
        internal static void ToDetailGUI(this Catalog data, GUIStyle bold, string selectedVersion)
        {
            var catalogEntries =
                data.GetAllCatalogEntry().Where(catalog => catalog.version == selectedVersion).ToArray();
            if (!catalogEntries.Any())
            {
                return;
            }

            Catalogentry catalogEntry = catalogEntries.First();

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Description", bold);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayoutExtention.WrapedLabel(
                        catalogEntry.description == "" ? catalogEntry.summary : catalogEntry.description
                    );
                }
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Version : ", bold);
                    GUILayoutExtention.WrapedLabel(selectedVersion);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Auther :", bold);
                    GUILayoutExtention.WrapedLabel(string.Join(", ", catalogEntry.authors));
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("License : ", bold);
                    GUILayoutExtention.UrlLabel(
                        catalogEntry.licenseExpression == "" ? catalogEntry.licenseUrl : catalogEntry.licenseExpression,
                        catalogEntry.licenseUrl
                    );
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Publish date : ", bold);
                    GUILayoutExtention.WrapedLabel(catalogEntry.published);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Project url : ", bold);
                    GUILayoutExtention.UrlLabel(catalogEntry.projectUrl, catalogEntry.projectUrl);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Tag : ", bold);
                    GUILayoutExtention.WrapedLabel(string.Join(", ", catalogEntry.tags));
                    GUILayout.FlexibleSpace();
                }
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                GUILayout.Label("Dependency", bold);

                if (catalogEntry.dependencyGroups == null)
                {
                    GUILayout.Label("    None");
                }
                else
                {
                    List<string> framework = FrameworkName.TARGET;

                    var dependencyGroups = catalogEntry.dependencyGroups.Where(
                                                           group => string.IsNullOrEmpty(group.targetFramework)
                                                                    || framework.Contains(group.targetFramework)
                                                       )
                                                       .ToArray();
                    if (!dependencyGroups.Any())
                    {
                        GUILayout.Label("    None");
                    }
                    else
                    {
                        var dependencies = new List<Dependency>();
                        var targetFramework = framework.First();
                        var dependAllGroup = dependencyGroups.Where(
                                                                 depend => string.IsNullOrEmpty(depend.targetFramework)
                                                             )
                                                             .ToArray();
                        if (dependAllGroup.Any())
                        {
                            dependencies.AddRange(dependAllGroup.First().dependencies);
                        }

                        var dependGroups = dependencyGroups.Except(dependAllGroup)
                                                           .OrderBy(
                                                               group =>
                                                               {
                                                                   var ret = framework.IndexOf(group.targetFramework);
                                                                   return ret < 0 ? int.MaxValue : ret;
                                                               }
                                                           )
                                                           .ToList();

                        if (dependGroups.Any() && dependGroups.First().dependencies != null)
                        {
                            Dependencygroup dependGroup = dependGroups.First();
                            dependencies.AddRange(dependGroup.dependencies);
                            if (dependGroup.dependencies.Any())
                            {
                                targetFramework = dependGroup.targetFramework;
                            }
                        }

                        GUILayout.Label("    " + targetFramework, bold);
                        if (!dependencies.Any())
                        {
                            GUILayout.Label("        None");
                        }
                        else
                        {
                            try
                            {
                                foreach (Dependency dependency in dependencies)
                                {
                                    GUILayout.Label(
                                        "        "
                                        + dependency.id
                                        + "  ("
                                        + SemVer.ToMathExpression(dependency.range)
                                        + ")"
                                    );
                                }
                            }
                            catch (Exception)
                            {
                                // During execution, the number of dependencies changes and an exception occurs, so I grip it. (because it's not a problem.)
                            }
                        }
                    }
                }
            }

            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// <para>Displays an overview of the package information as a GUI.</para>
        /// <para>パッケージの概要をGUIとして表示する。</para>
        /// </summary>
        /// <param name="summary">
        /// <para>An overview of the package information.</para>
        /// <para>パッケージの概要。</para>
        /// </param>
        /// <param name="bold">
        /// <para>Bold GUIStyle.</para>
        /// <para>太字のGUIStyle。</para>
        /// </param>
        /// <param name="window">
        /// <para>Main window of NuGet importer.</para>
        /// <para>NuGet importerのメインウィンドウ。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether only stable.</para>
        /// <para>安定版のみか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal static async Task ToGUI(
            this PackageSummary summary,
            GUIStyle bold,
            NuGetImporterWindow window,
            bool isReady,
            bool onlyStable,
            VersionSelectMethod method
        )
        {
            var tasks = new List<Task>();
            var sizeScale = window.position.width / 1920;
            var isExist = PackageManager.ExistingPackage.Package.Any(package => package.ID == summary.PackageId);
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(150)))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(150 * sizeScale)))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(
                            new GUIContent(summary.Image),
                            GUILayout.Width(128 * sizeScale),
                            GUILayout.Height(128 * sizeScale)
                        );
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayoutExtention.WrapedLabel(summary.PackageId, 24);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("version", bold);
                        List<string> versions = onlyStable ? summary.StableVersion : summary.AllVersion;
                        var index = versions.Contains(summary.SelectedVersion)
                            ? versions.IndexOf(summary.SelectedVersion)
                            : 0;
                        summary.SelectedVersion = versions[EditorGUILayout.Popup(
                            index,
                            versions.ToArray(),
                            GUILayout.ExpandWidth(true)
                        )];
                        var isSameVersion = summary.SelectedVersion == summary.InstalledVersion;
                        var installText = summary.InstalledVersion == null ? "Install" :
                            isSameVersion ? "Repair" : "Change Version";
                        using (new EditorGUI.DisabledGroupScope(!isReady || isExist))
                        {
                            if (GUILayout.Button(installText, GUILayout.ExpandWidth(true)))
                            {
                                if (summary.InstalledVersion == null)
                                {
                                    tasks.Add(
                                        PackageOperation(
                                            PackageManager.InstallPackageAsync(
                                                summary.PackageId,
                                                summary.SelectedVersion,
                                                onlyStable,
                                                method
                                            ),
                                            window,
                                            summary.PackageId
                                        )
                                    );
                                }
                                else if (isSameVersion)
                                {
                                    tasks.Add(
                                        PackageOperation(
                                            PackageManager.FixPackageAsync(summary.PackageId, false),
                                            window,
                                            summary.PackageId
                                        )
                                    );
                                }
                                else
                                {
                                    tasks.Add(
                                        PackageOperation(
                                            PackageManager.ChangePackageVersionAsync(
                                                summary.PackageId,
                                                summary.SelectedVersion,
                                                onlyStable,
                                                method
                                            ),
                                            window,
                                            summary.PackageId
                                        )
                                    );
                                }
                            }

                            using (new EditorGUI.DisabledScope(!isSameVersion))
                            {
                                if (GUILayout.Button("Uninstall", GUILayout.ExpandWidth(true)))
                                {
                                    tasks.Add(
                                        PackageOperation(
                                            PackageManager.UninstallPackagesAsync(summary.PackageId, onlyStable),
                                            window,
                                            summary.PackageId
                                        )
                                    );
                                }
                            }
                        }
                    }

                    List<string> ignores = NuGetImporterSettings.Instance.IgnorePackages;
                    var isIgnore = ignores.Contains(summary.PackageId);
                    GUILayout.Space(2);
                    isIgnore = GUILayout.Toggle(isIgnore, "Mark as ignore package");
                    GUILayout.Space(EditorGUIUtility.singleLineHeight / 2);
                    if (isIgnore)
                    {
                        ignores.Add(summary.PackageId);
                    }
                    else
                    {
                        ignores.Remove(summary.PackageId);
                    }

                    NuGetImporterSettings.Instance.IgnorePackages = ignores.Distinct().ToList();

                    if (isExist || isIgnore)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label("This package exists out of control in this project.");
                        }
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        private static async Task PackageOperation(
            Task<OperationResult> operation,
            NuGetImporterWindow window,
            string packageId
        )
        {
            OperationResult result = await operation;
            EditorUtility.DisplayDialog("NuGet  importer", result.Message, "OK");
            await window.UpdateInstalledList();
            await window.UpdateSelected(packageId);
        }

        /// <summary>
        /// <para>Get the icon for this package.</para>
        /// <para>このパッケージのアイコンを取得する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>パッケージ情報。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task GetIcon(this Catalog data, string installedVersion)
        {
            Catalogentry d = data.GetAllCatalogEntry().First(catalog => catalog.version == installedVersion);

            if (d.iconUrl == null || d.iconUrl == "")
            {
                data.icon = null;
                return;
            }

            // The below code is the cache process.
            lock (IconCache)
            {
                var haveIcon = IconCache.ContainsKey(d.iconUrl);
                if (haveIcon)
                {
                    IconLog.Remove(d.iconUrl);
                    IconLog.Add(d.iconUrl);
                    data.icon = IconCache[d.iconUrl];
                    return;
                }
            }

            var isGetting = false;
            lock (Getting)
            {
                isGetting = Getting.ContainsKey(d.iconUrl);
            }

            if (!isGetting)
            {
                if (TimeoutSet.Any())
                {
                    await Task.WhenAll(TimeoutSet.ToArray());
                }

                lock (Getting)
                {
                    Getting.Add(d.iconUrl, GetIcon(d.iconUrl));
                }
            }

            await Getting[d.iconUrl];
            lock (Getting)
            {
                Getting.Remove(d.iconUrl);
            }

            data.icon = IconCache[d.iconUrl];
        }

        private static async Task GetIcon(string url)
        {
            var source = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            var tryCount = NuGetImporterSettings.Instance.RetryLimit + 1;
            for (var i = 0; i < tryCount; i++)
            {
                try
                {
                    var data = await _client.GetByteArrayAsync(url);
                    source.LoadImage(data);
                    break;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("404") && e.Message.ToLower().Contains("not found"))
                    {
                        UpdateIconCache(url, null);
                        break;
                    }

                    if (i >= tryCount - 1)
                    {
                        lock (Getting)
                        {
                            Getting.Clear();
                        }

                        throw;
                    }

                    await Task.Delay(1000);
                }
            }

            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Graphics.ConvertTexture(source, texture);
            Object.DestroyImmediate(source);
            UpdateIconCache(url, texture);
        }

        private static void UpdateIconCache(string url, Texture2D texture)
        {
            lock (IconCache)
            {
                IconCache[url] = texture;
                IconLog.Add(url);
                while (IconCache.Count > NuGetImporterSettings.Instance.IconCacheLimit && IconCache.Count > 0)
                {
                    var delete = IconLog[0];
                    IconLog.RemoveAt(0);
                    Object.DestroyImmediate(IconCache[delete]);
                    IconCache.Remove(delete);
                }
            }
        }
    }
}
