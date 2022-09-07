using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    internal sealed class ReInstallAllPackages : OperatePackage
    {
        protected override string FinishMessage { get => "Reinstall is finished."; }

        private bool isOnlyStable;
        private VersionSelectMethod versionSelectMethod;

        public ReInstallAllPackages(bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            isConfirmToUser = false;
            isOnlyStable = onlyStable;
            versionSelectMethod = method;
        }

        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            var tasks = controlledPackages.installed.Select(async pkg => await PackageManager.HasNativeAsync(pkg));
            var requredPackages = await DependencySolver.CheckAllPackage(controlledPackages, isOnlyStable, versionSelectMethod);
            var hasNatives = await Task.WhenAll(tasks);
            if (!hasNatives.Any(native => native))
            {
                await ManipulatePackages(new Package[0], new Package[0], controlledPackages.installed, controlledPackages, operateLock);
                await Task.Delay(100);
                Task<IEnumerable<Package>> install = PackageManager.InstallSelectPackages(requredPackages, new string[0], operateLock);

                _ = PackageManager.DownloadProgress(0.5f, controlledPackages.installed.Select(package => package.id).ToArray());
                await install;
                if (install.IsFaulted)
                {
                    UnityEngine.Debug.LogException(install.Exception);
                    EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + install.Exception.Message, "OK");
                    await Rollback(controlledPackages, operateLock);
                    return new OperationResult(OperationState.Failure, "Rollback to before operation is complete.");
                }
                return new OperationResult(OperationState.Success, FinishMessage);
            }

            EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
            Process process = await PackageManager.OperateWithNativeAsync(requredPackages, new Package[0], controlledPackages.installed, requredPackages, requredPackages.Where(req => controlledPackages.root.Any(r => r.id == req.id)), operateLock);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            operateLock.result = OperationState.Success;
            process.Start();
            EditorApplication.Exit(0);
            return new OperationResult(OperationState.Success, FinishMessage);
        }
    }
}
