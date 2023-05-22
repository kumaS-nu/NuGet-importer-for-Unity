using System.IO;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for resolving the path of packages to be installed in Assets.</para>
    /// <para>Assetsにインストールするパッケージのパスを解決するためのクラス。</para>
    /// </summary>
    internal class AssetPathSolver : PackagePathSolverBase
    {
#pragma warning disable CS1998
        /// <inheritdoc/>
        internal override async Task<string> InstallPath(Package package)
        {
            return Path.Combine(PackageManager.DataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
        }

        /// <inheritdoc/>
        internal override async Task<string> InstallPath(string packageName, string version)
        {
            return Path.Combine(PackageManager.DataPath, "Packages", packageName.ToLowerInvariant() + "." + version.ToLowerInvariant());
        }
#pragma warning restore CS1998
    }
}
