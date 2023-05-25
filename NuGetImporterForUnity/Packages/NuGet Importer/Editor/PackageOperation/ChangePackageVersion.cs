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
        private readonly string _id;
        private readonly string _version;
        private readonly bool _onlyStable;
        private readonly VersionSelectMethod _method;

        protected override string FinishMessage { get => "Version change finished."; }

        public ChangePackageVersion(string packageId, string installVersion, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
        {
            _onlyStable = isOnlyStable;
            _method = versionSelect;
            _id = packageId;
            _version = installVersion;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(_id, _version, controlledPackages, _onlyStable, _method);

            requiredPackages = requiredPackages.Where(package => controlledPackages.Existing.All(exist => package.ID != exist.ID)).ToArray();
            Package[] rootPackages = requiredPackages.Where(package => controlledPackages.Root.Any(root => root.ID == package.ID)).ToArray();
            Package[] installPackages = requiredPackages.Where(package => !controlledPackages.Installed.Any(install => install.ID == package.ID && install.Version == package.Version)).ToArray();
            Package[] deletePackages = controlledPackages.Installed.Where(package => !requiredPackages.Any(req => req.ID == package.ID && req.Version == package.Version)).ToArray();

            return await ManipulatePackages(rootPackages, installPackages, deletePackages, controlledPackages, operateLock);
        }
    }
}
