#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Main window of NuGet importer.</para>
    /// <para>NuGet importerのメインウィンドウ。</para>
    /// </summary>
    public class NuGetImporterWindow : EditorWindow
    {
        private string inputText = "";
        private int selected = 0;
        private int hitPackages = 0;
        private float progress = 0;
        private string progressText = "";

        private Vector2 packagesScroll = Vector2.zero;
        private Vector2 detealScroll = Vector2.zero;

        private bool isAddingPackages = false;
        private readonly object lockAddingPackages = new object();

        private int Selected { get => selected; set { if (selected != value) { selected = value; _ = UpdateData(); } } }
        private string InputText { get => inputText; set { if (inputText != value) { inputText = value; _ = UpdateData(); } } }

        private readonly string[] selectedLabel = { "Search packages", "Installed packages" };

        private List<Catalog> catalogs = new List<Catalog>();
        private readonly List<Datum> searchPackages = new List<Datum>();
        private PackageSummary summary;
        private Catalog deteal;
        private bool isAddedSummary = false;

        private void OnEnable()
        {
            progress = 0;
            if (inputText == "" && selected == 0)
            {
                _ = UpdateData();
            }
        }

        [MenuItem("NuGet Importer/Manage packages", false, 0)]
        private static void ShowWindow()
        {
            var isAssets = NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsAssets;
            if (isAssets != File.Exists(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json")))
            {
                if (EditorUtility.DisplayDialog("NuGet importer", "The installation method setting does not match the current installation method: UPM (recommended) or Assets ?", "UPM", "Assets"))
                {
                    NuGetImporterSettings.Instance.InstallMethod = InstallMethod.AsUPM;
                    _ = Operate(PackageManager.ConvertToUPM());
                }
                else
                {
                    NuGetImporterSettings.Instance.InstallMethod = InstallMethod.AsAssets;
                    _ = Operate(PackageManager.ConvertToAssets());
                }
                return;
            }

            NuGetImporterWindow window = GetWindow<NuGetImporterWindow>("NuGet importer");
            if (window.position.width < 1175 || window.position.height < 450)
            {
                window.position = new Rect(0, 0, 1175, 450);
            }
            window.minSize = new Vector2(1175, 450);
        }

        [MenuItem("NuGet Importer/Repair packages", false, 1)]
        private static async Task FixPackages()
        {
            try
            {
                await PackageManager.FixPackage(NuGetImporterSettings.Instance.OnlyStable, NuGetImporterSettings.Instance.Method);
                EditorUtility.DisplayDialog("NuGet importer", "Packages repair are complete.", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("NuGet importer", "An error occured!\n\n" + e.Message, "OK");
            }
        }

        [MenuItem("NuGet Importer/Delete cache", false, 2)]
        private static void DeleteCache()
        {
            NuGet.DeleteCache();
            PackageDataExtentionToGUI.DeleteCache();
        }

        [MenuItem("NuGet Importer/Check update", false, 4)]
        private static async Task CheckUpdate()
        {
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases");
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                EditorUtility.DisplayDialog("NuGet importer", "An error occer when getting release page.", "OK");
                return;
            }
            var text = await response.Content.ReadAsStringAsync();
            MatchCollection matchs = Regex.Matches(text, @"NuGetImporterForUnity\.(?<version>\d+\.\d+\.\d+)\.zip");
            var versions = new List<string>();
            foreach (Match match in matchs)
            {
                versions.Add(match.Groups["version"].Value);
            }

            var lastestVersion = SemVer.SortVersion(versions)[0];
            Version thisVersion = typeof(NuGetImporterWindow).Assembly.GetName().Version;
            EditorUtility.DisplayDialog("NuGet importer", "Now version is " + thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Revision + ".\n Lastest version is " + lastestVersion, "OK");
        }


        [MenuItem("NuGet Importer/Go to project page", false, 5)]
        private static void GoProjectPage()
        {
            Help.BrowseURL("https://github.com/kumaS-nu/NuGet-importer-for-Unity");
        }

        /// <summary>
        /// <para>Update selected package infomation.</para>
        /// <para>選択されたパッケージ情報（右側の画面のやつ）を更新する。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Selected package id.</para>
        /// <para>選択されたパッケージのid。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal async Task UpdateSelected(string packageId)
        {
            if (selected == 0)
            {
                IEnumerable<Datum> pkg = searchPackages.Where(package => package.id == packageId);
                if (pkg != null && pkg.Any())
                {
                    progress = 0.25f;
                    progressText = "Getting package deteal";
                    await UpdateSelected(pkg.First());
                }
                else
                {
                    summary = null;
                    deteal = null;
                }
            }
            else
            {
                IEnumerable<Catalog> pkg = catalogs.Where(catalog => catalog.items[0].items[0].catalogEntry.id == packageId);
                if (pkg != null && pkg.Any())
                {
                    progress = 0.25f;
                    progressText = "Getting package deteal";
                    UpdateSelected(pkg.First());
                }
                else
                {
                    summary = null;
                    deteal = null;
                }
            }

            progress = 1;
            progressText = "Finish";
            Repaint();
        }

        /// <summary>
        /// <para>Update selected package infomation.</para>
        /// <para>選択されたパッケージ情報（右側の画面のやつ）を更新する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Selected package infomation.</para>
        /// <para>選択されたパッケージの情報。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal async Task UpdateSelected(Datum data)
        {
            string installedVersion = null;
            if (PackageManager.Installed != null && PackageManager.Installed.package != null)
            {
                IEnumerable<Package> installed = PackageManager.Installed.package.Where(package => package.id == data.id);
                if (installed != null && installed.Any())
                {
                    installedVersion = installed.First().version;
                }
            }
            if (summary == null)
            {
                isAddedSummary = true;
            }
            summary = new PackageSummary(data, installedVersion);
            deteal = await NuGet.GetCatalog(data.id);
            Repaint();
        }

        /// <summary>
        /// <para>Update selected package infomation.</para>
        /// <para>選択されたパッケージ情報（右側の画面のやつ）を更新する。</para>
        /// </summary>
        /// <param name="data">
        /// <para>Selected package catalog.</para>
        /// <para>選択されたパッケージのカタログ。</para>
        /// </param>
        internal void UpdateSelected(Catalog data)
        {
            if (summary == null)
            {
                isAddedSummary = true;
            }
            summary = new PackageSummary(data, PackageManager.Installed.package.First(package => package.id == data.items[0].items[0].catalogEntry.id).version);
            deteal = data;
            Repaint();
        }

        /// <summary>
        /// <para>Update window infomation.</para>
        /// <para>画面の情報を更新する。</para>
        /// </summary>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal async Task UpdateData()
        {
            progress = 0.25f;
            progressText = "Getting package list";
            if (selected == 1)
            {
                Task tasks = UpdateInstalledList();
                progress = 0.5f;
                progressText = "Organizing datas";

                // Asynchronous is only fetching images, so the catalog are all available. So, we can filter the catalog here.
                catalogs = catalogs.Where(catalog => catalog.items[0].items[0].catalogEntry.id.IndexOf(inputText, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
                searchPackages.Clear();
                Repaint();
                progress = 0.75f;
                progressText = "Getting icons";
                await Task.WhenAll(tasks);
                progress = 1;
                progressText = "Finish";
                Repaint();
            }
            else
            {
                var searchQuery = inputText;
                SearchResult searchResult = await NuGet.SearchPackage(inputText, prerelease: true);
                progress = 0.5f;
                progressText = "Getting icons";

                var tasks = new List<Task>();
                foreach (Datum search in searchResult.data)
                {
                    tasks.Add(search.GetIcon());
                }

                lock (searchPackages)
                {
                    hitPackages = searchResult.totalHits;
                    searchPackages.Clear();
                }

                if (selected == 0)
                {
                    if (searchQuery == inputText)
                    {
                        progress = 0.75f;
                        progressText = "Organizing datas";
                        lock (searchPackages)
                        {
                            searchPackages.AddRange(searchResult.data);
                        }
                    }
                    else
                    {
                        return;
                    }
                    catalogs.Clear();
                }
                Repaint();
                if (selected == 0 && searchQuery == inputText)
                {
                    await Task.WhenAll(tasks);
                    progress = 1;
                    progressText = "Finish";
                }
                Repaint();
            }
        }

        /// <summary>
        /// <para>Update the information of installed packages in the Window.</para>
        /// <para>ウィンドウのインストールされたパッケージの情報を更新する。</para>
        /// </summary>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        internal async Task UpdateInstalledList()
        {
            catalogs.Clear();
            var tasks = new List<Task>();
            lock (PackageManager.installedCatalog)
            {
                lock (catalogs)
                {
                    foreach (KeyValuePair<string, Catalog> catalog in PackageManager.installedCatalog)
                    {
                        catalogs.Add(catalog.Value);
                        tasks.Add(catalog.Value.GetIcon(PackageManager.installed.package.First(package => package.id == catalog.Value.items[0].items[0].catalogEntry.id).version));
                    }
                }
            }
            await Task.WhenAll(tasks);
            Repaint();
        }

        /// <summary>
        /// <para>Draw the contents of the Window.</para>
        /// <para>ウィンドウの中身を描写。</para>
        /// </summary>
        public void OnGUI()
        {
            var bold = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            var tasks = new List<Task>();
            Selected = GUILayout.Toolbar(Selected, selectedLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Method to select a version : ");
                    NuGetImporterSettings.Instance.Method = (VersionSelectMethod)EditorGUILayout.EnumPopup(NuGetImporterSettings.Instance.Method);
                    GUILayout.FlexibleSpace();
                }
                var beforeSate = NuGetImporterSettings.Instance.OnlyStable;
                NuGetImporterSettings.Instance.OnlyStable = !GUILayout.Toggle(!NuGetImporterSettings.Instance.OnlyStable, "Include development versions");
                if (beforeSate != NuGetImporterSettings.Instance.OnlyStable)
                {
                    _ = UpdateData();
                    GUIUtility.ExitGUI();
                }
            }
            GUILayoutExtention.WrapedLabel("Search", 18);
            Rect progressRect = GUILayoutUtility.GetLastRect();
            if (progress < 1 && progress > 0)
            {
                progressRect.x = 100;
                progressRect.width = position.width - progressRect.x - 20;
                progressRect.y = 70;
                progressRect.height = 21;
                EditorGUI.ProgressBar(progressRect, progress, progressText);
            }
            var searchStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 24
            };
            InputText = EditorGUILayout.TextField(InputText, searchStyle, GUILayout.Height(30));
            var sumHeight = float.MaxValue;
            using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (var packagesScrollView = new GUILayout.ScrollViewScope(packagesScroll, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.Width(position.width / 2), GUILayout.MinWidth(position.width / 2)))
                {
                    packagesScroll = packagesScrollView.scrollPosition;
                    if (selected == 0)
                    {
                        foreach (Datum search in searchPackages)
                        {
                            tasks.Add(search.ToGUI(bold, this, summary != null && summary.PackageId == search.id, NuGetImporterSettings.Instance.OnlyStable));
                        }
                        if (searchPackages.Count > 0)
                        {
                            sumHeight = GUILayoutUtility.GetLastRect().yMax;
                        }
                    }
                    else
                    {
                        foreach (Catalog catalog in catalogs)
                        {
                            var id = catalog.items[0].items[0].catalogEntry.id;
                            catalog.ToGUI(bold, this, summary != null && summary.PackageId == id, PackageManager.Installed.package.Where(package => package.id == id).First().version);
                        }
                    }
                }
                var scrollHeight = GUILayoutUtility.GetLastRect().height;
                if (sumHeight - scrollHeight <= packagesScroll.y && sumHeight != 1 && scrollHeight != 1)
                {
                    _ = SearchAditionalPackage();
                }

                using (var detealScrollView = new GUILayout.ScrollViewScope(detealScroll, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.Width(position.width / 2), GUILayout.MaxWidth(position.width / 2)))
                {
                    detealScroll = detealScrollView.scrollPosition;
                    if (isAddedSummary)
                    {
                        isAddedSummary = false;
                        GUIUtility.ExitGUI();
                    }
                    if (summary != null)
                    {
                        tasks.Add(summary.ToGUI(bold, this, NuGetImporterSettings.Instance.OnlyStable, NuGetImporterSettings.Instance.Method));

                        if (deteal != null)
                        {
                            deteal.ToDetailGUI(bold, summary.SelectedVersion);
                        }
                    }
                }
            }
        }

        private async Task SearchAditionalPackage()
        {
            lock (lockAddingPackages)
            {
                if (isAddingPackages)
                {
                    return;
                }
                isAddingPackages = true;
            }
            var nowPackages = 0;
            lock (searchPackages)
            {
                nowPackages = searchPackages.Count;
            }
            if (nowPackages >= hitPackages)
            {
                lock (lockAddingPackages)
                {
                    isAddingPackages = false;
                }
                return;
            }
            progress = 0.25f;
            progressText = "Getting package list";
            var searchQuery = inputText;
            SearchResult searchResult = await NuGet.SearchPackage(inputText, nowPackages, prerelease: true);
            progress = 0.5f;
            progressText = "Getting icons";

            var tasks = new List<Task>();
            foreach (Datum search in searchResult.data)
            {
                tasks.Add(search.GetIcon());
            }
            lock (searchPackages)
            {
                hitPackages = searchResult.totalHits;
            }

            await Task.WhenAll(tasks);
            if (searchQuery == inputText && selected == 0 && nowPackages == searchPackages.Count)
            {
                progress = 0.75f;
                progressText = "Organizing datas";
                lock (searchPackages)
                {
                    searchPackages.AddRange(searchResult.data);
                }
            }
            lock (lockAddingPackages)
            {
                isAddingPackages = false;
            }
            Repaint();
            if (selected == 0 && searchQuery == inputText)
            {
                progress = 1;
                progressText = "Finish";
            }
        }

        private static async Task Operate(Task<bool> operation)
        {
            try
            {
                await operation;
                EditorUtility.DisplayDialog("NuGet importer", "Conversion is finished.", "OK");
            }
            catch (InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("NuGet importer", e.Message, "OK");
            }
            catch (ArgumentException e)
            {
                EditorUtility.DisplayDialog("NuGet importer", e.Message, "OK");
            }
        }
    }
}

#endif