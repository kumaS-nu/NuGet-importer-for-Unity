using System.Linq;

using UnityEditor;
using UnityEditor.Build;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>A class that sets whether all packages introduced by NuGet are ready.</para>
    /// <para>NuGetで導入したパッケージがすべて準備完了であるか設定するクラス。</para>
    /// </summary>
    internal class PackageReadyState : IActiveBuildTargetChanged
    {
        public int callbackOrder { get => 0; }

        /// <summary>
        /// <para>Set as ready.</para>
        /// <para>準備完了状態と設定する。</para>
        /// </summary>
        internal static void SetReady()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();
            if (!symbols.Contains("NUGET_PACKAGE_READY"))
            {
                symbols.Add("NUGET_PACKAGE_READY");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
            }
        }

        /// <summary>
        /// <para>Set as unready.</para>
        /// <para>準備未完了状態と設定する。</para>
        /// </summary>
        internal static void SetUnReady()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';').ToList();
            symbols.Remove("NUGET_PACKAGE_READY");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", symbols));
        }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.quitting += SetUnReady;
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(previousTarget)).Split(';').ToList();
            symbols.Remove("NUGET_PACKAGE_READY");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(previousTarget), string.Join(";", symbols));
        }
    }
}
