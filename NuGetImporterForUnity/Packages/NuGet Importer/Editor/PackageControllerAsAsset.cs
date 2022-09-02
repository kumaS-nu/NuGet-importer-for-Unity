using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal sealed class PackageControllerAsAsset : PackageControllerBase
    {
        private static ManagedPluginList managedPluginList;
        private static readonly object managedPluginListLock = new object();
        private readonly static string managedPluginListPath = Path.Combine(PackageManager.DataPath, "Packages", "managedPluginList.json");

        /// <inheritdoc/>
        internal override void DeletePluginsOutOfDirectory(Package package)
        {
            lock (managedPluginListLock)
            {
                if (managedPluginList == null)
                {
                    LoadManagedPluginList();
                }
            }
            IEnumerable<PackageManagedPluginList> _managed = managedPluginList.managedList.Where(list => list.packageName == package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
            if (!_managed.Any())
            {
                return;
            }
            PackageManagedPluginList managed = _managed.First();
            try
            {
                foreach (var file in managed.fileNames)
                {
                    File.Delete(Path.Combine(PackageManager.DataPath, "Packages", "Plugins", file));
                    File.Delete(Path.Combine(PackageManager.DataPath, "Packages", "Plugins", file + ".meta"));
                }

                if (!Directory.GetFiles(Path.Combine(PackageManager.DataPath, "Packages", "Plugins")).Any())
                {
                    DeleteDirectory(Path.Combine(PackageManager.DataPath, "Packages", "Plugins"));
                }
            }
            catch (InvalidDataException) { }

            lock (managedPluginListLock)
            {
                managedPluginList.managedList.Remove(managed);
                WriteManagedPluginList();
            }
        }

#pragma warning disable CS1998
        /// <inheritdoc/>
        internal override async Task<string> GetInstallPath(Package package)
        {
            return Path.Combine(PackageManager.DataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
        }
#pragma warning restore CS1998

        /// <inheritdoc/>
        internal override async Task<(bool isSkipped, Package package, PackageManagedPluginList asm)> InstallPackageAsync(Package package, IEnumerable<string> loadedAsmName)
        {
            var topDirectory = Path.Combine(PackageManager.DataPath, "Packages");
            if (!Directory.Exists(topDirectory))
            {
                Directory.CreateDirectory(topDirectory);
            }

            Task<string> task = GetInstallPath(package);
            await ExtractPackageAsync(package);
            var installPath = await task;
            var asm = new PackageManagedPluginList
            {
                packageName = package.id,
                fileNames = new List<string>()
            };
            GetLoadableAsmInPackage(installPath, asm);

            if (asm.fileNames.Intersect(loadedAsmName).Any())
            {
                DeleteDirectory(installPath);
                return (true, package, asm);
            }

            return (false, package, asm);
        }

        private void LoadManagedPluginList()
        {
            if (File.Exists(managedPluginListPath))
            {
                managedPluginList = JsonUtility.FromJson<ManagedPluginList>(File.ReadAllText(managedPluginListPath));
            }

            if (managedPluginList == null)
            {
                managedPluginList = new ManagedPluginList();
            }

            if (managedPluginList.managedList == null)
            {
                managedPluginList.managedList = new List<PackageManagedPluginList>();
            }
        }

        private void WriteManagedPluginList()
        {
            File.WriteAllText(managedPluginListPath, JsonUtility.ToJson(managedPluginList, true));
        }
    }
}
