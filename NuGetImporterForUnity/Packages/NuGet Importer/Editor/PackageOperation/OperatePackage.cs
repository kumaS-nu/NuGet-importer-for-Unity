using System;
using System.Collections;
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
    /// <para>Base class for operating packages. Implement FindOperatePackages when operating on packages. Remember to change onlyStable and method.</para>
    /// <para>パッケージを操作する際の基底クラス。パッケージを操作する際は FindOperatePackages を実装。onlyStable, method の変更を忘れないように。</para>
    /// </summary>
    internal abstract class OperatePackage
    {
        private bool isOperated = false;
        protected bool isConfirmToUser = true;
        protected List<Package> installingPackages = new List<Package>();

        protected virtual string FinishMessage { get => "Operateion finished."; }

        /// <summary>
        /// <para>Execute operations to packages.</para>
        /// <para>パッケージの操作を行う。</para>
        /// </summary>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <para>It is thrown if the operation has already been executed in this instance.</para>
        /// <para>既にこのインスタンスで操作済みの場合スローされる。</para>
        /// </exception>
        public async Task<OperationResult> Execute()
        {
            if (isOperated)
            {
                throw new InvalidOperationException("It has already been operated in this instance.");
            }

            OperationState ret = OperationState.Progress;

            using (var operatorLock = new PackageManager.OperateLock())
            {
                if (operatorLock.IsInvalid)
                {
                    ret = OperationState.Cancel;
                    return new OperationResult(ret, "Now other processes are in progress.");
                }

                var controlledPackages = PackageManager.ControlledPackages;
                try
                {
                    EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);
                    var result = await Operate(controlledPackages, operatorLock);
                    ret = result.State;
                    return result;
                }
                catch (Exception e)
                {
                    ret = OperationState.Failure;
                    UnityEngine.Debug.LogException(e);
                    EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + e.Message, "OK");
                    await Rollback(controlledPackages, operatorLock, default);
                    return new OperationResult(ret, "Rollback to before operation is complete.");
                }
                finally
                {
                    operatorLock.result = ret;
                    isOperated = true;
                }
            }
        }

        /// <summary>
        /// <para>Notify users and manipulate packages by given arguments.</para>
        /// <para>与えられた引数を元にユーザーへ通知・パッケージを操作を行う。</para>
        /// </summary>
        /// <param name="rootPackages">
        /// <para>Root packages.</para>
        /// <para>ルートとなるパッケージ。</para>
        /// </param>
        /// <param name="installPackages">
        /// <para>Installing packages.</para>
        /// <para>インストールするパッケージ。</para>
        /// </param>
        /// <param name="deletePackages">
        /// <para>deleting packages.</para>
        /// <para>削除するパッケージ。</para>
        /// </param>
        /// <param name="controlledPackages">
        /// <para>List of packages under management.</para>
        /// <para>管理下にあるパッケージ一覧。</para>
        /// </param>
        /// <param name="operateLock">
        /// <para>Lock for operation.</para>
        /// <para>操作を行うためのロック。</para>
        /// </param>
        /// <param name="controller">
        /// <para>Controller to operate.</para>
        /// <para>操作を行うコントローラー。</para>
        /// </param>
        /// <returns>
        /// <para>Manipulate result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        protected async Task<OperationResult> ManipulatePackages(IEnumerable<Package> rootPackages, IEnumerable<Package> installPackages, IEnumerable<Package> deletePackages, ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock, PackageControllerBase controller = default)
        {
            OperationState ret = OperationState.Progress;

            installingPackages.AddRange(installPackages);
            Package[] uninstallPackages = deletePackages.Where(package => !installPackages.Any(install => install.id == package.id)).ToArray();
            Package[] changePackages = deletePackages.Where(package => installPackages.Any(install => install.id == package.id)).ToArray();
            Package[] allinstallPackages = controlledPackages.installed.Where(package => !deletePackages.Any(uninstall => uninstall.id == package.id)).Concat(installPackages).ToArray();

            var nativePackages = new List<Package>();
            var managedPackages = new List<Package>();
            foreach (Package package in deletePackages)
            {
                if (await PackageManager.HasNativeAsync(package))
                {
                    nativePackages.Add(package);
                }
                else
                {
                    managedPackages.Add(package);
                }
            }

            if (isConfirmToUser)
            {
                if (!await ConfirmToUser(installPackages, uninstallPackages, nativePackages, controlledPackages))
                {
                    ret = OperationState.Cancel;
                    return new OperationResult(ret, "Operation is canceled.");
                }
            }

            if (deletePackages.Any())
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.25f);

                if (nativePackages.Any())
                {
                    Process process = await PackageManager.OperateWithNativeAsync(installPackages, managedPackages, nativePackages, allinstallPackages, rootPackages, operateLock, controller);
                    ret = OperationState.Success;
                    operateLock.result = ret;
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }

                if (uninstallPackages.Any())
                {
                    await PackageManager.UninstallSelectedPackages(uninstallPackages, operateLock, controller);
                }
            }

            IEnumerable<string> loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
            loadedAsmNames = loadedAsmNames.Except(PackageManager.PackageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

            if (changePackages.Any())
            {
                await PackageManager.UninstallSelectedPackages(changePackages, operateLock, controller);
            }

            IEnumerable<Package> skipped = new List<Package>();
            if (installPackages.Any())
            {
                Task<IEnumerable<Package>> tasks = PackageManager.InstallSelectPackages(installPackages, loadedAsmNames, operateLock, controller);

                _ = PackageManager.DownloadProgress(0.5f, installPackages.Select(package => package.id).ToArray());
                skipped = await tasks;
                if (tasks.IsFaulted)
                {
                    ret = OperationState.Failure;
                    UnityEngine.Debug.LogException(tasks.Exception);
                    EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + tasks.Exception.Message, "OK");
                    await Rollback(controlledPackages, operateLock, controller);
                    return new OperationResult(ret, "Rollback to before operation is complete.");
                }
            }

            if (skipped.Any())
            {
                EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                    skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
            }

            ret = OperationState.Success;
            return new OperationResult(ret, FinishMessage);
        }

        /// <summary>
        /// <para>Confirm the package change to the user.</para>
        /// <para>ユーザーへパッケージ変更の確認を行う。</para>
        /// </summary>
        /// <param name="installPackages">
        /// <para>New package to be installed.</para>
        /// <para>新たにインストールするパッケージ。</para>
        /// </param>
        /// <param name="uninstallPackages">
        /// <para>Package to uninstall.</para>
        /// <para>アンインストールするパッケージ。</para>
        /// </param>
        /// <param name="nativePackages">
        /// <para>Packages that include native plug-ins in the package to be deleted.</para>
        /// <para>削除するパッケージの中でネイティブプラグインを含むパッケージ。</para>
        /// </param>
        /// <returns>
        /// <para>Return true when the user agrees.</para>
        /// <para>ユーザーが了解したか。</para>
        /// </returns>
        private async Task<bool> ConfirmToUser(IEnumerable<Package> installPackages, IEnumerable<Package> uninstallPackages, IEnumerable<Package> nativePackages, ReadOnlyControlledPackages controlledPackages)
        {
            if (!EditorUtility.DisplayDialog("NuGet importer", "Uninstalling below packages\n\n" + string.Join("\n", uninstallPackages.Select(package => package.id + " " + package.version)) + "\n\nInstall or upgrade / downgrade below packages\n\n" + string.Join("\n", installPackages.Select(package => package.id + " " + package.version)), "OK", "Cancel"))
            {
                return false;
            }

            IEnumerable<Package> warningPackages = new List<Package>();

#if UNITY_2021_2_OR_NEWER
            if(PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) == ApiCompatibilityLevel.NET_Standard){
                warningPackages = installPackages.Where(package => !FrameworkName.STANDARD2_1.Contains(package.targetFramework));
            }
