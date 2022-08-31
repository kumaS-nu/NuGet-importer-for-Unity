using System.Collections;
using System.Collections.Generic;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Repair the specified package.</para>
    /// <para>指定されたパッケージを修復する。</para>
    /// </summary>
    internal sealed class FixSpecifiedPackage : OperatePackage
    {
        private readonly string id;

        protected override string FinishMessage { get => "The repair finished."; }

        public FixSpecifiedPackage(string packageId, bool confirm = true)
        {
            id = packageId;
            isConfirmToUser = confirm;
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            IEnumerable<Package> fixPackage = controlledPackages.installed.Where(package => package.id == id).ToArray();
            if (!fixPackage.Any())
            {
                return new OperationResult(OperationState.Cancel, id + " is not installed.");
            }

            var tasks = controlledPackages.installed.Where(pkg => pkg.id == id).Select(async pkg => await PackageManager.HasNativeAsync(pkg));
            var hasNatives = await Task.WhenAll(tasks);
            if (hasNatives.Any(isNative => isNative))
            {
                EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
            }

            return await ManipulatePackages(controlledPackages.root, fixPackage, fixPackage, controlledPackages, operateLock);
        }
    }
}
