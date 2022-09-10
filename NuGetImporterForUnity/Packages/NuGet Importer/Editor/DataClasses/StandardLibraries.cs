using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Compilation;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>List of packages contained in Unity.</para>
    /// <para>Unityで最初から含まれているパッケージ一覧。</para>
    /// </summary>
    public static class StandardLibraries
    {
        private static ApiCompatibilityLevel profile = default;

        private static IList<string> packageIds = default;
        public static IList<string> PackageIds
        {
            get
            {
                if (profile != PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
                {
                    GetDefaultUnityAssembly();
                }
                return packageIds;
            }
        }

        [InitializeOnLoadMethod]
        private static void GetDefaultUnityAssembly()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath).Replace("\\", "/");
            Assembly playerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).First();
            var standardRef = playerAssembly.compiledAssemblyReferences
                                .Select(p => p.Replace("\\", "/"))
                                .Where(p => !p.StartsWith(projectPath)).Select(p => Path.GetFileNameWithoutExtension(p))
                                .ToList();
            packageIds = standardRef.AsReadOnly();
            profile = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
        }
    }
}
