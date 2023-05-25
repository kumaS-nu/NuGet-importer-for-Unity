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
        private static ApiCompatibilityLevel _profile;

        private static IList<string> _packageIds;

        public static IList<string> PackageIds
        {
            get
            {
                if (_profile
                    != PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
                {
                    GetDefaultUnityAssembly();
                }

                return _packageIds;
            }
        }

        [InitializeOnLoadMethod]
        private static void GetDefaultUnityAssembly()
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath)!.Replace("\\", "/");
            Assembly playerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault();
            if (playerAssembly != default)
            {
                var standardRef = playerAssembly.compiledAssemblyReferences
                                                .Select(p => p.Replace("\\", "/"))
                                                .Where(p => !p.StartsWith(projectPath))
                                                .Select(Path.GetFileNameWithoutExtension)
                                                .ToList();
                _packageIds = standardRef.AsReadOnly();
            }

            _profile = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
        }
    }
}
