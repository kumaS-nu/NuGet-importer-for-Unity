using System.Collections;
using System.Collections.Generic;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.Linq;
using System.Threading.Tasks;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Uninstall the specified package.</para>
    /// <para>指定したパッケージをアンインストールする。</para>
    /// </summary>
    internal sealed class UninstallPackages : OperatePackage
    {
        private readonly string id;
        private readonly bool onlyStable = true;
        private readonly VersionSelectMethod method = VersionSelectMethod.Suit;

        protected override string FinishMessage { get => "Uninstallation finished."; }

        public UninstallPackages(string packageId, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
        {
            id = packageId;
            onlyStable = isOnlyStable;
            method = versionSelect;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            if (!controlledPackages.installed.Any(pkg => pkg.id == id))
            {
                return new OperationResult(OperationState.Cancel, "Selected package is not installed.");
            }

            List<Package> uninstallPackages = await DependencySolver.FindRemovablePackages(id, controlledPackages, onlyStable, method);
            Package[] rootPackages = controlledPackages.installed.Where(package => !uninstallPackages.Any(uninstall => uninstall.id == package.id)).Where(package => controlledPackages.root.Any(root => root.id == package.id)).ToArray();

            if (!uninstallPackages.Any())
            {
                return new OperationResult(OperationState.Cancel, "Selected package is depended by other package.");
            }

            return await ManipulatePackages(rootPackages, new Package[0], uninstallPackages, controlledPackages, operateLock);
        }
    }

}
