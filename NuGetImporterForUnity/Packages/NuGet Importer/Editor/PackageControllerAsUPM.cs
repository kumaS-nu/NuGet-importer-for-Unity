using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal class PackageControllerAsUPM : PackageControllerBase
    {
        internal override void DeletePluginsOutOfDirectory(Package package) { }

        internal override async Task<string> GetInstallPath(Package package)
        {
            var catalog = await NuGet.GetCatalog(package.id);
            var selectedCatalog = catalog.GetAllCatalogEntry().First(entry => entry.version == package.version);
            return Path.Combine(dataPath.Replace("Assets", "Packages"), selectedCatalog.id);
        }

        internal override async Task InstallPackageAsync(Package package)
        {
            var task = NuGet.GetCatalog(package.id);
            var task2 = GetInstallPath(package);
            await ExtractPackageAsync(package);
            var catalog = await task;
            var installPath = await task2;
            var selectedCatalog = catalog.GetAllCatalogEntry().First(entry => entry.version == package.version);
            File.WriteAllText(Path.Combine(installPath, "package.json"), JsonUtility.ToJson(selectedCatalog.ToPackageJson(), true));
        }
    }
}