#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for managing packages.</para>
    /// <para>パッケージを管理するクラス。</para>
    /// </summary>
    public static class PackageManager
    {
        private static bool working = false;
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(InstalledPackages));
        private static readonly string projectSettingsPath = Application.dataPath.Replace("Assets", "ProjectSettings");
        private static readonly string packagePath = Path.Combine(Application.dataPath, "packages.config");
        private static readonly string rootPackagePath = Path.Combine(projectSettingsPath, "rootPackages.xml");
        private static readonly string existingPackagePath = Path.Combine(projectSettingsPath, "existingPackages.xml");
        private static readonly string packageAsmNamesPath = Path.Combine(projectSettingsPath, "packageAsmNames.json");

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages installed;

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages rootPackage;

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages existingPackage;

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static ManagedPluginList packageAsmNames;

        /// <value>
        /// <para>Catalogs of installed packages.</para>
        /// <para>インストールされているパッケージのカタログ。</para>
        /// </value>
        internal static Dictionary<string, Catalog> installedCatalog = new Dictionary<string, Catalog>();

        /// <value>
        /// <para>Path to install. 0:UPM・1:Assets/Plugins</para>
        /// <para>インストールする場所。0:UPM・1:Assets/Plugins</para>
        /// </value>
        internal static int installLocate = 0;

        /// <value>
        /// <para>Installed package.</para>
        /// <para>インストールされているパッケージ。</para>
        /// </value>
        internal static InstalledPackages Installed { get => installed; }

        /// <summary>
        /// <para>Root packages.</para>
        /// <para>ルートのパッケージ。</para>
        /// </summary>
        internal static InstalledPackages RootPackage { get => rootPackage; }

        /// <summary>
        /// <para>Packages that are not under control within a project.</para>
        /// <para>プロジェクト内で監理外にあるパッケージ。</para>
        /// </summary>
        internal static InstalledPackages ExiestingPackage { get => existingPackage; }

        /// <summary>
        /// <para>Save the package installation information.</para>
        /// <para>パッケージのインストール情報を保存する。</para>
        /// </summary>
        public static void Save()
        {
            using (var file = new StreamWriter(packagePath, false))
            {
                serializer.Serialize(file, installed);
            }

            using (var file = new StreamWriter(rootPackagePath, false))
            {
                serializer.Serialize(file, rootPackage);
            }

            using (var file = new StreamWriter(existingPackagePath, false))
            {
                serializer.Serialize(file, existingPackage);
            }

            File.WriteAllText(packageAsmNamesPath, JsonUtility.ToJson(packageAsmNames, true));
        }

        /// <summary>
        /// <para>Initialize.</para>
        /// <para>初期化する。</para>
        /// </summary>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        [InitializeOnLoadMethod]
        public static async Task Initialize()
        {
            if (File.Exists(packagePath))
            {
                using (var file = new StreamReader(packagePath))
                {
                    installed = (InstalledPackages)serializer.Deserialize(file);
                }
            }

            if (installed == null)
            {
                installed = new InstalledPackages();
            }
            if (installed.package == null)
            {
                installed.package = new Package[0];
            }

            if (File.Exists(Path.Combine(Application.dataPath, "rootPackages.xml")))
            {
                using (var file = new StreamReader(Path.Combine(Application.dataPath, "rootPackages.xml")))
                {
                    rootPackage = (InstalledPackages)serializer.Deserialize(file);
                }
                try
                {
                    File.Move(Path.Combine(Application.dataPath, "rootPackages.xml"), rootPackagePath);
                    File.Delete(Path.Combine(Application.dataPath, "rootPackages.xml.meta"));
                }
                catch (Exception) { }
            }
            else if (File.Exists(rootPackagePath))
            {
                using (var file = new StreamReader(rootPackagePath))
                {
                    rootPackage = (InstalledPackages)serializer.Deserialize(file);
                }
            }

            if (rootPackage == null)
            {
                rootPackage = new InstalledPackages();
            }
            if (rootPackage.package == null)
            {
                rootPackage.package = new Package[0];
            }

            if (File.Exists(existingPackagePath))
            {
                using (var file = new StreamReader(existingPackagePath))
                {
                    existingPackage = (InstalledPackages)serializer.Deserialize(file);
                }
            }

            if (existingPackage == null)
            {
                existingPackage = new InstalledPackages();
            }
            if (existingPackage.package == null)
            {
                existingPackage.package = new Package[0];
            }

            if (File.Exists(packageAsmNamesPath))
            {
                packageAsmNames = JsonUtility.FromJson<ManagedPluginList>(File.ReadAllText(packageAsmNamesPath));
            }

            if (packageAsmNames == null)
            {
                packageAsmNames = new ManagedPluginList();
            }

            if (packageAsmNames.managedList == null)
            {
                packageAsmNames.managedList = new List<PackageManagedPluginList>();
            }

            await RebootProcess();

            if (installed.package != null)
            {
                var tasks = installed.package.Select(pkg => NuGet.GetCatalog(pkg.id));
                var catalogs = await Task.WhenAll(tasks);
                lock (installedCatalog)
                {
                    foreach (var catalog in catalogs)
                    {
                        installedCatalog[catalog.nuget_id] = catalog;
                    }
                }
            }

            NuGetImporterWindow.Initialize();
        }

        private static async Task RebootProcess()
        {
            if (!File.Exists(Application.dataPath.Replace("Assets", "WillInstall.xml")))
            {
                return;
            }
            working = true;
            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("Nuget importer", "Installing packages", 0.25f);
                IEnumerable<Package> skipped = default;
                using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillInstall.xml")))
                {
                    var install = (InstalledPackages)serializer.Deserialize(file);
                    if (install != null && install.package != null && install.package.Any())
                    {
                        var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                        loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames));
                        var task = InstallSelectPackages(install.package, loadedAsmNames);
                        _ = DownloadProgress(0.25f, install.package.Select(pkg => pkg.id).ToArray());
                        skipped = await task;
                    }
                }

                if (NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsUPM)
                {
                    DeleteAsAssetDirectory();
                }

                using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillPackage.xml")))
                {
                    installed = (InstalledPackages)serializer.Deserialize(file);
                }

                if (installed == null)
                {
                    installed = new InstalledPackages();
                }
                if (installed.package == null)
                {
                    installed.package = new Package[0];
                }

                using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillRoot.xml")))
                {
                    rootPackage = (InstalledPackages)serializer.Deserialize(file);
                }

                if (rootPackage == null)
                {
                    rootPackage = new InstalledPackages();
                }
                if (rootPackage.package == null)
                {
                    rootPackage.package = new Package[0];
                }

                EditorUtility.ClearProgressBar();
                if (skipped != null)
                {
                    if (skipped.Any())
                    {
                        EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                            skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
                    }
                    EditorUtility.DisplayDialog("Nuget importer", "Installation finished.", "OK");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Nuget importer", e.Message, "OK");
            }
            finally
            {
                File.Delete(Application.dataPath.Replace("Assets", "WillInstall.xml"));
                File.Delete(Application.dataPath.Replace("Assets", "WillPackage.xml"));
                File.Delete(Application.dataPath.Replace("Assets", "WillRoot.xml"));
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }
        }

        private static void DeleteAsAssetDirectory()
        {
            try
            {
                var dirs = Directory.GetDirectories(Path.Combine(Application.dataPath, "Packages"));
                var files = Directory.GetFiles(Path.Combine(Application.dataPath, "Packages"));
                if (dirs.Length != files.Length)
                {
                    return;
                }
                if (dirs.Length != 0 && Path.GetFileName(dirs[0]) != "Plugins")
                {
                    return;
                }
                Directory.Delete(Path.Combine(Application.dataPath, "Packages"), true);
                File.Delete(Path.Combine(Application.dataPath, "Packages.meta"));
            }
            catch (Exception) { }
        }

        /// <summary>
        /// <para>Install the specified package.</para>
        /// <para>指定されたパッケージをインストールする。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id.</para>
        /// <para>パッケージのid。</para>
        /// </param>
        /// <param name="version">
        /// <para>The version of the package to install.</para>
        /// <para>インストールするパッケージのバージョン。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Has the operation been executed?</para>
        /// <para>操作が行われたか。</para>
        /// </returns>
        public static async Task<bool> InstallPackage(string packageId, string version, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                await Initialize();

                // Find out the packages that need to be changed.
                IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackages(packageId, version, onlyStable, method);
                requiredPackages = requiredPackages.Where(package => !existingPackage.package.Any(exist => package.id == exist.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                Package[] samePackages = installed.package.Where(install => requiredPackages.Any(dep => dep.id == install.id && dep.version != install.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in samePackages)
                {
                    if (await HasNativeAsync(package))
                    {
                        nativePackages.Add(package);
                    }
                    else
                    {
                        managedPackages.Add(package);
                    }
                }

                if (!await ConfirmToUser(installPackages, new Package[0], nativePackages))
                {
                    return false;
                }

                var addRootPackage = requiredPackages.Where(pkg => pkg.id == packageId);
                EditorUtility.DisplayProgressBar("NuGet importer", "Removing packages before upgrade.", 0.25f);
                if (nativePackages.Any())
                {
                    var rootPackages = requiredPackages.Where(pkg => rootPackage.package.Any(root => root.id == pkg.id));
                    if (addRootPackage.Any())
                    {
                        rootPackages = rootPackages.Append(addRootPackage.First());
                    }
                    var process = await OperateWithNativeAsync(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages.ToArray());
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }

                var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

                if (samePackages.Any())
                {
                    await UninstallPackages(samePackages);
                }

                var task = InstallSelectPackages(installPackages, loadedAsmNames);

                _ = DownloadProgress(0.5f, requiredPackages.Select(package => package.id).ToArray());
                var skipped = await task;
                installed.package = requiredPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();
                rootPackage.package = requiredPackages.Where(pkg => rootPackage.package.Any(root => root.id == pkg.id)).ToArray();
                if (addRootPackage.Any() && !skipped.Any(pkg => pkg.id == addRootPackage.First().id))
                {
                    rootPackage.package = rootPackage.package.Append(addRootPackage.First()).ToArray();
                }

                if (skipped.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                        skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        /// <summary>
        /// <para>Repair the specified package.</para>
        /// <para>指定されたパッケージを修復する。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id.</para>
        /// <para>パッケージのid。</para>
        /// </param>
        /// <returns>
        /// <para>Has the operation been executed?</para>
        /// <para>操作が行われたか。</para>
        /// </returns>
        public static async Task<bool> FixPackage(string packageId)
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("NuGet importer", "Reinstalling package", 0);

                await Initialize();

                IEnumerable<Package> fixPackage = installed.package.Where(package => package.id == packageId);
                if (!fixPackage.Any())
                {
                    throw new ArgumentException(packageId + " is not installed.");
                }
                Package fix = fixPackage.First();
                if (await HasNativeAsync(fix))
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the repair package. You need to restart the editor to repair packages.\n(The current project will be saved and repair packages will be resumed after a restart.)", "Restart", "Quit"))
                    {
                        var process = await OperateWithNativeAsync(new Package[] { fix }, new Package[0], new Package[] { fix }, installed.package, rootPackage.package);
                        AssetDatabase.SaveAssets();
                        EditorSceneManager.SaveOpenScenes();
                        process.Start();
                        EditorApplication.Exit(0);
                    }
                    else
                    {
                        return false;
                    }
                }
                var task = InstallSelectPackages(new Package[] { fix }, new string[0]);
                _ = DownloadProgress(0.25f, new string[] { fix.id });
                await task;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        /// <summary>
        /// <para>Optimize and repair the package.</para>
        /// <para>パッケージの依存関係等を最適化し、修復する。</para>
        /// </summary>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Has the operation been executed?</para>
        /// <para>操作が行われたか。</para>
        /// </returns>
        public static async Task<bool> FixPackage(bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                await Initialize();

                if (installed.package.Length == 0)
                {
                    throw new InvalidOperationException("No packages installed.");
                }

                // Find out the packages that need to be changed.
                IEnumerable<Package> requiredPackages = await DependencySolver.CheckAllPackage(onlyStable, method);
                Package[] deletePackages = installed.package.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();
                Package[] uninstallPackages = deletePackages.Where(package => !installed.package.Any(install => install.id == package.id)).ToArray();
                Package[] upgradePackages = deletePackages.Where(package => installed.package.Any(install => install.id == package.id)).ToArray();

                requiredPackages = requiredPackages.Where(package => !existingPackage.package.Any(exist => package.id == exist.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in deletePackages)
                {
                    if (await HasNativeAsync(package))
                    {
                        nativePackages.Add(package);
                    }
                    else
                    {
                        managedPackages.Add(package);
                    }
                }

                if (!deletePackages.Any() && !installPackages.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "Installed packages are already optimized.", "OK");
                    return true;
                }

                if (!await ConfirmToUser(installPackages, uninstallPackages, nativePackages))
                {
                    return false;
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.33f);

                if (nativePackages.Any())
                {
                    var rootPackages = requiredPackages.Where(pkg => rootPackage.package.Any(root => root.id == pkg.id)).ToArray();
                    var process = await OperateWithNativeAsync(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(uninstallPackages);

                var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

                await UninstallPackages(upgradePackages);

                var task = InstallSelectPackages(installPackages, loadedAsmNames);

                _ = DownloadProgress(0.33f, requiredPackages.Select(package => package.id).ToArray());
                var skipped = await task;
                installed.package = requiredPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();
                rootPackage.package = requiredPackages.Where(pkg => rootPackage.package.Any(root => root.id == pkg.id)).Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();

                if (skipped.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                        skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        /// <summary>
        /// <para>Uninstall the specified package.</para>
        /// <para>指定したパッケージをアンインストールする。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id.</para>
        /// <para>パッケージのid。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Has the operation been executed?</para>
        /// <para>操作が行われたか。</para>
        /// </returns>
        public static async Task<bool> UninstallPackages(string packageId, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;
            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                if (installed.package.Length == 0)
                {
                    throw new InvalidOperationException("No packages installed.");
                }
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                await Initialize();

                // Find out the packages that need to be changed.
                List<Package> uninstallPackages = await DependencySolver.FindRemovablePackages(packageId, onlyStable, method);
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in uninstallPackages)
                {
                    if (await HasNativeAsync(package))
                    {
                        nativePackages.Add(package);
                    }
                    else
                    {
                        managedPackages.Add(package);
                    }
                }
                Package[] installedPackages = installed.package.Where(package => !uninstallPackages.Any(uninstall => uninstall.id == package.id)).ToArray();
                var rootPackages = installedPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();

                if (!uninstallPackages.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "Selected package is depended by other package.", "OK");
                    return true;
                }

                if (!await ConfirmToUser(new Package[0], uninstallPackages, nativePackages))
                {
                    return false;
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Uninstalling packges", 0.5f);

                if (nativePackages.Any())
                {
                    var process = await OperateWithNativeAsync(new Package[0], managedPackages, nativePackages, installedPackages, rootPackages);
                    process.Start();
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(uninstallPackages);

                installed.package = installedPackages;
                rootPackage.package = rootPackages;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        /// <summary>
        /// <para>Change the version of the specified package.</para>
        /// <para>指定したパッケージのバージョンを変更する。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id.</para>
        /// <para>パッケージのid。</para>
        /// </param>
        /// <param name="newVersion">
        /// <para>Version after change.</para>
        /// <para>変更後のバージョン。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Has the operation been executed?</para>
        /// <para>操作が行われたか。</para>
        /// </returns>
        public static async Task<bool> ChangePackageVersion(string packageId, string newVersion, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                await Initialize();

                // Find out the packages that need to be changed.
                IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(packageId, newVersion, onlyStable, method);
                Package[] deletePackages = installed.package.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();
                Package[] uninstallPackages = deletePackages.Where(package => !installed.package.Any(install => install.id == package.id)).ToArray();
                Package[] upgradePackages = deletePackages.Where(package => installed.package.Any(install => install.id == package.id)).ToArray();

                requiredPackages = requiredPackages.Where(package => !existingPackage.package.Any(exist => package.id == exist.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in deletePackages)
                {
                    if (await HasNativeAsync(package))
                    {
                        nativePackages.Add(package);
                    }
                    else
                    {
                        managedPackages.Add(package);
                    }
                }

                if (!await ConfirmToUser(installPackages, uninstallPackages, nativePackages))
                {
                    return false;
                }

                var rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.25f);

                if (nativePackages.Any())
                {
                    var process = await OperateWithNativeAsync(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(uninstallPackages);

                var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

                await UninstallPackages(upgradePackages);

                var tasks = InstallSelectPackages(installPackages, loadedAsmNames);

                _ = DownloadProgress(0.5f, requiredPackages.Select(package => package.id).ToArray());
                var skipped = await tasks;
                installed.package = requiredPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();
                rootPackage.package = rootPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();

                if (skipped.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                        skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        public static async Task<bool> ConvertToUPM()
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("NuGet importer", "Checking packages", 0.1f);

                await Initialize();

                if (!installed.package.Any())
                {
                    try
                    {
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json.meta"));
                    }
                    catch (Exception) { }
                    DeleteAsAssetDirectory();

                    return true;
                }
                var controller = new PackageControllerAsAsset();
                var tasks = installed.package.Select(async pkg =>
                {
                    var path = await controller.GetInstallPath(pkg);
                    return HasNative(path);
                });

                var isNatives = await Task.WhenAll(tasks);
                EditorUtility.DisplayProgressBar("NuGet importer", "Deleting packages", 0.4f);
                if (isNatives.Any(isNative => isNative))
                {
                    EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
                    var process = await controller.OperateWithNativeAsync(installed.package, new Package[0], installed.package, installed.package, rootPackage.package);
                    try
                    {
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json.meta"));
                    }
                    catch (Exception) { }
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }
                else
                {
                    await controller.UninstallManagedPackagesAsync(installed.package);
                    try
                    {
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json.meta"));
                    }
                    catch (Exception) { }
                    DeleteAsAssetDirectory();

                    EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages", 0.5f);

                    var installer = new PackageControllerAsUPM();
                    var tasks2 = new List<Task>();
                    var loadedAsmName = new string[0];
                    foreach (var pkg in installed.package)
                    {
                        tasks2.Add(installer.InstallPackageAsync(pkg, loadedAsmName));
                    }
                    _ = DownloadProgress(0.5f, installed.package.Select(pkg => pkg.id).ToArray());
                    await Task.WhenAll(tasks2);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        public static async Task<bool> ConvertToAssets()
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();

                EditorUtility.DisplayProgressBar("NuGet importer", "Checking packages", 0.1f);

                await Initialize();

                if (!installed.package.Any())
                {
                    if (!Directory.Exists(Path.Combine(Application.dataPath, "Packages")))
                    {
                        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Packages"));
                    }
                    File.WriteAllText(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json"), "");
                    return true;
                }
                var controller = new PackageControllerAsUPM();
                var tasks = installed.package.Select(async pkg =>
                {
                    var path = await controller.GetInstallPath(pkg);
                    var packageId = "";
                    var jsonPath = Path.Combine(path, "package.json");
                    if (File.Exists(jsonPath))
                    {
                        var jsonString = File.ReadAllText(jsonPath);
                        try
                        {
                            var json = JsonUtility.FromJson<PackageJson>(jsonString);
                            packageId = json.name;
                        }
                        catch (Exception) { }
                    }
                    return HasNative(path, packageId);
                });

                var isNatives = await Task.WhenAll(tasks);
                EditorUtility.DisplayProgressBar("NuGet importer", "Deleting packages", 0.4f);
                if (isNatives.Any(isNative => isNative))
                {
                    EditorUtility.DisplayDialog("NuGet importer", "We restart Unity, because the native plugin is included in the installed package.\n(The current project will be saved.)", "OK");
                    var process = await controller.OperateWithNativeAsync(installed.package, new Package[0], installed.package, installed.package, rootPackage.package);
                    AssetDatabase.SaveAssets();
                    EditorSceneManager.SaveOpenScenes();
                    process.Start();
                    EditorApplication.Exit(0);
                }
                else
                {
                    await controller.UninstallManagedPackagesAsync(installed.package);
                    EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages", 0.5f);

                    var installer = new PackageControllerAsAsset();
                    var tasks2 = new List<Task>();
                    var loadedAsmName = new string[0];
                    foreach (var pkg in installed.package)
                    {
                        tasks2.Add(installer.InstallPackageAsync(pkg, loadedAsmName));
                    }
                    _ = DownloadProgress(0.5f, installed.package.Select(pkg => pkg.id).ToArray());
                    await Task.WhenAll(tasks2);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                Client.Resolve();
#endif
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        private async static Task<bool> ConfirmToUser(IEnumerable<Package> installPackages, IEnumerable<Package> uninstallPackages, IEnumerable<Package> nativePackages)
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
                    lock (installedCatalog)
                    {
                        if (installed.package != null)
                        {
                            isInstalled = installedCatalog.ContainsKey(installPackage.id);
                        }
                    }
                    Catalog catalog = isInstalled ? installedCatalog[installPackage.id] : await NuGet.GetCatalog(installPackage.id);
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

        private static async Task<IEnumerable<Package>> InstallSelectPackages(IEnumerable<Package> packages, IEnumerable<string> loadAssembliesFullName)
        {
            var controller = GetPackageController();
            var tasks = new List<Task<(bool isSkipped, Package package, PackageManagedPluginList asm)>>();
            foreach (var package in packages)
            {
                tasks.Add(controller.InstallPackageAsync(package, loadAssembliesFullName));
            }
            var result = await Task.WhenAll(tasks);
            var ret = new List<Package>();
            foreach (var (isSkipped, package, asm) in result)
            {
                if (isSkipped)
                {
                    ret.Add(package);
                }
                else
                {
                    Catalog catalog = await NuGet.GetCatalog(package.id);
                    lock (installedCatalog)
                    {
                        installedCatalog[package.id] = catalog;
                    }

                    lock (packageAsmNames)
                    {
                        packageAsmNames.managedList.Add(asm);
                    }
                }
            }

            lock (existingPackage)
            {
                var exist = existingPackage.package.ToList();
                exist.AddRange(ret.Select(r => new Package() { id = r.id, version = "0.0.0" }));
                existingPackage.package = exist.ToArray();
            }

            return ret;
        }

        private static async Task<Process> OperateWithNativeAsync(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root)
        {
            var controller = GetPackageController();
            var process = await controller.OperateWithNativeAsync(installs, manageds, natives, allInstalled, root);
            installed.package = installed.package.Where(package => !manageds.Any(manage => manage.id == package.id) && !natives.Any(native => native.id == package.id)).ToArray();
            rootPackage.package = rootPackage.package.Where(package => installed.package.Any(installed => installed.id == package.id)).ToArray();
            packageAsmNames.managedList = packageAsmNames.managedList.Where(pkg => installed.package.Any(installed => installed.id == pkg.packageName)).ToList();
            Save();
            return process;
        }

        private static async Task UninstallPackages(IEnumerable<Package> packages)
        {
            var controller = GetPackageController();
            await controller.UninstallManagedPackagesAsync(packages);
            lock (installed)
            {
                lock (installedCatalog)
                {
                    lock (packageAsmNames)
                    {
                        foreach (var package in packages)
                        {
                            installed.package = installed.package.Where(pkg => pkg.id != package.id).ToArray();
                            installedCatalog.Remove(package.id);
                            packageAsmNames.managedList.RemoveAll(names => names.packageName == package.id);
                        }
                    }
                }
            }
        }

        private static PackageControllerBase GetPackageController()
        {
            switch (NuGetImporterSettings.Instance.InstallMethod)
            {
                case InstallMethod.AsUPM:
                    return new PackageControllerAsUPM();
                case InstallMethod.AsAssets:
                    return new PackageControllerAsAsset();
                default:
                    throw new InvalidDataException();
            }

        }

        private static async Task<bool> HasNativeAsync(Package package)
        {
            var controller = GetPackageController();
            var path = await controller.GetInstallPath(package);
            var packageId = "";
            if (NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsUPM)
            {
                var jsonPath = Path.Combine(path, "package.json");
                if (File.Exists(jsonPath))
                {
                    var jsonString = File.ReadAllText(jsonPath);
                    try
                    {
                        var json = JsonUtility.FromJson<PackageJson>(jsonString);
                        packageId = json.name;
                    }
                    catch (Exception) { }
                }
            }
            return HasNative(path, packageId);
        }

        private readonly static string rootPath = Application.dataPath.Replace("Assets", "");

        private static bool HasNative(string path, string packageId = "")
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                var filePath = file.Replace(rootPath, "");
                if (packageId != "")
                {
                    var splited = filePath.Split('/', '\\');
                    splited[1] = packageId;
                    filePath = string.Join("/", splited);
                }
                var plugin = AssetImporter.GetAtPath(filePath) as PluginImporter;
                if (plugin != null)
                {
                    if (plugin.isNativePlugin)
                    {
                        return true;
                    }
                }
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                if (HasNative(directory, packageId))
                {
                    return true;
                }
            }

            return false;
        }

        private async static Task DownloadProgress(float startPos, IEnumerable<string> packageNames)
        {
            var allPackageSize = new Dictionary<string, long>();
            var downloadedSumSizeLog = new LinkedList<long>();
            while (true)
            {
                var downloadedSumSize = 0L;
                var finishedCount = 0;
                foreach (string packageName in packageNames)
                {
                    if (NuGet.TryGetDownloadingProgress(packageName, out long packageSize, out long downloadedSize))
                    {
                        allPackageSize[packageName] = packageSize;
                        downloadedSumSize += downloadedSize;

                    }
                    else
                    {
                        finishedCount++;
                        if (allPackageSize.ContainsKey(packageName))
                        {
                            downloadedSumSize += allPackageSize[packageName];
                        }
                    }
                }

                if (finishedCount == packageNames.Count())
                {
                    break;
                }

                var packageSumSize = 0L;
                foreach (var packageSize in allPackageSize)
                {
                    packageSumSize += packageSize.Value;
                }

                var downloadSpead = 0L;
                if (downloadedSumSizeLog.Count == 10)
                {
                    downloadSpead = downloadedSumSize - downloadedSumSizeLog.First.Value;
                }

                if (working)
                {
                    EditorUtility.DisplayProgressBar("NuGet importer", "Downloading packages. " + ToReadableSizeString(downloadedSumSize) + " / " + ToReadableSizeString(packageSumSize) + "    " + ToReadableSizeString(downloadSpead) + "/s", startPos + (1 - startPos) * 5 / 6 * downloadedSumSize / packageSumSize);
                }

                downloadedSumSizeLog.AddLast(downloadedSumSize);
                if (downloadedSumSizeLog.Count > 10)
                {
                    downloadedSumSizeLog.RemoveFirst();
                }

                await Task.Delay(100);
            }

            if (working)
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Extracting packages.", 1 - startPos / 6);
            }
        }

        private static readonly string[] unit = new string[] { "B", "KB", "MB", "GB", "TB" };

        private static string ToReadableSizeString(long size)
        {
            var index = 0;
            while (size > (1 << 10))
            {
                size >>= 10;
                index++;
            }

            return size + unit[index];
        }
    }
}

#endif