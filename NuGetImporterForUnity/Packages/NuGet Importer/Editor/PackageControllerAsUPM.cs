using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal class PackageControllerAsUPM : PackageControllerBase
    {
        /// <inheritdoc/>
        internal override void DeletePluginsOutOfDirectory(Package package) { }

        /// <inheritdoc/>
        internal override async Task<string> GetInstallPath(Package package)
        {
            Catalog catalog = await NuGet.GetCatalog(package.id);
            Catalogentry selectedCatalog = catalog.GetAllCatalogEntry().First(entry => entry.version == package.version);
            return Path.Combine(PackageManager.DataPath.Replace("Assets", "Packages"), selectedCatalog.id);
        }

        /// <inheritdoc/>
        internal override async Task<(bool isSkipped, Package package, PackageManagedPluginList asm)> InstallPackageAsync(Package package, IEnumerable<string> loadedAsmName)
        {
            Task<string> task = GetInstallPath(package);
            Task<Catalog> task2 = NuGet.GetCatalog(package.id);
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

            Catalog catalog = await task2;
            Catalogentry selectedCatalog = catalog.GetAllCatalogEntry().First(entry => entry.version == package.version);
            File.WriteAllText(Path.Combine(installPath, "package.json"), JsonUtility.ToJson(selectedCatalog.ToPackageJson(), true));
            return (false, package, asm);
        }
    }
}
