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
        private static string dataPath;
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(InstalledPackages));

        private static string ProjectSettingsPath { get => DataPath.Replace("Assets", "ProjectSettings"); }
        private static string PackagePath { get => Path.Combine(DataPath, "packages.config"); }
        private static string RootPackagePath { get => Path.Combine(ProjectSettingsPath, "rootPackages.xml"); }
        private static string ExistingPackagePath { get => Path.Combine(ProjectSettingsPath, "existingPackages.xml"); }
        private static string PackageAsmNamesPath { get => Path.Combine(ProjectSettingsPath, "packageAsmNames.json"); }

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
        /// <para>Thread-safe Application.dataPath.</para>
        /// <para>スレッドセーフなApplication.dataPath。</para>
        /// </summary>
        public static string DataPath { get => dataPath; }

        /// <summary>
        /// <para>Save the package installation information.</para>
        /// <para>パッケージのインストール情報を保存する。</para>
        /// </summary>
        public static void Save()
        {
            using (var file = new StreamWriter(PackagePath, false))
            {
                serializer.Serialize(file, installed);
            }

            using (var file = new StreamWriter(RootPackagePath, false))
            {
                serializer.Serialize(file, rootPackage);
            }

            using (var file = new StreamWriter(ExistingPackagePath, false))
            {
                serializer.Serialize(file, existingPackage);
            }

            File.WriteAllText(PackageAsmNamesPath, JsonUtility.ToJson(packageAsmNames, true));
        }

        /// <summary>
        /// <para>Load the package installation information.</para>
        /// <para>パッケージのインストール情報を読み込む。</para>
        /// </summary>
        public static void Load()
        {
            if (File.Exists(PackagePath))
            {
                using (var file = new StreamReader(PackagePath))
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

            if (File.Exists(Path.Combine(dataPath, "rootPackages.xml")))
            {
                using (var file = new StreamReader(Path.Combine(dataPath, "rootPackages.xml")))
                {
                    rootPackage = (InstalledPackages)serializer.Deserialize(file);
                }
                try
                {
                    File.Move(Path.Combine(dataPath, "rootPackages.xml"), RootPackagePath);
                    File.Delete(Path.Combine(dataPath, "rootPackages.xml.meta"));
                }
                catch (Exception) { }
            }
            else if (File.Exists(RootPackagePath))
            {
                using (var file = new StreamReader(RootPackagePath))
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

            if (File.Exists(ExistingPackagePath))
            {
                using (var file = new StreamReader(ExistingPackagePath))
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

            if (File.Exists(PackageAsmNamesPath))
            {
                packageAsmNames = JsonUtility.FromJson<ManagedPluginList>(File.ReadAllText(PackageAsmNamesPath));
            }

            if (packageAsmNames == null)
            {
                packageAsmNames = new ManagedPluginList();
            }

            if (packageAsmNames.managedList == null)
            {
                packageAsmNames.managedList = new List<PackageManagedPluginList>();
            }
        }

        [InitializeOnLoadMethod]
        public static async Task Boot()
        {
            dataPath = Application.dataPath;

            await RebootProcessAsync();

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

        private static async Task RebootProcessAsync()
        {
            if (!File.Exists(DataPath.Replace("Assets", "WillInstall.xml")))
            {
                if (NuGetImporterSettings.Instance.AutoPackagePlacementCheck)
                {
                    try
                    {
                        if (await IsNeedToFix())
                        {
                            if (EditorUtility.DisplayDialog("NuGet importer", "We find packages that are not installed. Do you install these packages?", "Yes", "No"))
                            {
                                var fixResult = await FixPackagesAsync(false);
                                EditorUtility.DisplayDialog("NuGet importer", fixResult.Message, "OK");
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    { }
                }

                Load();
                return;
            }
            var operation = new RebootProcess();
            var result = await operation.Operate();

            if (NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsUPM)
            {
                DeleteAsAssetDirectory();
            }

            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }

        internal class RebootProcess : OperatePackage
        {
            protected override string FinishMessage { get => "The package operation finished."; }

            public RebootProcess()
            {
                isConfirmToUser = false;
            }

            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                InstalledPackages install;
                using (var file = new StreamReader(DataPath.Replace("Assets", "WillInstall.xml")))
                {
                    install = (InstalledPackages)serializer.Deserialize(file);
                }

                if (install == null || install.package == null || !install.package.Any())
                {
                    File.Delete(DataPath.Replace("Assets", "WillInstall.xml"));
                    File.Delete(DataPath.Replace("Assets", "WillPackage.xml"));
                    File.Delete(DataPath.Replace("Assets", "WillRoot.xml"));
                    if (File.Exists(DataPath.Replace("Assets", "RollBackPackage.xml")))
                    {
                        File.Delete(DataPath.Replace("Assets", "RollBackPackage.xml"));
                        File.Delete(DataPath.Replace("Assets", "RollBackRoot.xml"));
                    }
                    return (new OperationResult(OperationState.Success, "Uninstallation finished."), default, default, default);
                }

                InstalledPackages root;
                using (var file = new StreamReader(DataPath.Replace("Assets", "WillRoot.xml")))
                {
                    root = (InstalledPackages)serializer.Deserialize(file);
                }

                if (root == null)
                {
                    root = new InstalledPackages();
                }
                if (root.package == null)
                {
                    root.package = new Package[0];
                }

                File.Delete(DataPath.Replace("Assets", "WillInstall.xml"));
                File.Delete(DataPath.Replace("Assets", "WillPackage.xml"));
                File.Delete(DataPath.Replace("Assets", "WillRoot.xml"));

                IEnumerable<Package> deletePackages = new List<Package>();
                InstalledPackages rollBackPackage = default;
                if (File.Exists(DataPath.Replace("Assets", "RollBackPackage.xml")))
                {
                    using (var file = new StreamReader(DataPath.Replace("Assets", "RollBackPackage.xml")))
                    {
                        rollBackPackage = (InstalledPackages)serializer.Deserialize(file);
                    }
                    File.Delete(DataPath.Replace("Assets", "RollBackPackage.xml"));

                    if (rollBackPackage == null)
                    {
                        rollBackPackage = new InstalledPackages();
                    }
                    if (rollBackPackage.package == null)
                    {
                        rollBackPackage.package = new Package[0];
                    }

                    deletePackages = rollBackPackage.package.Where(rollback => !installed.package.Any(pkg => pkg.id == rollback.id)).ToArray();
                    installed = rollBackPackage;
                }

                InstalledPackages rollBackRoot = default;
                if (File.Exists(DataPath.Replace("Assets", "RollBackRoot.xml")))
                {
                    using (var file = new StreamReader(DataPath.Replace("Assets", "RollBackRoot.xml")))
                    {
                        rollBackRoot = (InstalledPackages)serializer.Deserialize(file);
                    }
                    File.Delete(DataPath.Replace("Assets", "RollBackRoot.xml"));

                    if (rollBackRoot == null)
                    {
                        rollBackRoot = new InstalledPackages();
                    }
                    if (rollBackRoot.package == null)
                    {
                        rollBackRoot.package = new Package[0];
                    }

                    root = rollBackRoot;
                }

                return (new OperationResult(OperationState.Progress, ""), root.package, install.package, deletePackages);
            }
        }

        private static void DeleteAsAssetDirectory()
        {
            try
            {
                var dirs = Directory.GetDirectories(Path.Combine(DataPath, "Packages"));
                var files = Directory.GetFiles(Path.Combine(DataPath, "Packages"));
                if (dirs.Length != files.Length)
                {
                    return;
                }
                if (dirs.Length != 0 && Path.GetFileName(dirs[0]) != "Plugins")
                {
                    return;
                }
                Directory.Delete(Path.Combine(DataPath, "Packages"), true);
                File.Delete(Path.Combine(DataPath, "Packages.meta"));
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
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static Task<OperationResult> InstallPackageAsync(string packageId, string version, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operation = new InstallPackage(packageId, version, onlyStable, method);
            return operation.Operate();
        }

        /// <summary>
        /// <para>Install the package.</para>
        /// <para>パッケージをインストールする。</para>
        /// </summary>
        internal class InstallPackage : OperatePackage
        {
            private readonly string id;
            private readonly string version;
            private readonly bool onlyStable = true;
            private readonly VersionSelectMethod method = VersionSelectMethod.Suit;

            protected override string FinishMessage { get => "Installation finished."; }

            public InstallPackage(string packageId, string installVersion, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
            {
                onlyStable = isOnlyStable;
                method = versionSelect;
                id = packageId;
                version = installVersion;
            }

            /// <inheritdoc/>
            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackages(id, version, onlyStable, method);
                requiredPackages = requiredPackages.Where(package => !existingPackage.package.Any(exist => package.id == exist.id)).ToArray();

                Package[] rootPackages = requiredPackages.Where(req => rootPackage.package.Any(root => root.id == req.id)).Concat(requiredPackages.Where(req => req.id == id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                Package[] deletePackages = installed.package.Where(install => requiredPackages.Any(dep => dep.id == install.id && dep.version != install.version)).ToArray();

                return (new OperationResult(OperationState.Progress, ""), rootPackages, installPackages, deletePackages);
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
        /// <param name="confirm">
        /// <para>Confirm to user.</para>
        /// <para>確認を行うか。</para>
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static Task<OperationResult> FixPackageAsync(string packageId, bool confirm = true)
        {
            var operateion = new FixPackage(packageId, confirm);
            return operateion.Operate();
        }

        /// <summary>
        /// <para>Repair the specified package.</para>
        /// <para>指定されたパッケージを修復する。</para>
        /// </summary>
        internal class FixPackage : OperatePackage
        {
            private readonly string id;

            protected override string FinishMessage { get => "The repair finished."; }

            public FixPackage(string packageId, bool confirm = true)
            {
                id = packageId;
                isConfirmToUser = confirm;
            }

            /// <inheritdoc/>
            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                IEnumerable<Package> fixPackage = installed.package.Where(package => package.id == id).ToArray();
                if (!fixPackage.Any())
                {
                    return (new OperationResult(OperationState.Cancel, id + " is not installed."), default, default, default);
                }

                return (new OperationResult(OperationState.Progress, ""), rootPackage.package, fixPackage, fixPackage);
            }
        }

        /// <summary>
        /// <para>Fix as follows in package.config.</para>
        /// <para>package.configの通りに修復する。</para>
        /// </summary>
        /// <param name="confirm">
        /// <para>Confirm to user.</para>
        /// <para>確認を行うか。</para>
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static Task<OperationResult> FixPackagesAsync(bool confirm = true)
        {
            var operateion = new FixPackages(confirm);
            return operateion.Operate();
        }

        /// <summary>
        /// <para>Fix as follows in package.config.</para>
        /// <para>package.configの通りに修復する。</para>
        /// </summary>
        internal class FixPackages : OperatePackage
        {
            protected override string FinishMessage { get => "The repair finished."; }

            public FixPackages(bool confirm = true)
            {
                isConfirmToUser = confirm;
            }

            /// <inheritdoc/>
            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                var isInstalled = Installed.package.Select(async pkg =>
                {
                    var controller = GetPackageController();
                    var installPath = await controller.GetInstallPath(pkg);
                    return (Directory.Exists(installPath), pkg);
                });
                var isInstalled_ = await Task.WhenAll(isInstalled);
                var notInstalled = isInstalled_.Where(b => !b.Item1).Select(b => b.pkg);

                if (!notInstalled.Any())
                {
                    return (new OperationResult(OperationState.Cancel, "No packages to repair.\n(If you want to repair the contents of a package, please repair the package individually.)"), default, default, default);
                }
                return (new OperationResult(OperationState.Progress, ""), rootPackage.package, notInstalled, notInstalled);
            }
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
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static Task<OperationResult> UninstallPackagesAsync(string packageId, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operateion = new UninstallPackages(packageId, onlyStable, method);
            return operateion.Operate();
        }

        /// <summary>
        /// <para>Uninstall the specified package.</para>
        /// <para>指定したパッケージをアンインストールする。</para>
        /// </summary>
        internal class UninstallPackages : OperatePackage
        {
            private readonly string id;
            private readonly bool onlyStable = true;
            private readonly VersionSelectMethod method = VersionSelectMethod.Suit;

            protected override string FinishMessage { get => "Uninstallation finished."; }

            public UninstallPackages(string packageId, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
            {
                id = packageId;
                onlyStable = isOnlyStable;
                method = versionSelect;
            }

            /// <inheritdoc/>
            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                if (!installed.package.Any(pkg => pkg.id == id))
                {
                    return (new OperationResult(OperationState.Cancel, "Selected package is not installed."), default, default, default);
                }

                List<Package> uninstallPackages = await DependencySolver.FindRemovablePackages(id, onlyStable, method);
                var rootPackages = installed.package.Where(package => !uninstallPackages.Any(uninstall => uninstall.id == package.id)).Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();

                if (!uninstallPackages.Any())
                {
                    return (new OperationResult(OperationState.Cancel, "Selected package is depended by other package."), default, default, default);
                }

                return (new OperationResult(OperationState.Progress, ""), rootPackages, new Package[0], uninstallPackages);
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
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static Task<OperationResult> ChangePackageVersionAsync(string packageId, string newVersion, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operateion = new ChangePackageVersion(packageId, newVersion, onlyStable, method);
            return operateion.Operate();
        }

        /// <summary>
        /// <para>Change the version of the specified package.</para>
        /// <para>指定したパッケージのバージョンを変更する。</para>
        /// </summary>
        internal class ChangePackageVersion : OperatePackage
        {
            private readonly string id;
            private readonly string version;
            private readonly bool onlyStable = true;
            private readonly VersionSelectMethod method = VersionSelectMethod.Suit;

            protected override string FinishMessage { get => "Version change finished."; }

            public ChangePackageVersion(string packageId, string installVersion, bool isOnlyStable = true, VersionSelectMethod versionSelect = VersionSelectMethod.Suit)
            {
                onlyStable = isOnlyStable;
                method = versionSelect;
                id = packageId;
                version = installVersion;
            }

            /// <inheritdoc/>
            internal protected override async Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages()
            {
                IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackagesWhenChangeVersion(id, version, onlyStable, method);

                requiredPackages = requiredPackages.Where(package => !existingPackage.package.Any(exist => package.id == exist.id)).ToArray();
                Package[] rootPackages = requiredPackages.Where(package => rootPackage.package.Any(root => root.id == package.id)).ToArray();
                Package[] installPackages = requiredPackages.Where(package => !installed.package.Any(install => install.id == package.id && install.version == package.version)).ToArray();
                Package[] deletePackages = installed.package.Where(package => !requiredPackages.Any(req => req.id == package.id && req.version == package.version)).ToArray();

                return (new OperationResult(OperationState.Progress, ""), rootPackages, installPackages, deletePackages);
            }
        }


        /// <summary>
        /// <para>Check that the packages listed in package.config are installed.</para>
        /// <para>package.configに記載されているパッケージがインストールされているか確認する。</para>
        /// </summary>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// </param>
        /// <returns>
        /// <para>Is need to fix?</para>
        /// <para>全てインストールされておらず修復の必要があるか。</para>
        /// </returns>
        public static async Task<bool> IsNeedToFix()
        {
            if (working)
            {
                throw new InvalidOperationException("Now other processes are in progress.");
            }
            working = true;

            try
            {
                Load();
                var installed = await Task.WhenAll(Installed.package.Select(async pkg =>
                {
                    var controller = GetPackageController();
                    var installPath = await controller.GetInstallPath(pkg);
                    return Directory.Exists(installPath);
                }));

                return !installed.All(b => b);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                working = false;
            }
        }


        /// <summary>
        /// <para>Fix as follows in package.config.</para>
        /// <para>package.configの通りに修復する。</para>
        /// </summary>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        private static async Task<OperationResult> FixPackagesInternal()
        {
            var ret = OperationState.Progress;
            try
            {
                var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

                var isInstalled = Installed.package.Select(async pkg =>
                {
                    var controller = GetPackageController();
                    var installPath = await controller.GetInstallPath(pkg);
                    return (Directory.Exists(installPath), pkg);
                });
                var isInstalled_ = await Task.WhenAll(isInstalled);
                var notInstalled = isInstalled_.Where(b => !b.Item1).Select(b => b.pkg);

                if (!notInstalled.Any())
                {
                    ret = OperationState.Cancel;
                    return new OperationResult(ret, "No packages to repair.\n(If you want to repair the contents of a package, please repair the package individually.)");
                }

                var task = InstallSelectPackages(notInstalled, loadedAsmNames);

                _ = DownloadProgress(0, notInstalled.Select(package => package.id).ToArray());
                _ = await task;
                if (task.IsFaulted)
                {
                    ret = OperationState.Failure;
                    return new OperationResult(ret, "Error occured!\n" + task.Exception.Message);
                }
            }
            catch (Exception e)
            {
                ret = OperationState.Failure;
                return new OperationResult(ret, "Error occured!\n" + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ret = OperationState.Success;
            return new OperationResult(ret, "The repair finished.");
        }

        /// <summary>
        /// <para>Base class for operating packages. Implement FindOperatePackages when operating on packages. Remember to change onlyStable and method.</para>
        /// <para>パッケージを操作する際の基底クラス。パッケージを操作する際は FindOperatePackages を実装。onlyStable, method の変更を忘れないように。</para>
        /// </summary>
        internal abstract class OperatePackage
        {
            private bool isOperated = false;
            protected bool isConfirmToUser = true;

            protected virtual string FinishMessage { get => "Operateion finished."; }

            /// <summary>
            /// <para>Perform operations to packages.</para>
            /// <para>パッケージの操作を行う。</para>
            /// </summary>
            /// <returns>
            /// <para>Operation result.</para>
            /// <para>操作結果。</para>
            /// </returns>
            /// <exception cref="InvalidOperationException">
            /// <para>It is thrown if the operation has already been performed in this instance.</para>
            /// <para>既にこのインスタンスで操作済みの場合スローされる。</para>
            /// </exception>
            public async Task<OperationResult> Operate()
            {
                if (isOperated)
                {
                    throw new InvalidOperationException("It has already been operated in this instance.");
                }

                var ret = OperationState.Progress;
                var installingPackages = new List<Package>();

                if (working)
                {
                    ret = OperationState.Cancel;
                    return new OperationResult(ret, "Now other processes are in progress.");
                }
                working = true;

                try
                {
                    EditorApplication.LockReloadAssemblies();
                    AssetDatabase.StartAssetEditing();

                    EditorUtility.DisplayProgressBar("NuGet importer", "Solving dependency", 0);
                    Load();
                    var (result, rootPackages, installPackages, deletePackages) = await FindOperatePackages();
                    if (result.State != OperationState.Progress)
                    {
                        return result;
                    }
                    installingPackages = installPackages.ToList();
                    var uninstallPackages = deletePackages.Where(package => !installPackages.Any(install => install.id == package.id)).ToArray();
                    var changePackages = deletePackages.Where(package => installPackages.Any(install => install.id == package.id)).ToArray();
                    var allinstallPackages = installed.package.Where(package => !deletePackages.Any(uninstall => uninstall.id == package.id)).Concat(installPackages).ToArray();

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

                    if (isConfirmToUser)
                    {
                        if (!await ConfirmToUser(installPackages, uninstallPackages, nativePackages))
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
                            var process = await OperateWithNativeAsync(installPackages, managedPackages, nativePackages, allinstallPackages, rootPackages);
                            AssetDatabase.SaveAssets();
                            EditorSceneManager.SaveOpenScenes();
                            process.Start();
                            EditorApplication.Exit(0);
                        }

                        if (uninstallPackages.Any())
                        {
                            await UninstallSelectedPackages(uninstallPackages);
                        }
                    }

                    var loadedAsmNames = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name);
                    loadedAsmNames = loadedAsmNames.Except(packageAsmNames.managedList.SelectMany(pkg => pkg.fileNames)).ToArray();

                    if (changePackages.Any())
                    {
                        await UninstallSelectedPackages(changePackages);
                    }

                    IEnumerable<Package> skipped = new List<Package>();
                    if (installPackages.Any())
                    {
                        var tasks = InstallSelectPackages(installPackages, loadedAsmNames);

                        _ = DownloadProgress(0.5f, installPackages.Select(package => package.id).ToArray());
                        skipped = await tasks;
                        if (tasks.IsFaulted)
                        {
                            ret = OperationState.Failure;
                            EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + tasks.Exception.Message, "OK");
                            await Rollback(installingPackages);
                            return new OperationResult(ret, "Rollback to before operation is complete.");
                        }
                    }

                    installed.package = allinstallPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();
                    rootPackage.package = rootPackages.Where(pkg => !skipped.Any(skip => skip.id == pkg.id)).ToArray();

                    if (skipped.Any())
                    {
                        EditorUtility.DisplayDialog("NuGet importer", "The below packages are existing in your project so we skipped installing them.\n\n" +
                            skipped.Select(pkg => pkg.id).Aggregate((now, next) => now + "\n" + next), "OK");
                    }
                }
                catch (Exception e)
                {
                    ret = OperationState.Failure;
                    EditorUtility.DisplayDialog("NuGet importer", "Error occured!\nRolls back to before the operation.\nError :\n" + e.Message, "OK");
                    await Rollback(installingPackages);
                    return new OperationResult(ret, "Rollback to before operation is complete.");
                }
                finally
                {
                    FinalizeProcess(ret);
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
            private async Task<bool> ConfirmToUser(IEnumerable<Package> installPackages, IEnumerable<Package> uninstallPackages, IEnumerable<Package> nativePackages)
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

            /// <summary>
            /// <para>Rollback for this operation.</para>
            /// <para>この操作に対するロールバックを行う。</para>
            /// </summary>
            /// <param name="installingPackages">
            /// <para>Package to be installed by this operation.</para>
            /// <para>この操作でインストールするパッケージ。</para>
            /// </param>
            private async Task Rollback(IEnumerable<Package> installingPackages)
            {
                await UninstallSelectedPackages(installingPackages);
                await FixPackagesInternal();
            }

            /// <summary>
            /// <para>Package operation finalize processing.</para>
            /// <para>パッケージ操作終了処理。</para>
            /// </summary>
            /// <param name="ret">
            /// <para>Operation result.</para>
            /// <para>操作結果。</para>
            /// </param>
            private void FinalizeProcess(OperationState ret)
            {
                working = false;
                isOperated = true;
                EditorUtility.ClearProgressBar();
                Save();
                AssetDatabase.StopAssetEditing();
                if (ret != OperationState.Cancel)
                {
                    AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                    Client.Resolve();
#endif
                }
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                if (ret != OperationState.Cancel)
                {
                    CompilationPipeline.RequestScriptCompilation();
                }
            }

            /// <summary>
            /// <para>Find packages to be operated on.</para>
            /// <para>操作対象のパッケージを探す。</para>
            /// </summary>
            /// <returns>
            /// <para>Operation result, root package, Packages to install, packages to remove.</para>
            /// <para>操作結果、ルートのパッケージ、インストールするパッケージ、削除するパッケージ。</para>
            /// </returns>
            internal protected abstract Task<(OperationResult result, IEnumerable<Package> root, IEnumerable<Package> install, IEnumerable<Package> delete)> FindOperatePackages();
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

                Load();

                if (!installed.package.Any())
                {
                    try
                    {
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json.meta"));
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
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json.meta"));
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
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json"));
                        File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json.meta"));
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

                Load();

                if (!installed.package.Any())
                {
                    if (!Directory.Exists(Path.Combine(DataPath, "Packages")))
                    {
                        Directory.CreateDirectory(Path.Combine(DataPath, "Packages"));
                    }
                    File.WriteAllText(Path.Combine(DataPath, "Packages", "managedPluginList.json"), "");
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
            using (var file = new StreamWriter(DataPath.Replace("Assets", "RollBackPackage.xml")))
            {
                serializer.Serialize(file, installed);
            }

            using (var file = new StreamWriter(DataPath.Replace("Assets", "RollBackRoot.xml")))
            {
                serializer.Serialize(file, rootPackage);
            }

            var controller = GetPackageController();
            var process = await controller.OperateWithNativeAsync(installs, manageds, natives, allInstalled, root);
            installed.package = installed.package.Where(package => !manageds.Any(manage => manage.id == package.id) && !natives.Any(native => native.id == package.id)).ToArray();
            rootPackage.package = rootPackage.package.Where(package => installed.package.Any(installed => installed.id == package.id)).ToArray();
            packageAsmNames.managedList = packageAsmNames.managedList.Where(pkg => installed.package.Any(installed => installed.id == pkg.packageName)).ToList();
            Save();
            return process;
        }

        private static async Task UninstallSelectedPackages(IEnumerable<Package> packages)
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

        private static string RootPath { get => DataPath.Replace("Assets", ""); }

        private static bool HasNative(string path, string packageId = "")
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                var filePath = file.Replace(RootPath, "");
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