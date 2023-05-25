using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;
using UnityEditor.SceneManagement;

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
            IsConfirmToUser = false;
        }

        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            var controller = new PackageControllerAsUPM();
            IEnumerable<Task<bool>> tasks =
                controlledPackages.Installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg, controller));
            var hasNatives = await Task.WhenAll(tasks);
            if (!hasNatives.Any(native => native))
            {
                await ManipulatePackages(
                    Array.Empty<Package>(),
                    Array.Empty<Package>(),
                    controlledPackages.Installed,
                    controlledPackages,
                    operateLock,
                    controller
                );
                var install = PackageManager.InstallSelectPackages(
                    controlledPackages.Installed,
                    Array.Empty<string>(),
                    operateLock,
                    new PackageControllerAsAsset()
                );

                _ = PackageManager.DownloadProgress(
                    0.5f,
                    controlledPackages.Installed.Select(package => package.ID).ToArray()
                );
                await install;
                if (!install.IsFaulted)
                {
                    return new OperationResult(OperationState.Success, FinishMessage);
                }

                UnityEngine.Debug.LogException(install.Exception);
                EditorUtility.DisplayDialog(
                    "NuGet importer",
                    "Error occured!\nRolls back to before the operation.\nError :\n" + install.Exception!.Message,
                    "OK"
                );
                await Rollback(controlledPackages, operateLock, controller);
                return new OperationResult(OperationState.Failure, "Rollback to before operation is complete.");

            }

            EditorUtility.DisplayDialog(
                "NuGet importer",
                "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)",
                "OK"
            );
            Process process = await PackageManager.OperateWithNativeAsync(
                controlledPackages.Installed,
                Array.Empty<Package>(),
                controlledPackages.Installed,
                controlledPackages.Installed,
                controlledPackages.Root,
                operateLock,
                controller
            );
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            operateLock.result = OperationState.Success;
            process.Start();
            EditorApplication.Exit(0);
            return new OperationResult(OperationState.Success, FinishMessage);
        }
    }
}
