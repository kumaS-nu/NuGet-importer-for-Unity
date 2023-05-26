using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Move the packages from under Asset to under UPM.</para>
    /// <para>パッケージをAsset下からUPM下に移す。</para>
    /// </summary>
    internal sealed class ConvertToUPM : OperatePackage
    {
        protected override string FinishMessage { get => "Conversion is finished."; }

        public ConvertToUPM()
        {
            IsConfirmToUser = false;
        }

        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            var controller = new PackageControllerAsAsset();
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
                    new PackageControllerAsUPM()
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
                    "Error occured!\nRolls back to before the operation.\nError :\n"
                    + install.Exception!.Message
                    + "\n"
                    + install.Exception!.StackTrace,
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
            try
            {
                File.Delete(Path.Combine(PackageManager.DataPath, "Packages", "managedPluginList.json"));
                File.Delete(Path.Combine(PackageManager.DataPath, "Packages", "managedPluginList.json.meta"));
            }
            catch (Exception) { }

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            operateLock.result = OperationState.Success;
            process.Start();
            EditorApplication.Exit(0);
            return new OperationResult(OperationState.Success, FinishMessage);
        }
    }
}
