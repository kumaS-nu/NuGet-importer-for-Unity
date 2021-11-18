#if ZIP_AVAILABLE

using UnityEditor;

using UnityEngine;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.IO;
using System.Threading.Tasks;
using System;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>EditorWindow class to configure cache settings for NuGet importer.</para>
    /// <para>NuGet importerのキャッシュを設定をするエディタウィンドウクラス。</para>
    /// </summary>
    public class NuGetImporterSettingWindow : EditorWindow
    {
        [MenuItem("NuGet Importer/Cache settings", false, 3)]
        private static void ShowWindow()
        {
            GetWindow<NuGetImporterSettingWindow>("Cache settings (NuGet importer)");
        }

        private void OnGUI()
        {
            var isAssets = NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsAssets;
            if (isAssets != File.Exists(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json")))
            {
                if(EditorUtility.DisplayDialog("NuGet importer", "The installation method setting does not match the current installation method: UPM (recommended) or Assets ?", "UPM", "Assets")){
                    NuGetImporterSettings.Instance.InstallMethod = InstallMethod.AsUPM;
                    _ = Operate(PackageManager.ConvertToUPM());
                }
                else
                {
                    NuGetImporterSettings.Instance.InstallMethod = InstallMethod.AsAssets;
                    _ = Operate(PackageManager.ConvertToAssets());
                }
            }

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("install method");
                    GUILayout.FlexibleSpace();
                    NuGetImporterSettings.Instance.InstallMethod = (InstallMethod)EditorGUILayout.EnumPopup(NuGetImporterSettings.Instance.InstallMethod);
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
            }
        }

        private async Task Operate(Task<bool> operation)
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