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
        private string _inputText = "";
        private int _selected;
        private int _hitPackages;
        private float _progress;
        private string _progressText = "";

        private Vector2 _packagesScroll = Vector2.zero;
        private Vector2 _detailScroll = Vector2.zero;

        private bool _isAddingPackages;
        private readonly object _lockAddingPackages = new object();

        private int Selected
        {
            get => _selected;
            set
            {
                if (_selected == value)
                {
                    return;
                }

                _selected = value;
                _ = UpdateData();
            }
        }

        private string InputText
        {
            get => _inputText;
            set
            {
                if (_inputText == value)
                {
                    return;
                }

                _inputText = value;
                _dataUpdateRequest.Push(DateTime.Now);
            }
        }

        private readonly string[] _selectedLabel = { "Search packages", "Installed packages" };

        private List<Catalog> _catalogs = new List<Catalog>();
        private readonly List<Datum> _searchPackages = new List<Datum>();
        private PackageSummary _summary;
        private Catalog _detail;
        private bool _isAddedSummary;
        private readonly Stack<DateTime> _dataUpdateRequest = new Stack<DateTime>();
        private readonly TimeSpan _throttleTime = TimeSpan.FromMilliseconds(500);

        private void OnEnable()
        {
            _progress = 0;
            if (_inputText == "" && _selected == 0)
            {
                _ = UpdateData();
            }
        }

        internal static void Initialize()
        {
            NuGetImporterWindow[] instance = Resources.FindObjectsOfTypeAll<NuGetImporterWindow>();
            if (instance.Length > 0)
            {
                _ = instance[0].UpdateData();
            }
        }

        [MenuItem("NuGet Importer/Manage packages", false, 0)]
        private static void ShowWindow()
        {
            var isAssets = NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsAssets;
            if (isAssets != File.Exists(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json")))
            {
                if (EditorUtility.DisplayDialog(
                        "NuGet importer",
                        "The installation method setting does not match the current installation method: UPM (recommended) or Assets ?",
                        "UPM",
                        "Assets"
                    ))
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
        private static async void FixPackages()
        {
            OperationResult result = await PackageManager.FixPackagesAsync(false);
            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }

        [MenuItem("NuGet Importer/Delete cache", false, 2)]
        private static void DeleteCache()
        {
            NuGet.DeleteCache();
            PackageDataExtensionToGUI.DeleteCache();
        }

        [MenuItem("NuGet Importer/Clean up this plugin", false, 3)]
        private static async void CleanUp()
        {
            if (!EditorUtility.DisplayDialog(
                    "NuGet importer",
                    "!!!!!!!!!!!!!!!!!!\n! WARNING !\n!!!!!!!!!!!!!!!!!!\nThis operation is for when this extension does not work.\n\nIf you execute this operation, NuGet importer will uninstall packages installed through this.",
                    "Clean up",
                    "Cancel"
                ))
            {
                return;
            }

            NuGet.DeleteCache();
            PackageDataExtensionToGUI.DeleteCache();
            OperationResult result = await PackageManager.CleanUp();
            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }

        [MenuItem("NuGet Importer/Check update", false, 5)]
        private static async void CheckUpdate()
        {
            var client = new HttpClient();
            HttpResponseMessage response =
                await client.GetAsync("https://github.com/kumaS-nu/NuGet-importer-for-Unity/releases");
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
            var matches = Regex.Matches(text, @"NuGetImporterForUnity\.(?<version>\d+\.\d+\.\d+)\.zip");
            var versions = (from Match match in matches select match.Groups["version"].Value).ToList();

            var latestVersion = SemVer.SortVersion(versions)[0];
            Version thisVersion = typeof(NuGetImporterWindow).Assembly.GetName().Version;
            EditorUtility.DisplayDialog(
                "NuGet importer",
                "Now version is "
                + thisVersion.Major
                + "."
                + thisVersion.Minor
                + "."
                + thisVersion.Revision
                + ".\n Lastest version is "
                + latestVersion,
                "OK"
            );
        }

        [MenuItem("NuGet Importer/Go to project page", false, 5)]
        private static void GoProjectPage()
        {
            Help.BrowseURL("https://github.com/kumaS-nu/NuGet-importer-for-Unity");
        }

        [SerializeField] private ApiCompatibilityLevel beforeAPI;

        private async void Update()
        {
            if (beforeAPI == default)
            {
                beforeAPI = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
            }

            if (beforeAPI != PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)
                && PackageManager.ControlledPackages.Installed.Any())
            {
                EditorUtility.DisplayDialog(
                    "NuGet  importer",
                    "You changed script backend. We change the package to fit the current script backend.",
                    "OK"
                );
                beforeAPI = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
                OperationResult result = await PackageManager.ReInstallAllPackages(
                    NuGetImporterSettings.Instance.OnlyStable,
                    NuGetImporterSettings.Instance.Method
                );
                EditorUtility.DisplayDialog("NuGet  importer", result.Message, "OK");
                await UpdateInstalledList();
                if (_summary != null && _summary.PackageId != null && _summary.PackageId != "")
                {
                    await UpdateSelected(_summary.PackageId);
                }
            }

            if (NuGetImporterSettings.Instance.IsNetworkSavemode)
            {
                if (!_dataUpdateRequest.Any() || DateTime.Now - _dataUpdateRequest.Peek() <= _throttleTime)
                {
                    return;
                }

                _dataUpdateRequest.Clear();
                _ = UpdateData();

                return;
            }

            if (!_dataUpdateRequest.Any())
            {
                return;
            }

            _dataUpdateRequest.Clear();
            _ = UpdateData();
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
            if (_selected == 0)
            {
                ICollection<Datum> pkg = _searchPackages.Where(package => package.id == packageId).ToList();
                if (pkg.Any())
                {
                    _progress = 0.25f;
                    _progressText = "Getting package deteal";
                    await UpdateSelected(pkg.First());
                }
                else
                {
                    _summary = null;
                    _detail = null;
                }
            }
            else
            {
                ICollection<Catalog> pkg =
                    _catalogs.Where(catalog => catalog.items[0].items[0].catalogEntry.id == packageId).ToList();
                if (pkg.Any())
                {
                    _progress = 0.25f;
                    _progressText = "Getting package deteal";
                    UpdateSelected(pkg.First());
                }
                else
                {
                    _summary = null;
                    _detail = null;
                }
            }

            _progress = 1;
            _progressText = "Finish";
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
            if (PackageManager.Installed != null && PackageManager.Installed.Package != null)
            {
                ICollection<Package> installed =
                    PackageManager.Installed.Package.Where(package => package.ID == data.id).ToList();
                if (installed.Any())
                {
                    installedVersion = installed.First().Version;
                }
            }

            if (_summary == null)
            {
                _isAddedSummary = true;
            }

            _summary = new PackageSummary(data, installedVersion);
            _detail = null;
            _detail = await NuGet.GetCatalog(data.id);
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
            if (_summary == null)
            {
                _isAddedSummary = true;
            }

            var package =
                PackageManager.Installed.Package.Where(package => package.ID == data.items[0].items[0].catalogEntry.id)
                              .ToArray();
            if (package.Any())
            {
                _summary = new PackageSummary(data, package.First().Version);
                _detail = data;
            }

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
            try
            {
                _progress = 0.25f;
                _progressText = "Getting package list";
                if (_selected == 1)
                {
                    Task tasks = UpdateInstalledList();
                    _progress = 0.5f;
                    _progressText = "Organizing datas";

                    // Asynchronous is only fetching images, so the catalog are all available. So, we can filter the catalog here.
                    _catalogs = _catalogs.Where(
                                             catalog => catalog.items[0]
                                                               .items[0]
                                                               .catalogEntry.id.IndexOf(
                                                                   _inputText,
                                                                   StringComparison.CurrentCultureIgnoreCase
                                                               )
                                                        >= 0
                                         )
                                         .ToList();
                    _searchPackages.Clear();
                    Repaint();
                    _progress = 0.75f;
                    _progressText = "Getting icons";
                    await Task.WhenAll(tasks);
                    _progress = 1;
                    _progressText = "Finish";
                    Repaint();
                }
                else
                {
                    var searchQuery = _inputText;
                    SearchResult searchResult = await NuGet.SearchPackage(_inputText, prerelease: true);
                    _progress = 0.5f;
                    _progressText = "Getting icons";

                    var tasks = new List<Task>();
                    foreach (Datum search in searchResult.data)
                    {
                        tasks.Add(search.GetIcon());
                    }

                    lock (_searchPackages)
                    {
                        _hitPackages = searchResult.totalHits;
                        _searchPackages.Clear();
                    }

                    if (_selected == 0)
                    {
                        if (searchQuery == _inputText)
                        {
                            _progress = 0.75f;
                            _progressText = "Organizing datas";
                            lock (_searchPackages)
                            {
                                _searchPackages.AddRange(searchResult.data);
                            }
                        }
                        else
                        {
                            return;
                        }

                        _catalogs.Clear();
                    }

                    Repaint();
                    if (_selected == 0 && searchQuery == _inputText)
                    {
                        await Task.WhenAll(tasks);
                        _progress = 1;
                        _progressText = "Finish";
                    }

                    Repaint();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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
            var tasks = new List<Task>();
            lock (PackageManager.installedCatalog)
            {
                lock (_catalogs)
                {
                    _catalogs.Clear();

                    foreach (KeyValuePair<string, Catalog> catalog in PackageManager.installedCatalog)
                    {
                        _catalogs.Add(catalog.Value);
                        var package = PackageManager.Installed.Package.Where(
                                                        package => package.ID
                                                                   == catalog.Value.items[0].items[0].catalogEntry.id
                                                    )
                                                    .ToList();
                        if (package.Any())
                        {
                            tasks.Add(catalog.Value.GetIcon(package.First().Version));
                        }
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
            Selected = GUILayout.Toolbar(Selected, _selectedLabel);
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                var beforeState = NuGetImporterSettings.Instance.OnlyStable;
                NuGetImporterSettings.Instance.OnlyStable = !GUILayout.Toggle(
                    !NuGetImporterSettings.Instance.OnlyStable,
                    "Include development versions"
                );
                if (beforeState != NuGetImporterSettings.Instance.OnlyStable)
                {
                    _ = UpdateData();
                    GUIUtility.ExitGUI();
                }
            }

            GUILayoutExtention.WrapedLabel("Search", 18);
            Rect progressRect = GUILayoutUtility.GetLastRect();
            if (_progress < 1 && _progress > 0)
            {
                progressRect.x = 100;
                progressRect.width = position.width - progressRect.x - 20;
                progressRect.height = 21;
                EditorGUI.ProgressBar(progressRect, _progress, _progressText);
            }

            var searchStyle = new GUIStyle(EditorStyles.textField) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent icon = EditorGUIUtility.IconContent("Search Icon");
                GUILayout.Box(icon, GUILayout.Width(30), GUILayout.Height(30));
                InputText = EditorGUILayout.TextField(
                    InputText,
                    searchStyle,
                    GUILayout.Height(30),
                    GUILayout.ExpandWidth(true)
                );
            }

            var sumHeight = float.MaxValue;
            using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (var packagesScrollView = new GUILayout.ScrollViewScope(
                           _packagesScroll,
                           false,
                           true,
                           GUILayout.ExpandHeight(true),
                           GUILayout.ExpandWidth(true),
                           GUILayout.Width(position.width / 2),
                           GUILayout.MinWidth(position.width / 2)
                       ))
                {
                    _packagesScroll = packagesScrollView.scrollPosition;
                    if (_selected == 0)
                    {
                        tasks.AddRange(
                            _searchPackages.Select(
                                search => search.ToGUI(
                                    bold,
                                    this,
                                    _summary != null && _summary.PackageId == search.id,
                                    NuGetImporterSettings.Instance.OnlyStable
                                )
                            )
                        );

                        IEnumerable<Datum> isShown = _searchPackages.Where(
                            search => !NuGetImporterSettings.Instance.OnlyStable
                                      || search.versions.Any(
                                          version => !version.version.Contains('-') && version.version[0] != '0'
                                      )
                        );
                        if (isShown.Any())
                        {
                            sumHeight = GUILayoutUtility.GetLastRect().yMax;
                        }
                    }
                    else
                    {
                        if (PackageManager.Installed.Package != null)
                        {
                            foreach (Catalog catalog in _catalogs)
                            {
                                var id = catalog.items[0].items[0].catalogEntry.id;
                                ICollection<Package> installedPkg =
                                    PackageManager.Installed.Package.Where(package => package.ID == id).ToArray();
                                if (installedPkg.Count != 1)
                                {
                                    GUIUtility.ExitGUI();
                                }

                                catalog.ToGUI(
                                    bold,
                                    this,
                                    _summary != null && _summary.PackageId == id,
                                    installedPkg.First().Version
                                );
                            }
                        }
                    }
                }

                var scrollHeight = GUILayoutUtility.GetLastRect().height;
                if (sumHeight - scrollHeight <= _packagesScroll.y && sumHeight != 1 && scrollHeight != 1)
                {
                    _ = SearchAditionalPackage();
                }

                using (var detailScrollView = new GUILayout.ScrollViewScope(
                           _detailScroll,
                           false,
                           true,
                           GUILayout.ExpandHeight(true),
                           GUILayout.ExpandWidth(true),
                           GUILayout.Width(position.width / 2),
                           GUILayout.MaxWidth(position.width / 2)
                       ))
                {
                    _detailScroll = detailScrollView.scrollPosition;
                    if (_isAddedSummary)
                    {
                        _isAddedSummary = false;
                        GUIUtility.ExitGUI();
                    }

                    if (_summary != null)
                    {
                        tasks.Add(
                            _summary.ToGUI(
                                bold,
                                this,
                                _detail != null,
                                NuGetImporterSettings.Instance.OnlyStable,
                                NuGetImporterSettings.Instance.Method
                            )
                        );

                        _detail?.ToDetailGUI(bold, _summary.SelectedVersion);
                    }
                }
            }
        }

        private async Task SearchAditionalPackage()
        {
            lock (_lockAddingPackages)
            {
                if (_isAddingPackages)
                {
                    return;
                }

                _isAddingPackages = true;
            }

            int nowPackages;
            lock (_searchPackages)
            {
                nowPackages = _searchPackages.Count;
            }

            if (nowPackages >= _hitPackages)
            {
                lock (_lockAddingPackages)
                {
                    _isAddingPackages = false;
                }

                return;
            }

            _progress = 0.25f;
            _progressText = "Getting package list";
            var searchQuery = _inputText;
            SearchResult searchResult = await NuGet.SearchPackage(_inputText, nowPackages, prerelease: true);
            _progress = 0.5f;
            _progressText = "Getting icons";

            var tasks = new List<Task>();
            foreach (Datum search in searchResult.data)
            {
                tasks.Add(search.GetIcon());
            }

            lock (_searchPackages)
            {
                _hitPackages = searchResult.totalHits;
            }

            await Task.WhenAll(tasks);
            if (searchQuery == _inputText && _selected == 0 && nowPackages == _searchPackages.Count)
            {
                _progress = 0.75f;
                _progressText = "Organizing datas";
                lock (_searchPackages)
                {
                    _searchPackages.AddRange(searchResult.data);
                }
            }

            lock (_lockAddingPackages)
            {
                _isAddingPackages = false;
            }

            Repaint();
            if (_selected == 0 && searchQuery == _inputText)
            {
                _progress = 1;
                _progressText = "Finish";
            }
        }

        private static async Task Operate(Task<OperationResult> operation)
        {
            OperationResult result = await operation;
            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }
    }
}
