using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

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
            isConfirmToUser = false;
        }

        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            var tasks = controlledPackages.installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg));
            var hasNatives = await Task.WhenAll(tasks);
            if (hasNatives.Any(isNative => isNative))
            {
                EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
            }
            return await ManipulatePackages(new Package[0], new Package[0], controlledPackages.installed, controlledPackages, operateLock);
        }
    }
}
