using System.Collections;
using System.Collections.Generic;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using System.IO;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Fix as follows in package.config.</para>
    /// <para>package.configの通りに修復する。</para>
    /// </summary>
    internal sealed class FixPackages : OperatePackage
    {
        protected override string FinishMessage { get => "The repair finished."; }

        public FixPackages(bool confirm = true)
        {
            isConfirmToUser = confirm;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            IEnumerable<Task<(bool, Package pkg)>> isInstalled = controlledPackages.installed.Select(async pkg =>
            {
                return (await PackageManager.IsPackageCorrectlyInstalled(pkg), pkg);
            });
            (bool, Package pkg)[] isInstalled_ = await Task.WhenAll(isInstalled);
            IEnumerable<Package> notInstalled = isInstalled_.Where(b => !b.Item1).Select(b => b.pkg);

            if (!notInstalled.Any())
            {
                return new OperationResult(OperationState.Cancel, "No packages to repair.\n(If you want to repair the contents of a package, please repair the package individually.)");
            }

            return await ManipulatePackages(controlledPackages.root, notInstalled, notInstalled, controlledPackages, operateLock);
        }
    }
}
