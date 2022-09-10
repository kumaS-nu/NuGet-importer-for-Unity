using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Change the version of the specified package.</para>
    /// <para>指定したパッケージのバージョンを変更する。</para>
    /// </summary>
    internal sealed class ChangePackageVersion : OperatePackage
    {
        private readonly string id;
        private readonly string version;
        private readonly bool onlyStable = true;
        private readonly VersionSelectMethod method = VersionSelectMethod.Suit;

        protected override string FinishMessage { get => "Version change finished."; }

        public ChangePackageVersion(string packageId, string installVersion, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
        {
            onlyStable = isOnlyStable;
            method = versionSelect;
            id = packageId;
            version = installVersion;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(id, version, controlledPackages, onlyStable, method);

            requiredPackages = requiredPackages.Where(package => !controlledPackages.existing.Any(exist => package.id == exist.id)).ToArray();
            Package[] rootPackages = requiredPackages.Where(package => controlledPackages.root.Any(root => root.id == package.id)).ToArray();
            Package[] installPackages = requiredPackages.Where(package => !controlledPackages.installed.Any(install => install.id == package.id && install.version == package.version)).ToArray();
            Package[] deletePackages = controlledPackages.installed.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();

            return await ManipulatePackages(rootPackages, installPackages, deletePackages, controlledPackages, operateLock);
        }
    }
}
