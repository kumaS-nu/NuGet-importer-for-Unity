using System;
using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    [Serializable]
    public class ManagedPluginList
    {
        public List<PackageManagedPluginList> managedList;
    }

    [Serializable]
    public class PackageManagedPluginList
    {
        public string packageName;
        public List<string> fileNames;
    }
}
