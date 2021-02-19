#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
        private static readonly PropertyInfo validateReferences = typeof(PluginImporter).GetProperty("ValidateReferences", BindingFlags.Instance | BindingFlags.NonPublic);

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

            BuildTarget target = BuildTarget.NoTarget;
            var dirName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(assetImporter.assetPath)));
            if (!assetImporter.assetPath.Contains("Packages"))
            {
                return;
            }

            if (dirName == "Packages" && assetImporter.assetPath.EndsWith(".dll"))
            {
                if (validateReferences != null)
                {
                    validateReferences.SetValue(pluginImporter, false);
                }
                return;
            }

            if (!assetImporter.assetPath.Contains("native") || !assetImporter.assetPath.Contains("runtimes"))
            {
                return;
            }
            if (dirName.StartsWith("win"))
            {
                if (dirName.EndsWith("x86"))
                {
                    target = BuildTarget.StandaloneWindows;
                }
                else if (dirName.EndsWith("64"))
                {
                    target = BuildTarget.StandaloneWindows64;
                }
                else
                {
                    return;
                }
            }
            else if (dirName == "osx-x64")
            {
                target = BuildTarget.StandaloneOSX;
            }
            else if (linuxName.Any(linux => dirName.StartsWith(linux)))
            {
                target = BuildTarget.StandaloneLinux64;
            }
            else
            {
                return;
            }

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(true);
            foreach (BuildTarget tar in allTarget)
            {
                pluginImporter.SetCompatibleWithPlatform(tar, false);
            }
            pluginImporter.SetCompatibleWithPlatform(target, true);
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                    pluginImporter.SetEditorData("OS", "Linux");
                    pluginImporter.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "None");
                    break;
                case BuildTarget.StandaloneOSX:
                    pluginImporter.SetEditorData("OS", "OSX");
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    pluginImporter.SetEditorData("OS", "Windows");
                    break;
            }
            pluginImporter.SetEditorData("CPU", target == BuildTarget.StandaloneWindows ? "x86" : "x86_64");
            pluginImporter.SetPlatformData(target, "CPU", target == BuildTarget.StandaloneWindows ? "x86" : "x86_64");
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

#endif