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
    internal sealed class ReInstallAllPackages : OperatePackage
    {
        protected override string FinishMessage { get => "Reinstall is finished."; }

        private readonly bool _isOnlyStable;
        private readonly VersionSelectMethod _versionSelectMethod;

        public ReInstallAllPackages(bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            IsConfirmToUser = false;
            _isOnlyStable = onlyStable;
            _versionSelectMethod = method;
        }

        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            IEnumerable<Task<bool>> tasks =
                controlledPackages.Installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg));
            List<Package> requiredPackages = await DependencySolver.CheckAllPackage(
                controlledPackages,
                _isOnlyStable,
                _versionSelectMethod
            );
            var hasNatives = await Task.WhenAll(tasks);
            if (!hasNatives.Any(native => native))
            {
                await ManipulatePackages(
                    new Package[0],
                    new Package[0],
                    controlledPackages.Installed,
                    controlledPackages,
                    operateLock
                );
                await Task.Delay(100);
                var install = PackageManager.InstallSelectPackages(
                    requiredPackages,
                    Array.Empty<string>(),
                    operateLock
                );

                _ = PackageManager.DownloadProgress(
                    0.5f,
                    controlledPackages.Installed.Select(package => package.ID).ToArray()
                );
                await install;
                if (install.IsFaulted)
                {
                    UnityEngine.Debug.LogException(install.Exception);
                    EditorUtility.DisplayDialog(
                        "NuGet importer",
                        "Error occured!\nRolls back to before the operation.\nError :\n" + install.Exception!.Message,
                        "OK"
                    );
                    await Rollback(controlledPackages, operateLock);
                    return new OperationResult(OperationState.Failure, "Rollback to before operation is complete.");
                }

                return new OperationResult(OperationState.Success, FinishMessage);
            }

            EditorUtility.DisplayDialog(
                "NuGet importer",
                "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)",
                "OK"
            );
            Process process = await PackageManager.OperateWithNativeAsync(
                requiredPackages,
                Array.Empty<Package>(),
                controlledPackages.Installed,
                requiredPackages,
                requiredPackages.Where(req => controlledPackages.Root.Any(r => r.ID == req.ID)),
                operateLock
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
