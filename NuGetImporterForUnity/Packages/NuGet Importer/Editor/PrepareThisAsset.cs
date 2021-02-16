using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class to prepare to use NuGet importer.</para>
    /// <para>NuGet importerを使う準備をするクラス。</para>
    /// </summary>
    public class PrepareThisAsset : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            SetCorrectDefine();
        }

        [InitializeOnLoadMethod]
        private static void SetCorrectDefine()
        {
            var haveChange = false;
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';').ToList();

            ApiCompatibilityLevel apiLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (apiLevel == ApiCompatibilityLevel.NET_4_6 || apiLevel == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                if (!File.Exists(Path.Combine(Application.dataPath, "csc.rsp")) || !File.ReadAllLines(Path.Combine(Application.dataPath, "csc.rsp")).Contains("-r:System.IO.Compression.FileSystem.dll"))
                {
                    File.AppendAllText(Path.Combine(Application.dataPath, "csc.rsp"), "-r:System.IO.Compression.FileSystem.dll\n");
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
                        haveChange = true;
                        symbols.Remove("ZIP_AVAILABLE");
                        EditorUtility.DisplayDialog("NuGet importer", "NuGet importer work only .NET 4.x Equivalent.", "OK");
                    }
                }
            }

            if (haveChange)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbols));
            }
        }
    }
}
