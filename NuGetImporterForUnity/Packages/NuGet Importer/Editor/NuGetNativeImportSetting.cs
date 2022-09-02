using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for configuring native plugins introduced from NuGet.</para>
    /// <para>NuGetから導入したネイティブプラグインの設定をするためのクラス。</para>
    /// </summary>
    public class NuGetNativeImportSetting : AssetPostprocessor
    {
        private static readonly BuildTarget[] allTarget = (BuildTarget[])Enum.GetValues(typeof(BuildTarget));
        private static readonly List<string> linuxName = new List<string>() { "linux", "ubuntu", "centos", "debian" };

        private void OnPreprocessAsset()
        {
            if (!assetImporter.importSettingsMissing)
            {
                return;
            }

            var pluginImporter = assetImporter as PluginImporter;
            if (pluginImporter == null)
            {
                return;
            }
            if (!assetImporter.assetPath.Contains("Packages"))
            {
                return;
            }

            if (!assetImporter.assetPath.Contains("native") || !assetImporter.assetPath.Contains("runtimes"))
            {
                return;
            }

            BuildTarget target = BuildTarget.NoTarget;
            var targetCPU = "";
            var enableOnEditor = false;
            var dirName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(assetImporter.assetPath)));
            var splitedDirName = dirName.Split('-');
            if (splitedDirName.Length < 2)
            {
                return;
            }

            switch (splitedDirName[0])
            {
                case "win":
                    enableOnEditor = true;
                    switch (splitedDirName[1])
                    {
                        case "x64":
                            target = BuildTarget.StandaloneWindows64;
                            targetCPU = "x86_64";
                            break;
                        case "x86":
                            target = BuildTarget.StandaloneWindows;
                            targetCPU = "x86";
                            break;
                        default:
                            return;
                    }
                    break;
                case "osx":
                    target = BuildTarget.StandaloneOSX;
                    switch (splitedDirName[1])
                    {
                        case "x64":
                            enableOnEditor = true;
                            targetCPU = "x86_64";
                            break;
#if UNITY_2020_2_OR_NEWER
                        case "arm64":
                            enableOnEditor = true;
                            targetCPU = "ARM64";
                            break;
#endif
                        default:
                            return;
                    }
                    break;
                case "android":
                    target = BuildTarget.Android;
                    switch (splitedDirName[1])
                    {
                        case "arm":
                            targetCPU = "ARMv7";
                            break;
                        case "arm64":
                            targetCPU = "ARM64";
                            break;
                        case "x64":
                            targetCPU = "x86_64";
                            break;
                        case "x86":
                            targetCPU = "x86";
                            break;
                        default:
                            return;
                    }
                    break;
                case "ios":
                    target = BuildTarget.iOS;
                    switch (splitedDirName[1])
                    {
                        case "arm":
                            targetCPU = "ARMv7";
                            break;
                        case "arm64":
                            targetCPU = "ARM64";
                            break;
                        case "x64":
                            targetCPU = "X64";
                            break;
                        default:
                            return;
                    }
                    break;
                default:
                    enableOnEditor = true;
                    if (linuxName.Contains(splitedDirName[0]))
                    {
                        target = BuildTarget.StandaloneLinux64;
                        targetCPU = "x86_64";
                    }
                    else
                    {
                        return;
                    }
                    break;
            }

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(enableOnEditor);
            foreach (BuildTarget tar in allTarget)
            {
                pluginImporter.SetCompatibleWithPlatform(tar, false);
            }
            pluginImporter.SetCompatibleWithPlatform(target, true);
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                    pluginImporter.SetEditorData("OS", "Linux");
                    pluginImporter.SetEditorData("CPU", targetCPU);
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "None");
                    break;
                case BuildTarget.StandaloneOSX:
                    pluginImporter.SetEditorData("OS", "OSX");
                    pluginImporter.SetEditorData("CPU", targetCPU);
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    pluginImporter.SetEditorData("OS", "Windows");
                    pluginImporter.SetEditorData("CPU", targetCPU);
                    break;
            }
            pluginImporter.SetPlatformData(target, "CPU", targetCPU);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var imported in importedAssets)
            {
                var dirName = Path.GetFileName(Path.GetDirectoryName(imported));
                if (dirName == "native")
                {
                    var native = AssetImporter.GetAtPath(imported) as PluginImporter;
                    if (native != null)
                    {
                        native.isPreloaded = true;
                    }
                }
            }
        }
    }
}
