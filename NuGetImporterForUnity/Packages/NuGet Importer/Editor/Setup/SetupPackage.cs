using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.Setup
{
    /// <summary>
    /// <para>Class to prepare to use NuGet importer.</para>
    /// <para>NuGet importerを使う準備をするクラス。</para>
    /// </summary>
    public class SetupPackage : AssetPostprocessor
    {
        private static List<ApiCompatibilityLevel> enableApiLevel = new List<ApiCompatibilityLevel>
        {
            ApiCompatibilityLevel.NET_4_6,
            ApiCompatibilityLevel.NET_Standard_2_0
#if UNITY_2021_2_OR_NEWER
            ,ApiCompatibilityLevel.NET_Unity_4_8
            ,ApiCompatibilityLevel.NET_Standard
#endif
        };

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            SetCorrectDefine();
        }

        [InitializeOnLoadMethod]
        private static void SetCorrectDefine()
        {
            var haveChange = false;
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();

            ApiCompatibilityLevel apiLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (enableApiLevel.Contains(apiLevel))
            {
                if (!File.Exists(Path.Combine(Application.dataPath, "csc.rsp")) || !File.ReadAllLines(Path.Combine(Application.dataPath, "csc.rsp")).Contains("-r:System.IO.Compression.FileSystem.dll"))
                {
                    File.AppendAllText(Path.Combine(Application.dataPath, "csc.rsp"), "-r:System.IO.Compression.FileSystem.dll\n");
                }

                if (!symbols.Contains("ZIP_AVAILABLE"))
                {
                    haveChange = true;
                    symbols.Add("ZIP_AVAILABLE");
                }
            }
            else
            {
                if (File.Exists(Path.Combine(Application.dataPath, "csc.rsp")))
                {
                    var allLine = File.ReadAllLines(Path.Combine(Application.dataPath, "csc.rsp")).ToList();
                    if (allLine.Contains("-r:System.IO.Compression.FileSystem.dll"))
                    {
                        allLine.Remove("-r:System.IO.Compression.FileSystem.dll");
                        File.WriteAllLines(Path.Combine(Application.dataPath, "csc.rsp"), allLine);
                    }
                }

                if (symbols.Contains("ZIP_AVAILABLE"))
                {
                    haveChange = true;
                    symbols.Remove("ZIP_AVAILABLE");
                    EditorUtility.DisplayDialog("NuGet importer", "NuGet importer work only .NET 4.x Equivalent.", "OK");
                }
            }

            if (haveChange)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
            }
        }

        [InitializeOnLoadMethod]
        private static void SetAssemblyVersionValidation()
        {
            PlayerSettings.assemblyVersionValidation = false;
            EditorApplication.quitting += SetUnReady;
        }

        /// <summary>
        /// <para>Set as unready.</para>
        /// <para>準備未完了状態と設定する。</para>
        /// </summary>
        internal static void SetUnReady()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();
            symbols.Remove("ZIP_AVAILABLE");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        }
    }
}
