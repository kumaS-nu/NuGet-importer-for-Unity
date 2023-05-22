using System.IO;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>EditorWindow class to configure settings for NuGet importer.</para>
    /// <para>NuGet importerの設定をするエディタウィンドウクラス。</para>
    /// </summary>
    public class NuGetImporterSettingWindow : EditorWindow
    {
        private Vector2 scrollPos;

        [MenuItem("NuGet Importer/NuGet importer settings", false, 4)]
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

            GetWindow<NuGetImporterSettingWindow>("NuGet importer settings");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    InstallMethod before = NuGetImporterSettings.Instance.InstallMethod;
                    EditorGUILayout.LabelField("install method");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.InstallMethod = (InstallMethod)EditorGUILayout.EnumPopup(NuGetImporterSettings.Instance.InstallMethod);
                    if (before != NuGetImporterSettings.Instance.InstallMethod)
                    {
                        _ = NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsUPM
                            ? Operate(PackageManager.ConvertToUPM())
                            : Operate(PackageManager.ConvertToAssets());
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Method to select a version");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.Method = (VersionSelectMethod)EditorGUILayout.EnumPopup(NuGetImporterSettings.Instance.Method);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Auto package placement check");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.AutoPackagePlacementCheck = EditorGUILayout.Toggle(NuGetImporterSettings.Instance.AutoPackagePlacementCheck);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("search cache count");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.SearchCacheLimit = EditorGUILayout.IntField(NuGetImporterSettings.Instance.SearchCacheLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("catalog cache count");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.CatalogCacheLimit = EditorGUILayout.IntField(NuGetImporterSettings.Instance.CatalogCacheLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("icon cache count");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.IconCacheLimit = EditorGUILayout.IntField(NuGetImporterSettings.Instance.IconCacheLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var before = NuGetImporterSettings.Instance.IsCreateAsmdefForAnalyzer;
                    EditorGUILayout.LabelField("Create admdef for analyzer");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.IsCreateAsmdefForAnalyzer = EditorGUILayout.Toggle(NuGetImporterSettings.Instance.IsCreateAsmdefForAnalyzer);
                    if (before != NuGetImporterSettings.Instance.IsCreateAsmdefForAnalyzer)
                    {
                        _ = AsmdefController.UpdateAsmdef(PackageManager.ControlledPackages.installed, PackageManager.GetPackagePathSolver());
                        AssetDatabase.Refresh();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.LabelField("Network settings", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("network save mode");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.IsNetworkSavemode = EditorGUILayout.Toggle(NuGetImporterSettings.Instance.IsNetworkSavemode);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("retry limit");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.RetryLimit = EditorGUILayout.IntField(NuGetImporterSettings.Instance.RetryLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("timeout seconds");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.Timeout = EditorGUILayout.IntField(NuGetImporterSettings.Instance.Timeout);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("ignore packages", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    var ignorePackages = NuGetImporterSettings.Instance.IgnorePackages;
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
                    {
                        scrollPos = scrollView.scrollPosition;
                        for (var i = 0; i < ignorePackages.Count; i++)
                        {
                            ignorePackages[i] = GUILayout.TextField(ignorePackages[i]);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add"))
                        {
                            ignorePackages.Add("");
                        }

                        if (GUILayout.Button("Remove"))
                        {
                            ignorePackages.RemoveAt(ignorePackages.Count - 1);
                        }
                    }
                    NuGetImporterSettings.Instance.IgnorePackages = ignorePackages;
                }
            }
        }

        private static async Task Operate(Task<OperationResult> operation)
        {
            OperationResult result = await operation;
            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }
    }
}
