using System;
using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Assembly definition file.</para>
    /// </summary>
    [Serializable]
    public class AssemblyDefinitionFile
    {
        public string name = "";
        public string rootNamespace = "";
        public List<string> references = new List<string>();
        public List<string> includePlatforms = new List<string>();
        public List<string> excludePlatforms = new List<string>();
        public bool allowUnsafeCode = false;
        public bool overrideReferences = false;
        public List<string> precompiledReferences = new List<string>();
        public bool autoReferenced = true;
        public List<string> defineConstraints = new List<string>();
        public List<VersionDefine> versionDefines;
        public bool noEngineReferences = false;
    }

    /// <summary>
    /// <para>Version defines of asmdef.</para>
    /// <para>asmdef偺Version defines丅</para>
    /// </summary>
    [Serializable]
    public class VersionDefine
    {
        public string name = "";
        public string expression = "";
        public string define = "";
    }
}
