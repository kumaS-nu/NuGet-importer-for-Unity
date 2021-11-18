#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

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
        private static ManagedPluginList managedPluginList;
        private static readonly List<string> linuxName = new List<string>() { "linux", "ubuntu", "centos", "debian" };
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(InstalledPackages));

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages installed;

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages rootPackage;

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
        /// <para>List of managed plugins.</para>
        /// <para>マネージドプラグインのリスト。</para>
        /// </summary>
        internal static ManagedPluginList ManagedPluginList { get => managedPluginList; }

        /// <summary>
        /// <para>Save the package installation information.</para>
        /// <para>パッケージのインストール情報を保存する。</para>
        /// </summary>
        public static void Save()
        {
            using (var file = new StreamWriter(Path.Combine(Application.dataPath, "packages.config"), false, Encoding.UTF8))
            {
                serializer.Serialize(file, installed);
            }

            using (var file = new StreamWriter(Path.Combine(Application.dataPath, "rootPackages.xml"), false, Encoding.UTF8))
            {
                serializer.Serialize(file, rootPackage);
            }

            if (!Directory.Exists(Path.Combine(Application.dataPath, "Packages")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Packages"));
            }
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
            if (File.Exists(Path.Combine(Application.dataPath, "packages.config")))
            {
                using (var file = new StreamReader(Path.Combine(Application.dataPath, "packages.config")))
                {
                    installed = (InstalledPackages)serializer.Deserialize(file);
                }
            }
            else
            {
                installed = new InstalledPackages();
            }

            if (File.Exists(Path.Combine(Application.dataPath, "rootPackages.xml")))
            {
                using (var file = new StreamReader(Path.Combine(Application.dataPath, "rootPackages.xml")))
                {
                    rootPackage = (InstalledPackages)serializer.Deserialize(file);
                }
            }
            else
            {
                rootPackage = new InstalledPackages();
            }

            // Processing when rebooting to delete natives.
            if (File.Exists(Application.dataPath.Replace("Assets", "WillInstall.xml")))
            {
                if (!File.Exists(Application.dataPath.Replace("Assets", "WillPackage.xml")) || !File.Exists(Application.dataPath.Replace("Assets", "WillRoot.xml")))
                {
                    EditorUtility.DisplayDialog("Nuget importer", "Installation failed.\nThe required file is missing.", "OK");
                }
                else
                {
                    working = true;
                    EditorApplication.LockReloadAssemblies();
                    try
                    {
                        EditorUtility.DisplayProgressBar("Nuget importer", "Installing packages", 0.25f);
                        InstalledPackages install = default;
                        using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillInstall.xml")))
                        {
                            install = (InstalledPackages)serializer.Deserialize(file);
                        }
                        var tasks = new List<Task>();
                        if (install.package != null)
                        {
                            foreach (Package package in install.package)
                            {
                                tasks.Add(InstallSelectPackage(package));
                            }
                        }
                        await Task.WhenAll(tasks);
                        using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillPackage.xml")))
                        {
                            installed = (InstalledPackages)serializer.Deserialize(file);
                        }
                        using (var file = new StreamReader(Application.dataPath.Replace("Assets", "WillRoot.xml")))
                        {
                            rootPackage = (InstalledPackages)serializer.Deserialize(file);
                        }
                        EditorUtility.ClearProgressBar();
                        if (tasks.Any())
                        {
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
                        EditorApplication.UnlockReloadAssemblies();
                        working = false;
                        EditorUtility.ClearProgressBar();
                        Save();
                        AssetDatabase.Refresh();
                    }
                }
            }

            if (File.Exists(Application.dataPath.Replace("Assets", "WillPackage.xml")))
            {
                File.Delete(Application.dataPath.Replace("Assets", "WillPackage.xml"));
            }

            if (File.Exists(Application.dataPath.Replace("Assets", "WillRoot.xml")))
            {
                File.Delete(Application.dataPath.Replace("Assets", "WillRoot.xml"));
            }

            if (installed.package != null)
            {
                var tasks = new List<Task>();
                foreach (Package package in installed.package)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        Catalog catalog = await NuGet.GetCatalog(package.id);
                        lock (installedCatalog)
                        {
                            installedCatalog.Add(package.id, catalog);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                var tasks = new List<Task>();
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                // Find out the packages that need to be changed.
                List<Package> requiredPackages = await DependencySolver.FindRequiredPackages(packageId, version, onlyStable, method);
                Package[] installPackages = requiredPackages.Where(package => { if (installed.package == null) { return true; } return !installed.package.Any(install => install.id == package.id && install.version == package.version); }).ToArray();
                Package[] samePackages = installed.package == null ? new Package[0] : installed.package.Where(install => requiredPackages.Any(dep => dep.id == install.id && dep.version != install.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                if (samePackages != null)
                {
                    foreach (Package package in samePackages)
                    {
                        if (HasNative(Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant())))
                        {
                            nativePackages.Add(package);
                        }
                        else
                        {
                            managedPackages.Add(package);
                        }
                    }
                }

                if (!await ConfirmToUser(installPackages, default, nativePackages))
                {
                    return false;
                }

                Package[] rootPackages = requiredPackages.Where(package => package.id == packageId).ToArray();
                if (rootPackage.package != null && rootPackage.package.Length > 0)
                {
                    rootPackages = rootPackage.package.Append(rootPackages[0]).ToArray();
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing packages before upgrade.", 0.25f);
                if (nativePackages.Any())
                {
                    var process = OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    process.Start();
                    EditorApplication.Exit(0);
                }

                if (samePackages != null && samePackages.Any())
                {
                    await UninstallPackages(samePackages);
                }

                if (installPackages != null && installPackages.Any())
                {
                    foreach (Package requiredPackage in installPackages)
                    {
                        tasks.Add(InstallSelectPackage(requiredPackage));
                    }
                }

                _ = DownloadProgress(0.5f, requiredPackages.Select(package => package.id).ToArray());
                await Task.WhenAll(tasks);
                installed.package = requiredPackages.ToArray();
                if (rootPackage.package != null)
                {
                    rootPackage.package = rootPackage.package.Append(requiredPackages.Where(package => package.id == packageId).First()).ToArray();
                }
                else
                {
                    rootPackage.package = requiredPackages.Where(package => package.id == packageId).ToArray();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Reinstalling package", 0);
                if (installed == null || installed.package == null)
                {
                    throw new ArgumentException(packageId + " is not installed.");
                }
                IEnumerable<Package> fixPackage = installed.package.Where(package => package.id == packageId);
                if (!fixPackage.Any())
                {
                    throw new ArgumentException(packageId + " is not installed.");
                }
                Package fix = fixPackage.First();
                if (HasNative(Path.Combine(Application.dataPath, "Packages", fix.id.ToLowerInvariant() + "." + fix.version.ToLowerInvariant())))
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the repair package. You need to restart the editor to repair packages.\n(The current project will be saved and repair packages will be resumed after a restart.)", "Restart", "Quit"))
                    {
                        var process = OperateWithNative(new Package[] { fix }, new Package[0], new Package[] { fix }, installed.package, rootPackage.package);
                        process.Start();
                        EditorApplication.Exit(0);
                    }
                    else
                    {
                        return false;
                    }
                }
                await InstallSelectPackage(fix);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);
                var tasks = new List<Task>();
                if (installed == null || installed.package == null || installed.package.Length == 0)
                {
                    throw new InvalidOperationException("No packages installed.");
                }

                // Find out the packages that need to be changed.
                List<Package> requiredPackages = await DependencySolver.CheckAllPackage(onlyStable, method);
                Package[] deletePackages = installed.package.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();
                Package[] uninstallPackages = deletePackages.Where(package => !installed.package.Any(install => install.id == package.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in deletePackages)
                {
                    if (HasNative(Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant())))
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

                var rootPackages = new Package[0];
                if (rootPackage != null && rootPackage.package != null && rootPackage.package.Length != 0)
                {
                    rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.33f);

                if (nativePackages.Any())
                {
                    var process = OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    process.Start();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(deletePackages);

                foreach (Package requiredPackage in requiredPackages)
                {
                    tasks.Add(InstallSelectPackage(requiredPackage));
                }

                _ = DownloadProgress(0.33f, requiredPackages.Select(package => package.id).ToArray());
                await Task.WhenAll(tasks);
                installed.package = requiredPackages.ToArray();
                if (rootPackage == null || rootPackage.package == null)
                {
                    rootPackage = new InstalledPackages();
                }
                else
                {
                    rootPackage.package = rootPackages;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                if (installed == null || installed.package == null || installed.package.Length == 0)
                {
                    throw new InvalidOperationException("No packages installed.");
                }
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                // Find out the packages that need to be changed.
                List<Package> uninstallPackages = await DependencySolver.FindRemovablePackages(packageId, onlyStable, method);
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in uninstallPackages)
                {
                    if (HasNative(Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant())))
                    {
                        nativePackages.Add(package);
                    }
                    else
                    {
                        managedPackages.Add(package);
                    }
                }
                Package[] installedPackages = installed.package.Where(package => !uninstallPackages.Any(uninstall => uninstall.id == package.id)).ToArray();
                var rootPackages = new Package[0];
                if (rootPackage != null && rootPackage.package != null && rootPackage.package.Length != 0)
                {
                    rootPackages = installedPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();
                }

                if (uninstallPackages == null || !uninstallPackages.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "Selected package is depended by other package.", "OK");
                    return true;
                }

                if (!await ConfirmToUser(default, uninstallPackages, nativePackages))
                {
                    return false;
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Uninstalling packges", 0.5f);

                if (nativePackages.Any())
                {
                    var process = OperateWithNative(new Package[0], managedPackages, nativePackages, installedPackages, rootPackages);
                    process.Start();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(uninstallPackages);

                installed.package = installedPackages;
                if (rootPackage == null || rootPackage.package == null)
                {
                    rootPackage = new InstalledPackages();
                }
                else
                {
                    rootPackage.package = rootPackages;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);

                var tasks = new List<Task>();

                // Find out the packages that need to be changed.
                List<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(packageId, newVersion, onlyStable, method);
                Package[] deletePackages = installed.package.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();
                Package[] uninstallPackages = deletePackages.Where(package => !installed.package.Any(install => install.id == package.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                var nativePackages = new List<Package>();
                var managedPackages = new List<Package>();
                foreach (Package package in deletePackages)
                {
                    if (HasNative(Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant())))
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

                Package[] rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.25f);

                if (nativePackages.Any())
                {
                    var process = OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    process.Start();
                    EditorApplication.Exit(0);
                }

                await UninstallPackages(deletePackages);

                foreach (Package requiredPackage in requiredPackages)
                {
                    tasks.Add(InstallSelectPackage(requiredPackage));
                }

                _ = DownloadProgress(0.5f, requiredPackages.Select(package => package.id).ToArray());
                await Task.WhenAll(tasks);
                installed.package = requiredPackages.ToArray();
                rootPackage.package = rootPackages;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Checking packages", 0.1f);
                var controller = new PackageControllerAsAsset();
                var tasks = installed.package.Select(async pkg => {
                    var path = await controller.GetInstallPath(pkg);
                    return HasNative(path);
                });

                var isNatives = await Task.WhenAll(tasks);
                EditorUtility.DisplayProgressBar("NuGet importer", "Deleting packages", 0.4f);
                if (isNatives.Any(isNative => isNative))
                {
                    var process = await controller.OperateWithNativeAsync(installed.package, new Package[0], installed.package, installed.package, rootPackage.package);
                    try
                    {
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(Application.dataPath, "Packages", "managedPluginList.json.meta"));
                    }
                    catch (Exception) { }
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

                    EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages", 0.5f);

                    var installer = new PackageControllerAsUPM();
                    var tasks2 = new List<Task>();
                    foreach(var pkg in installed.package)
                    {
                        tasks2.Add(installer.InstallPackageAsync(pkg));
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
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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
            EditorApplication.LockReloadAssemblies();
            try
            {
                EditorUtility.DisplayProgressBar("NuGet importer", "Checking packages", 0.1f);
                var controller = new PackageControllerAsUPM();
                var tasks = installed.package.Select(async pkg => {
                    var path = await controller.GetInstallPath(pkg);
                    return HasNative(path);
                });

                var isNatives = await Task.WhenAll(tasks);
                EditorUtility.DisplayProgressBar("NuGet importer", "Deleting packages", 0.4f);
                if (isNatives.Any(isNative => isNative))
                {
                    var process = await controller.OperateWithNativeAsync(installed.package, new Package[0], installed.package, installed.package, rootPackage.package);
                    process.Start();
                    EditorApplication.Exit(0);
                }
                else
                {
                    await controller.UninstallManagedPackagesAsync(installed.package);
                    EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages", 0.5f);

                    var installer = new PackageControllerAsAsset();
                    var tasks2 = new List<Task>();
                    foreach (var pkg in installed.package)
                    {
                        tasks2.Add(installer.InstallPackageAsync(pkg));
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
                EditorApplication.UnlockReloadAssemblies();
                working = false;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.Refresh();
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

        private static async Task InstallSelectPackage(Package package)
        {
            var controller = GetPackageController();
            await controller.InstallPackageAsync(package);
            Catalog catalog = await NuGet.GetCatalog(package.id);
            lock (installedCatalog)
            {
                installedCatalog[package.id] = catalog;
            }
        }

        private static async Task<Process> OperateWithNative(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root)
        {
            var controller = GetPackageController();
            var process = await controller.OperateWithNativeAsync(installs, manageds, natives, allInstalled, root);
            installed.package = installed.package.Where(package => !manageds.Any(manage => manage.id == package.id) && !natives.Any(native => native.id == package.id)).ToArray();
            rootPackage.package = rootPackage.package.Where(package => installed.package.Any(installed => installed.id == package.id)).ToArray();
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
                    foreach (var package in packages)
                    {
                        installed.package = installed.package.Where(pkg => pkg.id != package.id).ToArray();
                        installedCatalog.Remove(package.id);
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

        private static bool HasNative(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                var plugin = AssetImporter.GetAtPath(file.Replace(Application.dataPath.Replace("Assets", ""), "").Substring(1)) as PluginImporter;
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
                if (HasNative(directory))
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