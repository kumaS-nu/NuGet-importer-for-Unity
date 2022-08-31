using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor.SceneManagement;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Move the packages from under UPM to under Asset.</para>
    /// <para>パッケージをUPM下からAsset下に移す。</para>
    /// </summary>
    internal sealed class ConvertToAssets : OperatePackage
    {
        protected override string FinishMessage { get => "Conversion is finished."; }

        public ConvertToAssets()
        {
            isConfirmToUser = false;
        }

        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            var controller = new PackageControllerAsUPM();
            var tasks = controlledPackages.installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg, controller));
            var hasNatives = await Task.WhenAll(tasks);
            if (!hasNatives.Any(native => native))
            {
                await ManipulatePackages(new Package[0], new Package[0], controlledPackages.installed, controlledPackages, operateLock, controller);
                Task<IEnumerable<Package>> install = PackageManager.InstallSelectPackages(controlledPackages.installed, new string[0], operateLock, new PackageControllerAsAsset());

                _ = PackageManager.DownloadProgress(0.5f, controlledPackages.installed.Select(package => package.id).ToArray());
                await install;
                if (install.IsFaulted)
                {
                    UnityEngine.Debug.LogException(install.Exception);
                    EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + install.Exception.Message, "OK");
                    await Rollback(controlledPackages, operateLock, controller);
                    return new OperationResult(OperationState.Failure, "Rollback to before operation is complete.");
                }
                return new OperationResult(OperationState.Success, FinishMessage);
            }

            EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
            Process process = await PackageManager.OperateWithNativeAsync(controlledPackages.installed, new Package[0], controlledPackages.installed, controlledPackages.installed, controlledPackages.root, operateLock, controller);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            operateLock.result = OperationState.Success;
            process.Start();
            EditorApplication.Exit(0);
            return new OperationResult(OperationState.Success, FinishMessage);
        }
    }
}
