using System.Collections;
using System.Collections.Generic;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.IO;
using System.Threading.Tasks;

using UnityEngine;
using System.Linq;
using System.Xml.Serialization;

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

        public RebootProcess(InstalledPackages willInstall, InstalledPackages willRoot, InstalledPackages rollbackPackages)
        {
            isConfirmToUser = false;
            install = willInstall;
            root = willRoot;
            rPackages = rollbackPackages;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            if (!install.package.Any())
            {
                return new OperationResult(OperationState.Success, "Uninstallation finished.");
            }

            Package[] deletePackages = new Package[0];

            if (rPackages != null)
            {
                deletePackages = rPackages.package.Where(rollback => !controlledPackages.installed.Any(pkg => pkg.id == rollback.id)).ToArray();
            }

            return await ManipulatePackages(root.package, install.package, deletePackages, controlledPackages, operateLock);
        }
    }
}
