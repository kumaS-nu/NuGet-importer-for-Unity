
#if ZIP_AVAILABLE

using UnityEditor;

using UnityEngine;


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
            NuGetImporterSettingWindow window = GetWindow<NuGetImporterSettingWindow>();
            window.titleContent = new GUIContent("Cache settings (NuGet importer)");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("search cache count");
                    GUILayout.FlexibleSpace();
                    NuGet.searchCacheLimit = EditorGUILayout.IntField(NuGet.searchCacheLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("catalog cache count");
                    GUILayout.FlexibleSpace();
                    NuGet.catalogCacheLimit = EditorGUILayout.IntField(NuGet.catalogCacheLimit);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("icon cache count");
                    GUILayout.FlexibleSpace();
                    PackageDataExtentionToGUI.iconCacheLimit = EditorGUILayout.IntField(PackageDataExtentionToGUI.iconCacheLimit);
                }
            }
        }
    }
}

#endif