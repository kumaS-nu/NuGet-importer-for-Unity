using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

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
            IsConfirmToUser = confirm;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            IEnumerable<Task<(bool, Package pkg)>> isInstalled = controlledPackages.Installed.Select(
                async pkg => (await PackageManager.IsPackageCorrectlyInstalled(pkg), pkg)
            );
            var installedToPackage = await Task.WhenAll(isInstalled);
            var notInstalled = installedToPackage.Where(b => !b.Item1).Select(b => b.pkg).ToArray();

            return !notInstalled.Any()
                ? new OperationResult(
                    OperationState.Cancel,
                    "No packages to repair.\n(If you want to repair the contents of a package, please repair the package individually.)"
                )
                : await ManipulatePackages(
                    controlledPackages.Root,
                    notInstalled,
                    notInstalled,
                    controlledPackages,
                    operateLock
                );
        }
    }
}
