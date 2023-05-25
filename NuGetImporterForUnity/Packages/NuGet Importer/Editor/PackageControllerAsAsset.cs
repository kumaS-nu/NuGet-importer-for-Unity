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
        private static ManagedPluginList _managedPluginList;
        private static readonly object ManagedPluginListLock = new object();

        private static readonly string ManagedPluginListPath = Path.Combine(
            PackageManager.DataPath,
            "Packages",
            "managedPluginList.json"
        );

        public PackageControllerAsAsset()
        {
            pathSolver = new AssetPathSolver();
        }

        /// <inheritdoc/>
        internal override void DeletePluginsOutOfDirectory(Package package)
        {
            lock (ManagedPluginListLock)
            {
                if (_managedPluginList == null)
                {
                    LoadManagedPluginList();
                }
            }

            var managedPluginLists = _managedPluginList!.managedList.Where(
                                                            list => list.packageName
                                                                    == package.ID.ToLowerInvariant()
                                                                    + "."
                                                                    + package.Version
                                                                             .ToLowerInvariant()
                                                        )
                                                        .ToArray();
            if (!managedPluginLists.Any())
            {
                return;
            }

            var managed = managedPluginLists.First();
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

            lock (ManagedPluginListLock)
            {
                _managedPluginList.managedList.Remove(managed);
                WriteManagedPluginList();
            }
        }

        /// <inheritdoc/>
        internal override async Task<(bool isSkipped, Package package, PackageManagedPluginList asm)>
            InstallPackageAsync(Package package, IEnumerable<string> loadedAsmName)
        {
            var topDirectory = Path.Combine(PackageManager.DataPath, "Packages");
            if (!Directory.Exists(topDirectory))
            {
                Directory.CreateDirectory(topDirectory);
            }

            Task<string> task = pathSolver.InstallPath(package);
            await ExtractPackageAsync(package);
            var installPath = await task;
            var asm = new PackageManagedPluginList { packageName = package.ID, fileNames = new List<string>() };
            GetLoadableAsmInPackage(installPath, asm);

            if (!asm.fileNames.Intersect(loadedAsmName).Any())
            {
                return (false, package, asm);
            }

            DeleteDirectory(installPath);
            return (true, package, asm);

        }

        private void LoadManagedPluginList()
        {
            if (File.Exists(ManagedPluginListPath))
            {
                _managedPluginList = JsonUtility.FromJson<ManagedPluginList>(File.ReadAllText(ManagedPluginListPath));
            }

            _managedPluginList ??= new ManagedPluginList();

            _managedPluginList.managedList ??= new List<PackageManagedPluginList>();
        }

        private void WriteManagedPluginList()
        {
            File.WriteAllText(ManagedPluginListPath, JsonUtility.ToJson(_managedPluginList, true));
        }
    }
}
