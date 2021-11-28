using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal sealed class PackageControllerAsAsset : PackageControllerBase
    {
        private static ManagedPluginList managedPluginList;
        private static readonly object managedPluginListLock = new object();
        private readonly static string managedPluginListPath = Path.Combine(Application.dataPath, "Packages", "managedPluginList.json");

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
            var managed = managedPluginList.managedList.First(list => list.packageName == package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
            try
            {
                foreach (var file in managed.fileNames)
                {
                    File.Delete(Path.Combine(dataPath, "Packages", "Plugins", file));
                    File.Delete(Path.Combine(dataPath, "Packages", "Plugins", file + ".meta"));
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
            return Path.Combine(dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
        }
#pragma warning restore CS1998

        /// <inheritdoc/>
        internal override async Task InstallPackageAsync(Package package)
        {
            var topDirectory = Path.Combine(dataPath, "Packages");
            var managedDirectory = Path.Combine(topDirectory, "Plugins");
            var directoryName = package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant();
            var packageDirectory = Path.Combine(topDirectory, directoryName);
            if (!Directory.Exists(topDirectory))
            {
                Directory.CreateDirectory(topDirectory);
            }
            if (!Directory.Exists(managedDirectory))
            {
                Directory.CreateDirectory(managedDirectory);
            }

            await ExtractPackageAsync(package);

            var packageManagedList = new PackageManagedPluginList
            {
                packageName = directoryName,
                fileNames = new List<string>()
            };

            if (Directory.Exists(Path.Combine(packageDirectory, "lib")))
            {
                var lib = Directory.GetDirectories(Path.Combine(packageDirectory, "lib"));
                if (lib.Length > 1)
                {
                    throw new InvalidDataException();
                }
                if (lib.Any())
                {
                    foreach (var moveFile in Directory.GetFiles(lib[0]))
                    {
                        var destPath = Path.Combine(managedDirectory, Path.GetFileName(moveFile));
                        if (File.Exists(destPath))
                        {
                            File.Delete(destPath);
                        }
                        File.Move(moveFile, destPath);
                        packageManagedList.fileNames.Add(Path.GetFileName(moveFile));
                    }
                }
            }

            lock (managedPluginListLock)
            {
                if (managedPluginList == null)
                {
                    LoadManagedPluginList();
                }
                managedPluginList.managedList.Add(packageManagedList);
                WriteManagedPluginList();
            }
            DeleteDirectory(Path.Combine(packageDirectory, "lib"));
        }

        private void LoadManagedPluginList()
        {
            if (File.Exists(managedPluginListPath))
            {
                managedPluginList = JsonUtility.FromJson<ManagedPluginList>(File.ReadAllText(managedPluginListPath));
            }
            
            if(managedPluginList == null)
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
