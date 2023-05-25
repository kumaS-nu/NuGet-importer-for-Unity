using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for resolving the path of packages to be installed in UPM.</para>
    /// <para>UPMにインストールするパッケージのパスを解決するためのクラス。</para>
    /// </summary>
    internal class UPMPathSolver : PackagePathSolverBase
    {
        /// <inheritdoc/>
        internal override Task<string> InstallPath(Package package)
        {
            return InstallPath(package.ID, package.Version);
        }

        /// <inheritdoc/>
        internal override async Task<string> InstallPath(string packageName, string version)
        {
            Catalog catalog = await NuGet.GetCatalog(packageName);
            Catalogentry selectedCatalog = catalog.GetAllCatalogEntry().First(entry => entry.version == version);
            return Path.Combine(PackageManager.DataPath.Replace("Assets", "Packages"), selectedCatalog.id);
        }
    }
}