#else
            if (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                warningPackages = installPackages.Where(package => !FrameworkName.STANDARD2_0.Contains(package.targetFramework));
            }
#endif
            if (warningPackages.Any())
            {
                if (!EditorUtility.DisplayDialog("Warning from NuGet importer", "Now the api compatibility level for this project is " +
                    PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup).ToString() +
                    ". But below packages are builded for .NETFramework. Do you install them?" + "\n\n" + string.Join("\n", warningPackages.Select(package => package.id + " " + package.version)), "Install", "Cancel"))
                {
                    return false;
                }
            }

            if (installPackages != null && installPackages.Any())
            {
                foreach (Package installPackage in installPackages)
                {
                    var isInstalled = false;
                    if (controlledPackages.installed != null)
                    {
                        isInstalled = PackageManager.InstalledCatalog.ContainsKey(installPackage.id);
                    }
                    Catalog catalog = isInstalled ? PackageManager.InstalledCatalog[installPackage.id] : await NuGet.GetCatalog(installPackage.id);
                    Catalogentry catalogEntry = catalog.GetAllCatalogEntry().First(entry => entry.version == installPackage.version);
                    if (catalogEntry.requireLicenseAcceptance)
                    {
                        var option = EditorUtility.DisplayDialogComplex("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel", "Go url");
                        switch (option)
                        {
                            case 0:
                                break;
                            case 1:
                                return false;
                            case 2:
                                Help.BrowseURL(catalogEntry.licenseUrl);
                                if (!EditorUtility.DisplayDialog("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel"))
                                {
                                    return false;
                                }
                                break;
                        }
                    }
                }
            }

            if (nativePackages.Any())
            {
                if (!EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the modifying package. You need to restart the editor to modify packages.\n(The current project will be saved and modify packages will be resumed after a restart.)", "Restart", "Cancel"))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// <para>Rollback for this operation.</para>
        /// <para>この操作に対するロールバックを行う。</para>
        /// </summary>
        /// <param name="operatorLock">
        /// <para>Lock for this operation.</para>
        /// <para>この操作のために取得したロック。</para>
        /// </param>
        /// <param name="controller">
        /// <para>PackageController used for this operation.</para>
        /// <para>この操作で使用したパッケージコントローラー。</para>
        /// </param>
        protected async Task Rollback(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operatorLock, PackageControllerBase controller)
        {
            await PackageManager.UninstallSelectedPackages(installingPackages, operatorLock, controller);
            IEnumerable<Task<(bool, Package pkg)>> isInstalled = controlledPackages.installed.Select(async pkg =>
            {
                return (await PackageManager.IsPackageCorrectlyInstalled(pkg), pkg);
            });
            (bool, Package pkg)[] isInstalled_ = await Task.WhenAll(isInstalled);
            IEnumerable<Package> notInstalled = isInstalled_.Where(b => !b.Item1).Select(b => b.pkg);
            IEnumerable<string> loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
            loadedAsmNames = loadedAsmNames.Except(PackageManager.PackageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();
            await PackageManager.InstallSelectPackages(notInstalled, loadedAsmNames, operatorLock, controller);
        }

        /// <summary>
        /// <para>Operation.</para>
        /// <para>操作内容。</para>
        /// </summary>
        /// <param name="controlledPackages">
        /// <para>List of packages currently under control.</para>
        /// <para>現在制御下にあるパッケージ一覧。</para>
        /// </param>
        /// <returns>
        /// <para>Operation result</para>
        /// <para>操作結果。</para>
        /// </returns>
        protected abstract Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock);
    }
}
