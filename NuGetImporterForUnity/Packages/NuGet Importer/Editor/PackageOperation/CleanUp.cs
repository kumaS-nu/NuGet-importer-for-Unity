using System;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Uninstall the packages installed with this plugin and initialize the internal data.</para>
    /// <para>このプラグインでインストールしたパッケージを削除し、内部データを初期化する。</para>
    /// </summary>
    internal sealed class CleanUp : OperatePackage
    {
        protected override string FinishMessage { get => "Clean up is finished."; }

        public CleanUp()
        {
            IsConfirmToUser = false;
        }

        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            System.Collections.Generic.IEnumerable<Task<bool>> tasks =
                controlledPackages.Installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg));
            var hasNatives = await Task.WhenAll(tasks);
            if (hasNatives.Any(isNative => isNative))
            {
                EditorUtility.DisplayDialog(
                    "NuGet importer",
                    "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)",
                    "OK"
                );
            }

            return await ManipulatePackages(
                Array.Empty<Package>(),
                Array.Empty<Package>(),
                controlledPackages.Installed,
                controlledPackages,
                operateLock
            );
        }
    }
}
