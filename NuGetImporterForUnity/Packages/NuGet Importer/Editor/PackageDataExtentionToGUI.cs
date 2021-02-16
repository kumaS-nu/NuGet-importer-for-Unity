#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class that provides extend methods to display the package information as a GUI.</para>
    /// <para>パッケージの情報をGUIとして表示する拡張メソッドを提供するクラス。</para>
    /// </summary>
    public static class PackageDataExtentionToGUI
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Dictionary<string, Task> getting = new Dictionary<string, Task>();
        private static readonly Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
        private static readonly List<string> iconLog = new List<string>();

        /// <value>
        /// <para>Limit of icon cache.</para>
        /// <para>アイコンのキャッシュの最大数。</para>
        /// </value>
        public static int iconCacheLimit = 500;

        /// <summary>
        /// <para>Delete icon cache.</para>
        /// <para>アイコンのキャッシュを削除する。</para>
        /// </summary>
        public static void DeleteCache()
        {
            lock (iconCache)
            {
                iconCache.Clear();
                iconLog.Clear();
                getting.Clear();
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
        internal static async Task ToGUI(this Datum data, GUIStyle bold, NuGetImporterWindow window, bool selected, bool onlyStable)
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

            using (var scope = new EditorGUILayout.HorizontalScope("Box", GUILayout.MinHeight(150), GUILayout.ExpandWidth(true)))
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
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(128 * sizeScale)))
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(128 * sizeScale)))
                        {
                            Rect rect = GUILayoutUtility.GetRect(128 * sizeScale, 128 * sizeScale);
                            if (data.icon != null)
                            {
                                EditorGUI.DrawPreviewTexture(rect, data.icon);
                            }
                        }
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
                        GUILayout.Label(string.Join(", ", data.authors));
                        GUILayout.Label("Download :", bold);
                        GUILayout.Label(data.totalDownloads.ToString());
                        GUILayout.FlexibleSpace();
                        IEnumerable<string> sortedVersions = data.GetAllVersion().AsEnumerable().Reverse();
                        var version = onlyStable ? sortedVersions.First(ver => !ver.Contains('-') && ver[0] != '0') : sortedVersions.First();
                        if (PackageManager.Installed != null && PackageManager.Installed.package != null)
                        {
                            IEnumerable<Package> installed = PackageManager.Installed.package.Where(package => package.id == data.id);
                            if (installed != null && installed.Any())
                            {
                                version = installed.First().version;
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
            if (data.iconUrl == null)
            {
                data.icon = null;
                return;
            }

            var haveIcon = false;
            var isGetting = false;
            lock (iconCache)
            {
                haveIcon = iconCache.ContainsKey(data.iconUrl);
            }
            lock (getting)
            {
                isGetting = getting.ContainsKey(data.iconUrl);
            }

            if (!haveIcon && !isGetting)
            {
                lock (getting)
                {
                    getting.Add(data.iconUrl, GetIcon(data.iconUrl));
                }
            }

            if (!haveIcon)
            {
                await getting[data.iconUrl];
            }
            else
            {
                lock (iconCache)
                {
                    iconLog.Remove(data.iconUrl);
                    iconLog.Add(data.iconUrl);
                }
            }

            data.icon = iconCache[data.iconUrl];
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
        internal static void ToGUI(this Catalog data, GUIStyle bold, NuGetImporterWindow window, bool selected, string installedVersion)
        {
            var sizeScale = window.position.width / 1920;
            Color color = GUI.color;
            if (selected)
            {
                GUI.color = Color.cyan;
            }
            Catalogentry catalogEntry = data.GetAllCatalogEntry().Where(catalog => catalog.version == installedVersion).First();
            using (var scope = new EditorGUILayout.HorizontalScope("Box", GUILayout.MinHeight(150), GUILayout.ExpandWidth(true)))
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
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(128 * sizeScale)))
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(128 * sizeScale)))
                        {
                            Rect rect = GUILayoutUtility.GetRect(128 * sizeScale, 128 * sizeScale);
                            if (catalogEntry.icon != null)
                            {
                                EditorGUI.DrawPreviewTexture(rect, catalogEntry.icon);
                            }
                        }
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
                    GUILayoutExtention.WrapedLabel(catalogEntry.summary == "" ? catalogEntry.description : catalogEntry.summary);
                }
            }
        }

        /// <summary>
        /// <para>Display the package information details as a GUI.</para>
        /// <para>パッケージ情報の詳細をGUIとして表示する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Package infomation.</para>
        /// <para>ッケージ情報。</para>
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
            Catalogentry catalogEntry = data.GetAllCatalogEntry().Where(catalog => catalog.version == selectedVersion).First();

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Description", bold);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayoutExtention.WrapedLabel(catalogEntry.description == "" ? catalogEntry.summary : catalogEntry.description);
                }
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Version : ", bold);
                    GUILayout.Label(selectedVersion);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Auther :", bold);
                    GUILayout.Label(string.Join(", ", catalogEntry.authors));
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("License : ", bold);
                    GUILayoutExtention.UrlLabel(catalogEntry.licenseExpression == "" ? catalogEntry.licenseUrl : catalogEntry.licenseExpression, catalogEntry.licenseUrl);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Publish date : ", bold);
                    GUILayout.Label(catalogEntry.published);
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
                    GUILayout.Label(string.Join(", ", catalogEntry.tags));
                    GUILayout.FlexibleSpace();
                }
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                GUILayout.Label("Dependency", bold);

                var framework = new List<string>();
                switch (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
                {
                    case ApiCompatibilityLevel.NET_4_6:
                        framework = FrameworkName.NET;
                        break;
                    case ApiCompatibilityLevel.NET_Standard_2_0:
                        framework = FrameworkName.STANDARD;
                        break;
                }

                IEnumerable<Dependencygroup> dependencyGroups = catalogEntry.dependencyGroups.Where(group => framework.Contains(group.targetFramework));
                if (dependencyGroups == null || !dependencyGroups.Any())
                {
                    GUILayout.Label("    None");
                }
                else
                {
                    Dependencygroup dependencyGroup = dependencyGroups.OrderBy(group => framework.IndexOf(group.targetFramework)).First();
                    GUILayout.Label("    " + dependencyGroup.targetFramework, bold);
                    if (dependencyGroup.dependencies == null || dependencyGroup.dependencies.Length == 0)
                    {
                        GUILayout.Label("        None");
                    }
                    else
                    {
                        foreach (Dependency dependency in dependencyGroup.dependencies)
                        {
                            GUILayout.Label("        " + dependency.id + "  (" + SemVer.ToMathExpression(dependency.range) + ")");
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
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal static async Task ToGUI(this PackageSummary summary, GUIStyle bold, NuGetImporterWindow window, bool onlyStable)
        {
            var tasks = new List<Task>();
            var sizeScale = window.position.width / 1920;
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(150)))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(150 * sizeScale)))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(128 * sizeScale)))
                    {
                        GUILayout.FlexibleSpace();
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(128 * sizeScale)))
                        {
                            Rect rect = GUILayoutUtility.GetRect(128 * sizeScale, 128 * sizeScale);
                            if (summary.Image != null)
                            {
                                EditorGUI.DrawPreviewTexture(rect, summary.Image);
                            }
                        }
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
                        var index = versions.Contains(summary.SelectedVersion) ? versions.IndexOf(summary.SelectedVersion) : 0;
                        summary.SelectedVersion = versions[EditorGUILayout.Popup(index, versions.ToArray(), GUILayout.ExpandWidth(true))];
                        var isSameVersion = summary.SelectedVersion == summary.InstalledVersion;
                        var installText = summary.InstalledVersion == null ? "Install" : isSameVersion ? "Repair" : "Change Version";
                        if (GUILayout.Button(installText, GUILayout.ExpandWidth(true)))
                        {
                            if (summary.InstalledVersion == null)
                            {
                                tasks.Add(PackageOperation(PackageManager.InstallPackage(summary.PackageId, summary.SelectedVersion, onlyStable), window, summary.PackageId, "Installation finished."));
                            }
                            else if (isSameVersion)
                            {
                                tasks.Add(PackageOperation(PackageManager.FixPackage(summary.PackageId), window, summary.PackageId, "The repair finished."));
                            }
                            else
                            {
                                tasks.Add(PackageOperation(PackageManager.ChangePackageVersion(summary.PackageId, summary.SelectedVersion, onlyStable), window, summary.PackageId, "Version change finished."));
                            }
                        }

                        using (new EditorGUI.DisabledScope(!isSameVersion))
                        {
                            if (GUILayout.Button("Uninstall", GUILayout.ExpandWidth(true)))
                            {
                                tasks.Add(PackageOperation(PackageManager.UninstallPackages(summary.PackageId, onlyStable), window, summary.PackageId, "Uninstallation finished."));
                            }
                        }
                    }
                }
            }
            await Task.WhenAll(tasks);
        }

        private static async Task PackageOperation(Task operation, NuGetImporterWindow window, string packageId, string message)
        {
            try
            {
                await operation;
                EditorUtility.DisplayDialog("NuGet importer", message, "OK");
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("NuGet importer", e.Message, "OK");
            }
            catch (ArgumentException e)
            {
                EditorUtility.DisplayDialog("NuGet importer", e.Message, "OK");
            }
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
        public static async Task GetIcon(this Catalog data)
        {
            foreach (Catalogentry d in data.GetAllCatalogEntry())
            {
                if (d.iconUrl == null || d.iconUrl == "")
                {
                    d.icon = null;
                    continue;
                }
                var haveIcon = false;
                var isGetting = false;
                lock (iconCache)
                {
                    haveIcon = iconCache.ContainsKey(d.iconUrl);
                }
                lock (getting)
                {
                    isGetting = getting.ContainsKey(d.iconUrl);
                }
                if (!haveIcon && !isGetting)
                {
                    lock (getting)
                    {
                        getting.Add(d.iconUrl, GetIcon(d.iconUrl));
                    }
                }

                if (!haveIcon)
                {
                    await getting[d.iconUrl];
                }
                else
                {
                    lock (iconCache)
                    {
                        iconLog.Remove(d.iconUrl);
                        iconLog.Add(d.iconUrl);
                    }
                }

                d.icon = iconCache[d.iconUrl];
            }
        }

        private static async Task GetIcon(string url)
        {
            var texture = new Texture2D(128, 128);
            texture.LoadImage(await client.GetByteArrayAsync(url));
            lock (iconCache)
            {
                iconCache[url] = texture;
                iconLog.Add(url);
                while (iconCache.Count > iconCacheLimit && iconCache.Count > 0)
                {
                    var delete = iconLog[0];
                    iconLog.RemoveAt(0);
                    iconCache.Remove(delete);
                }
            }
        }
    }
}

#endif