using System;
using System.IO;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for configuring native plugins introduced from NuGet.</para>
    /// <para>NuGetから導入したネイティブプラグインの設定をするためのクラス。</para>
    /// </summary>
    public class NuGetNativeImportSetting : AssetPostprocessor
    {
        private static BuildTarget[] allTarget = default;

        private static void SetAllTarget()
        {
            if (allTarget != default)
            {
                return;
            }

            System.Collections.Generic.IEnumerable<(Enum val, string name)> all = Enum.GetValues(typeof(BuildTarget)).Cast<Enum>().ToArray()
                            .Zip(Enum.GetNames(typeof(BuildTarget)), (val, name) => (val, name));

            BuildTarget[] nonObsolete = all.Where(platform => !typeof(BuildTarget).GetMember(platform.name).First()
                                                .GetCustomAttributes(typeof(ObsoleteAttribute), false)
                                                .Any(attr => attr is ObsoleteAttribute))
                                .Select(platform => platform.val).Cast<BuildTarget>().ToArray();
            allTarget = nonObsolete.Where(platform => platform > 0).ToArray();
        }

        // I don't know why, but I can't set it up in OnPreprocessAsset().

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            SetAllTarget();

            foreach (var imported in importedAssets)
            {
                if (!imported.Contains("Packages") || !imported.Contains("native") || !imported.Contains("runtimes"))
                {
                    continue;
                }

                var native = AssetImporter.GetAtPath(imported) as PluginImporter;
                if (native == null)
                {
                    continue;
                }

                (bool isNoTarget, bool enableOnEditor, BuildTarget target, string architecture) current = GetImportSettings(native);
                (bool enableOnEditor, BuildTarget target, string architecture) target = GetPluginSetting(imported);
                if (current.isNoTarget && !current.enableOnEditor && target.target == BuildTarget.NoTarget)
                {
                    continue;
                }

                if (current.target == target.target && current.enableOnEditor == target.enableOnEditor && current.architecture == target.architecture)
                {
                    continue;
                }

                SetImportSetting(native, target.enableOnEditor, target.target, target.architecture);
            }
        }

        /// <summary>
        /// <para>Get current plugin settings.</para>
        /// <para>現在のプラグイン設定を取得。</para>
        /// </summary>
        /// <param name="pluginImporter">
        /// <para>Target plugins.</para>
        /// <para>対象のプラグイン。</para>
        /// </param>
        /// <returns>
        /// <para>Is there a target? Is it valid on the editor? Target. Architecture.</para>
        /// <para>ターゲットが無いか。エディタ上で有効か。ターゲット。アーキテクチャ。</para>
        /// </returns>
        private static (bool isNoTarget, bool enableOnEditor, BuildTarget target, string architecture) GetImportSettings(PluginImporter pluginImporter)
        {
            var enableOnEditor = pluginImporter.GetCompatibleWithEditor();
            BuildTarget[] enableTarget = allTarget.Where(t => pluginImporter.GetCompatibleWithPlatform(t)).ToArray();
            BuildTarget target = enableTarget.Length == 1 ? enableTarget[0] : BuildTarget.NoTarget;
            var architecture = target == BuildTarget.NoTarget ? "" : pluginImporter.GetPlatformData(target, "CPU");
            return (!enableTarget.Any(), enableOnEditor, target, architecture);
        }

        /// <summary>
        /// <para>Get the plugin settings that should be set.</para>
        /// <para>設定すべきプラグイン設定を取得。</para>
        /// </summary>
        /// <param name="pluginPath">
        /// <para>Target plugins path.</para>
        /// <para>対象のプラグインのパス。</para>
        /// </param>
        /// <returns>
        /// <para>Is it valid on the editor? Target. Architecture.</para>
        /// <para>エディタ上で有効か。ターゲット。アーキテクチャ。</para>
        /// </returns>
        private static (bool enableOnEditor, BuildTarget target, string architecture) GetPluginSetting(string pluginPath)
        {
            BuildTarget target = BuildTarget.NoTarget;
            var architecture = "";
            var enableOnEditor = false;
            var platformPath = pluginPath;
            while (platformPath.Contains("native"))
            {
                platformPath = Path.GetDirectoryName(platformPath);
            }
            var platform = new NativePlatform(platformPath);
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.x64):
                    architecture = platform.os == nameof(OSType.ios) ? "X64" : "x86_64";
                    break;
                case nameof(ArchitectureType.x86):
                    architecture = "x86";
                    break;
                case nameof(ArchitectureType.arm64):
                    architecture = "ARM64";
                    break;
                case nameof(ArchitectureType.arm):
                    architecture = "ARMv7";
                    break;
            }

            switch (platform.os)
            {
                case nameof(OSType.win):
                    target = WindowsProcess(platform);
                    enableOnEditor = true;
                    break;
                case nameof(OSType.osx):
                    target = OSXProcess(platform);
                    enableOnEditor = true;
                    break;
                case nameof(OSType.android):
                    target = AndroidProcess(platform);
                    enableOnEditor = false;
                    break;
                case nameof(OSType.ios):
                    target = IOSProcess(platform);
                    enableOnEditor = false;
                    break;
                case nameof(OSType.linux):
                case nameof(OSType.ubuntu):
                case nameof(OSType.debian):
                case nameof(OSType.fedora):
                case nameof(OSType.centos):
                case nameof(OSType.alpine):
                case nameof(OSType.rhel):
                case nameof(OSType.arch):
                case nameof(OSType.opensuse):
                case nameof(OSType.gentoo):
                    target = LinuxProcess(platform);
                    enableOnEditor = true;
                    break;
            }

            return (enableOnEditor, target, architecture);
        }

        private static void SetImportSetting(PluginImporter pluginImporter, bool enableOnEditor, BuildTarget target, string architecture)
        {
            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(enableOnEditor);
            pluginImporter.SetExcludeEditorFromAnyPlatform(enableOnEditor);
            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                    pluginImporter.SetEditorData("OS", "Linux");
                    pluginImporter.SetEditorData("CPU", architecture);
                    break;
                case BuildTarget.StandaloneOSX:
                    pluginImporter.SetEditorData("OS", "OSX");
                    pluginImporter.SetEditorData("CPU", architecture);
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    pluginImporter.SetEditorData("OS", "Windows");
                    pluginImporter.SetEditorData("CPU", architecture);
                    break;
            }

            foreach (BuildTarget tar in allTarget)
            {
                if (tar != target)
                {
                    pluginImporter.SetCompatibleWithPlatform(tar, false);
                    pluginImporter.SetExcludeFromAnyPlatform(tar, false);
                }
                else
                {
                    pluginImporter.SetCompatibleWithPlatform(target, true);
                    pluginImporter.SetExcludeFromAnyPlatform(target, true);
                    if (target == BuildTarget.StandaloneLinux64)
                    {
                        // It would be better if we specify x86_64 for Linux,
                        // but it is replaced with AnyCPU, and the change is confirmed
                        // whenever we see it in the inspector.
                        pluginImporter.SetPlatformData(target, "CPU", "AnyCPU");
                    }
                    else
                    {
                        pluginImporter.SetPlatformData(target, "CPU", architecture);
                    }
                }
            }

            if (target != BuildTarget.NoTarget)
            {
                pluginImporter.isPreloaded = true;
            }
        }

        private static BuildTarget WindowsProcess(NativePlatform platform)
        {
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.x64):
                    return BuildTarget.StandaloneWindows64;
                case nameof(ArchitectureType.x86):
                    return BuildTarget.StandaloneWindows;
                case nameof(ArchitectureType.arm64):
                case nameof(ArchitectureType.arm):
                    return BuildTarget.WSAPlayer;
                default:
                    return BuildTarget.NoTarget;
            }
        }

        private static BuildTarget OSXProcess(NativePlatform platform)
        {
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.x64):
                    return BuildTarget.StandaloneOSX;
#if UNITY_2020_2_OR_NEWER
                case nameof(ArchitectureType.arm64):
                    return BuildTarget.StandaloneOSX;
#endif
                default:
                    return BuildTarget.NoTarget;
            }
        }

        private static BuildTarget LinuxProcess(NativePlatform platform)
        {
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.x64):
                    return BuildTarget.StandaloneLinux64;
                default:
                    return BuildTarget.NoTarget;
            }
        }

        private static BuildTarget AndroidProcess(NativePlatform platform)
        {
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.x64):
                case nameof(ArchitectureType.x86):
                case nameof(ArchitectureType.arm64):
                case nameof(ArchitectureType.arm):
                    return BuildTarget.Android;
                default:
                    return BuildTarget.NoTarget;
            }
        }

        private static BuildTarget IOSProcess(NativePlatform platform)
        {
            switch (platform.architecture)
            {
                case nameof(ArchitectureType.arm64):
                case nameof(ArchitectureType.arm):
                    return BuildTarget.iOS;
                default:
                    return BuildTarget.NoTarget;
            }
        }
    }
}
