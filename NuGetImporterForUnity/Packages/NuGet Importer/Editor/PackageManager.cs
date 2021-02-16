#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;
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
        private static readonly List<string> linuxName = new List<string>() { "linux", "ubuntu", "centos", "debian" };

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages installed;

        /// <value>
        /// <para>For Test.</para>
        /// </value>
        internal static InstalledPackages rootPackage;

        /// <value>
        /// <para>Ctalogs of installed packages.</para>
        /// <para>インストールされているパッケージのカタログ。</para>
        /// </value>
        internal static Dictionary<string, Catalog> installedCatalog = new Dictionary<string, Catalog>();

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
                        EditorUtility.DisplayDialog("Nuget importer", "Installation finished.", "OK");
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
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task InstallPackage(string packageId, string version, bool onlyStable)
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
                List<Package> requiredPackages = await DependencySolver.FindRequiredPackages(packageId, version, onlyStable);
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
                Package[] rootPackages = requiredPackages.Where(package => package.id == packageId).ToArray();
                if (rootPackage.package != null && rootPackage.package.Length > 0)
                {
                    rootPackages = rootPackage.package.Append(rootPackages[0]).ToArray();
                }


                if (!EditorUtility.DisplayDialog("NuGet importer", "Install or upgrade / downgrade below packages\n\n" + string.Join("\n", installPackages.Select(package => package.id + " " + package.version)), "Install", "Cancel"))
                {
                    return;
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
                                    return;
                                case 2:
                                    Help.BrowseURL(catalogEntry.licenseUrl);
                                    if (!EditorUtility.DisplayDialog("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel"))
                                    {
                                        return;
                                    }
                                    break;
                            }
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing packages before upgrade.", 0.25f);

                if (nativePackages.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the deleting package. You need to restart the editor to repair packages.\n(The current project will be saved and repair packages will be resumed after a restart.)", "Restart", "Quit"))
                    {
                        OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    }
                    else
                    {
                        return;
                    }
                }

                if (samePackages != null && samePackages.Any())
                {
                    foreach (Package samePackage in samePackages)
                    {
                        UninstallPackage(samePackage);
                    }
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages", 0.5f);

                if (installPackages != null && installPackages.Any())
                {
                    foreach (Package requiredPackage in installPackages)
                    {
                        tasks.Add(InstallSelectPackage(requiredPackage));
                    }
                }
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
        /// <para>Task</para>
        /// </returns>
        public static async Task FixPackage(string packageId)
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
                        OperateWithNative(new Package[] { fix }, new Package[0], new Package[] { fix }, installed.package, rootPackage.package);
                    }
                    else
                    {
                        return;
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
        }

        /// <summary>
        /// <para>Optimize and repair the package.</para>
        /// <para>パッケージの依存関係等を最適化し、修復する。</para>
        /// </summary>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task FixPackage()
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
                var onlyStable = !installed.package.Any(package => package.version.Contains('-') || package.version[0] == '0');
                List<Package> requiredPackages = await DependencySolver.CheckAllPackage(onlyStable);
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
                var rootPackages = new Package[0];
                if (rootPackage != null && rootPackage.package != null && rootPackage.package.Length != 0)
                {
                    rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();
                }
                if (!deletePackages.Any() && !installPackages.Any())
                {
                    EditorUtility.DisplayDialog("NuGet importer", "Packages are already installed are optimized.", "OK");
                    return;
                }

                if (!EditorUtility.DisplayDialog("NuGet importer", "Uninstalling below packages\n\n" + string.Join("\n", uninstallPackages.Select(package => package.id + " " + package.version)) + "\n\nInstall or upgrade / downgrade below packages\n\n" + string.Join("\n", installPackages.Select(package => package.id + " " + package.version)), "Go", "Cancel"))
                {
                    return;
                }

                foreach (Package installPackage in installPackages)
                {
                    var isInstalled = false;
                    lock (installedCatalog)
                    {
                        isInstalled = installedCatalog.ContainsKey(installPackage.id);
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
                                return;
                            case 2:
                                Help.BrowseURL(catalogEntry.licenseUrl);
                                if (!EditorUtility.DisplayDialog("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel"))
                                {
                                    return;
                                }
                                break;
                        }
                    }
                }


                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.33f);

                if (nativePackages.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the deleting package. You need to restart the editor to repair packages.\n(The current project will be saved and repair packages will be resumed after a restart.)", "Restart", "Quit"))
                    {
                        OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    }
                    else
                    {
                        return;
                    }
                }

                foreach (Package deletePackage in deletePackages)
                {
                    UninstallPackage(deletePackage);
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages.", 0.33f);
                foreach (Package requiredPackage in requiredPackages)
                {
                    tasks.Add(InstallSelectPackage(requiredPackage));
                }
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
        }

        /// <summary>
        /// <para>Uninstall the specified package.</para>
        /// <para>指定したパッケージをアンインストールする。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id.</para>
        /// <para>ッケージのid。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <returns>
        /// <para>Task</para>
        /// </returns>
        public static async Task UninstallPackages(string packageId, bool onlyStable)
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
                List<Package> uninstallPackages = await DependencySolver.FindRemovablePackages(packageId, onlyStable);
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
                    return;
                }
                if (!EditorUtility.DisplayDialog("NuGet importer", "Uninstalling below packages\n\n" + string.Join("\n", uninstallPackages.Select(package => package.id + " " + package.version)), "Uninstall", "Cancel"))
                {
                    return;
                }
                EditorUtility.DisplayProgressBar("NuGet importer", "Uninstalling packges", 0.5f);

                if (nativePackages.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the deleting package. You need to restart the editor to delete package.\n(The current project will be saved.)", "Restart", "Quit"))
                    {
                        OperateWithNative(new Package[0], managedPackages, nativePackages, installedPackages, rootPackages);
                    }
                    else
                    {
                        return;
                    }
                }

                foreach (Package uninstallPackage in uninstallPackages)
                {
                    UninstallPackage(uninstallPackage);
                }
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
        /// <returns>
        /// <para>ask</para>
        /// </returns>
        public static async Task ChangePackageVersion(string packageId, string newVersion, bool onlyStable)
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
                List<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(packageId, newVersion, onlyStable);
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
                Package[] rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();
                if (!EditorUtility.DisplayDialog("NuGet importer", "Uninstalling below packages\n\n" + string.Join("\n", uninstallPackages.Select(package => package.id + " " + package.version)) + "\n\nInstall or upgrade / downgrade below packages\n\n" + string.Join("\n", installPackages.Select(package => package.id + " " + package.version)), "Install", "Cancel"))
                {
                    return;
                }

                if (requiredPackages != null && requiredPackages.Any())
                {
                    foreach (Package requiredPackage in requiredPackages)
                    {
                        var isInstalled = false;
                        lock (installedCatalog)
                        {
                            isInstalled = installedCatalog.ContainsKey(requiredPackage.id);
                        }
                        Catalog catalog = isInstalled ? installedCatalog[requiredPackage.id] : await NuGet.GetCatalog(requiredPackage.id);
                        Catalogentry catalogEntry = catalog.GetAllCatalogEntry().First(entry => entry.version == requiredPackage.version);
                        if (catalogEntry.requireLicenseAcceptance)
                        {
                            var option = EditorUtility.DisplayDialogComplex("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel", "Go url");
                            switch (option)
                            {
                                case 0:
                                    break;
                                case 1:
                                    return;
                                case 2:
                                    Help.BrowseURL(catalogEntry.licenseUrl);
                                    if (!EditorUtility.DisplayDialog("NuGet importer", catalogEntry.id + " " + catalogEntry.version + " need agree license.\nUrl : " + catalogEntry.licenseUrl, "Agree", "Cancel"))
                                    {
                                        return;
                                    }
                                    break;
                            }
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Removing unnecessary packages.", 0.25f);

                if (nativePackages.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("NuGet importer", "Native plugins were found in the deleting package. You need to restart the editor to change the version.\n(The current project will be saved and version changes will be resumed after a restart.)", "Restart", "Quit"))
                    {
                        OperateWithNative(installPackages, managedPackages, nativePackages, requiredPackages, rootPackages);
                    }
                    else
                    {
                        return;
                    }
                }

                foreach (Package delete in deletePackages)
                {
                    UninstallPackage(delete);
                }

                EditorUtility.DisplayProgressBar("NuGet importer", "Installing packages.", 0.5f);
                foreach (Package requiredPackage in requiredPackages)
                {
                    tasks.Add(InstallSelectPackage(requiredPackage));
                }
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
        }

        private static async Task InstallSelectPackage(Package package)
        {
            var topDirectory = Path.Combine(Application.dataPath, "Packages");
            var topNupkg = Path.Combine(topDirectory, package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant() + ".nupkg");
            var packageDirectory = Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
            var packageNupkg = Path.Combine(packageDirectory, package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant() + ".nupkg");

            if (!Directory.Exists(Path.Combine(Application.dataPath, "Packages")))
            {
                Directory.CreateDirectory(topDirectory);
            }

            if (File.Exists(packageNupkg))
            {
                File.Move(packageNupkg, topNupkg);
            }
            else
            {
                await NuGet.GetPackage(package.id, package.version, topDirectory);
            }

            DeleteDirectory(packageDirectory);
            ZipFile.ExtractToDirectory(topNupkg, packageDirectory);
            File.Move(topNupkg, packageNupkg);

            DeleteDirectory(Path.Combine(packageDirectory, "_rels"));
            DeleteDirectory(Path.Combine(packageDirectory, "ref"));
            DeleteDirectory(Path.Combine(packageDirectory, "package"));
            DeleteDirectory(Path.Combine(packageDirectory, "build"));
            DeleteDirectory(Path.Combine(packageDirectory, "buildMultiTargeting"));
            DeleteDirectory(Path.Combine(packageDirectory, "buildTransitive"));
            DeleteDirectory(Path.Combine(packageDirectory, "tools"));

            foreach (var file in Directory.GetFiles(packageDirectory))
            {
                if (file.Contains(".nuspec"))
                {
                    File.Delete(file);
                }
                if (file.Contains("[Content_Types].xml"))
                {
                    File.Delete(file);
                }
            }

            List<string[]> frameworkDictionary = FrameworkName.ALLPLATFORM;
            var targetFramework = frameworkDictionary.Where(framework => framework.Contains(package.targetFramework)).FirstOrDefault();
            List<string> frameworkList;
            switch (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
            {
                case ApiCompatibilityLevel.NET_4_6:
                    frameworkList = FrameworkName.NET;
                    break;
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    frameworkList = FrameworkName.STANDARD;
                    break;
                default:
                    throw new NotSupportedException("Now this is only suppoort .Net4.x equivalent");
            }
            if (Directory.Exists(Path.Combine(packageDirectory, "lib")))
            {
                var deleteList = new List<string>();
                var target = "";
                var priority = int.MaxValue;
                foreach (var lib in Directory.GetDirectories(Path.Combine(packageDirectory, "lib")))
                {
                    var dirName = Path.GetFileName(lib);

                    if (targetFramework != default && targetFramework.Contains(dirName))
                    {
                        if (target != "")
                        {
                            deleteList.Add(target);
                        }
                        priority = -1;
                        target = lib;
                        package.targetFramework = frameworkDictionary.Where(framework => framework.Contains(dirName)).First()[0];
                    }
                    else if (frameworkList.Contains(dirName) && frameworkList.IndexOf(dirName) < priority)
                    {
                        if (target != "")
                        {
                            deleteList.Add(target);
                        }
                        priority = frameworkList.IndexOf(dirName);
                        target = lib;
                        package.targetFramework = frameworkDictionary.Where(framework => framework.Contains(dirName)).First()[0];
                    }
                    else
                    {
                        deleteList.Add(lib);
                    }
                }
                foreach (var del in deleteList)
                {
                    DeleteDirectory(del);
                }

                if (Directory.GetDirectories(Path.Combine(packageDirectory, "lib")).Length == 0)
                {
                    DeleteDirectory(Path.Combine(packageDirectory, "lib"));
                }
            }


            if (Directory.Exists(Path.Combine(packageDirectory, "runtimes")))
            {
                var deleteList = new List<string>();
                var target = "";
                var priority = int.MaxValue;
                foreach (var runtime in Directory.GetDirectories(Path.Combine(packageDirectory, "runtimes")))
                {
                    DeleteDirectory(Path.Combine(runtime, "lib"));

                    var dirName = Path.GetFileName(runtime);
                    if (dirName.StartsWith("win"))
                    {
                        if (!dirName.EndsWith("x86") && !dirName.EndsWith("64"))
                        {
                            deleteList.Add(runtime);
                        }
                        else
                        {
                            deleteList.AddRange(Directory.GetDirectories(runtime).Where(path => !path.EndsWith("native")));
                        }
                    }
                    else if (dirName == "osx-x64")
                    {
                        deleteList.AddRange(Directory.GetDirectories(runtime).Where(path => !path.EndsWith("native")));
                    }
                    else
                    {
                        IEnumerable<string> osName = linuxName.Where(linux => dirName.StartsWith(linux));
                        if (osName != null && osName.Any() && linuxName.IndexOf(osName.First()) < priority)
                        {
                            if (target != "")
                            {
                                deleteList.Add(target);
                            }
                            priority = linuxName.IndexOf(osName.First());
                            target = runtime;
                        }
                        else
                        {
                            deleteList.Add(runtime);
                        }
                    }
                }
                if (target != "" && Directory.GetDirectories(target).Where(path => !path.EndsWith("native")).Any())
                {
                    deleteList.AddRange(Directory.GetDirectories(target).Where(path => !path.EndsWith("native")));
                }
                foreach (var delete in deleteList)
                {
                    DeleteDirectory(delete);
                }
                if (Directory.GetDirectories(Path.Combine(packageDirectory, "runtimes")).Length == 0)
                {
                    DeleteDirectory(Path.Combine(packageDirectory, "runtimes"));
                }
            }

            Catalog catalog = await NuGet.GetCatalog(package.id);
            lock (installedCatalog)
            {
                installedCatalog[package.id] = catalog;
            }
        }

        private static void OperateWithNative(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root)
        {
            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillInstall.xml"), false, Encoding.UTF8))
            {
                var write = new InstalledPackages
                {
                    package = installs.ToArray()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillPackage.xml"), false, Encoding.UTF8))
            {
                var write = new InstalledPackages
                {
                    package = allInstalled.ToArray()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillRoot.xml"), false, Encoding.UTF8))
            {
                var write = new InstalledPackages
                {
                    package = root.ToArray()
                };
                serializer.Serialize(file, write);
            }

            foreach (Package managed in manageds)
            {
                UninstallPackage(managed);
            }

            installed.package = installed.package.Where(package => !manageds.Any(manage => manage.id == package.id) && !natives.Any(native => native.id == package.id)).ToArray();
            rootPackage.package = rootPackage.package.Where(package => installed.package.Any(installed => installed.id == package.id)).ToArray();
            Save();
            DeleteNative(natives.Select(package => Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant())));
        }

        private static void DeleteNative(IEnumerable<string> paths)
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            var command = new StringBuilder();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                process.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
                command.Append("/c timeout 5 && ");
                foreach (var path in paths)
                {
                    command.Append("rd /s /q \"");
                    command.Append(path);
                    command.Append("\"");
                    command.Append(" && ");
                    command.Append("del \"");
                    command.Append(path);
                    command.Append(".meta");
                    command.Append("\"");
                    command.Append(" && ");
                }
                command.Append(Environment.CommandLine);
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";
                command.Append("-c \" sleep 5 && ");
                foreach (var path in paths)
                {
                    command.Append("rm -rf '");
                    command.Append(path);
                    command.Append("'");
                    command.Append(" && ");
                    command.Append("rm -f '");
                    command.Append(path);
                    command.Append(".meta");
                    command.Append("'");
                    command.Append(" && ");
                }
                command.Append(Environment.CommandLine);
                command.Append("\"");
            }
            process.StartInfo.Arguments = command.ToString();
            process.Start();
            EditorApplication.Exit(0);
        }

        private static bool HasNative(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                var plugin = AssetImporter.GetAtPath(Path.Combine("Assets", file.Replace(Application.dataPath, "").Substring(1))) as PluginImporter;
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

        private static void UninstallPackage(Package package)
        {
            DeleteDirectory(Path.Combine(Application.dataPath, "Packages", package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant()));
            lock (installed)
            {
                installed.package = installed.package.Where(pkg => pkg.id != package.id).ToArray();
            }
            lock (installedCatalog)
            {
                installedCatalog.Remove(package.id);
            }
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
                File.Delete(path + ".meta");
            }
            catch (Exception e) when (e is ArgumentException || e is DirectoryNotFoundException || e is FileNotFoundException || e is NotSupportedException)
            {

            }
        }
    }
}

#endif