using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Processes the startup process after a reboot.</para>
    /// <para>再起動を行った後の起動時の処理を行う。</para>
    /// </summary>
    internal sealed class RebootProcess : OperatePackage
    {
        protected override string FinishMessage { get => "The package operation finished."; }
        private readonly InstalledPackages install;
        private readonly InstalledPackages root;
        private readonly InstalledPackages rPackages;

        public RebootProcess(
            InstalledPackages willInstall,
            InstalledPackages willRoot,
            InstalledPackages rollbackPackages
        )
        {
            IsConfirmToUser = false;
            install = willInstall;
            root = willRoot;
            rPackages = rollbackPackages;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            if (!install.Package.Any())
            {
                return new OperationResult(OperationState.Success, "Uninstallation finished.");
            }

            var deletePackages = Array.Empty<Package>();

            if (rPackages != null)
            {
                deletePackages = rPackages.Package
                                          .Where(
                                              rollback => controlledPackages.Installed.All(pkg => pkg.ID != rollback.ID)
                                          )
                                          .ToArray();
            }

            return await ManipulatePackages(
                root.Package,
                install.Package,
                deletePackages,
                controlledPackages,
                operateLock
            );
        }
    }
}
