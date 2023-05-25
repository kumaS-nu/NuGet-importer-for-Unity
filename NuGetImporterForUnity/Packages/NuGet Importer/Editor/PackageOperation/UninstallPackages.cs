using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Uninstall the specified package.</para>
    /// <para>指定したパッケージをアンインストールする。</para>
    /// </summary>
    internal sealed class UninstallPackages : OperatePackage
    {
        private readonly string _id;
        private readonly bool _onlyStable;
        private readonly VersionSelectMethod _method;

        protected override string FinishMessage { get => "Uninstallation finished."; }

        public UninstallPackages(
            string packageId,
            bool isOnlyStable = true,
            VersionSelectMethod versionSelect = VersionSelectMethod.Suit
        )
        {
            _id = packageId;
            _onlyStable = isOnlyStable;
            _method = versionSelect;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            if (controlledPackages.Installed.All(pkg => pkg.ID != _id))
            {
                return new OperationResult(OperationState.Cancel, "Selected package is not installed.");
            }

            List<Package> uninstallPackages =
                await DependencySolver.FindRemovablePackages(_id, controlledPackages, _onlyStable, _method);
            Package[] rootPackages = controlledPackages.Installed
                                                       .Where(
                                                           package => uninstallPackages.All(
                                                               uninstall => uninstall.ID != package.ID
                                                           )
                                                       )
                                                       .Where(
                                                           package => controlledPackages.Root.Any(
                                                               root => root.ID == package.ID
                                                           )
                                                       )
                                                       .ToArray();

            return !uninstallPackages.Any()
                ? new OperationResult(OperationState.Cancel, "Selected package is depended by other package.")
                : await ManipulatePackages(
                    rootPackages,
                    Array.Empty<Package>(),
                    uninstallPackages,
                    controlledPackages,
                    operateLock
                );
        }
    }
}
