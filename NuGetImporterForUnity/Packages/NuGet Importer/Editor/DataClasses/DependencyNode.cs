using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    internal class DependencyNode
    {
        internal string PackageName { get; private set; }
        internal SemVer Version { get; private set; }
        internal string TragetFramework { get; set; }
        internal readonly List<DependencyNode> dependingNode = new List<DependencyNode>();
        internal readonly List<DependencyNode> dependedNode = new List<DependencyNode>();

        internal DependencyNode(string packageName, string allowedVersion)
        {
            PackageName = packageName;
            Version = new SemVer
            {
                AllowedVersion = allowedVersion
            };
        }
    }
}