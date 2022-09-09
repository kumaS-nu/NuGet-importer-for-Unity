using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;
using kumaS.NuGetImporter.Editor.PackageOperation;

using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for managing packages.</para>
    /// <para>パッケージを管理するクラス。</para>
    /// </summary>
    public static partial class PackageManager
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
        internal static ReadOnlyControlledPackages controlledPackages;

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
        /// <para>The name of the assembly included in the package.</para>
        /// <para>パッケージに含まれているアセンブリ名。</para>
        /// </summary>
        internal static ManagedPluginList PackageAsmNames { get => packageAsmNames; }

        public static ReadOnlyControlledPackages ControlledPackages { get => controlledPackages.Clone(); }

        public static IReadOnlyDictionary<string, Catalog> InstalledCatalog { get => installedCatalog; }

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

            // Unity2019 is below C# 8.0, so we don't use the compound assignment operator now.

            if (installed == null)
            {
                installed = new InstalledPackages();
            }
            if (installed.package == null)
            {
                installed.package = new List<Package>();
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
                rootPackage.package = new List<Package>();
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
                existingPackage.package = new List<Package>();
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

            controlledPackages = new ReadOnlyControlledPackages(installed, rootPackage, existingPackage);
        }

        [InitializeOnLoadMethod]
        public static async Task Boot()
        {
            dataPath = Application.dataPath;

            await RebootProcessAsync();

            if (installed.package != null)
            {
                IEnumerable<Task<Catalog>> tasks = installed.package.Select(pkg => NuGet.GetCatalog(pkg.id));
                Catalog[] catalogs = await Task.WhenAll(tasks);
                lock (installedCatalog)
                {
                    foreach (Catalog catalog in catalogs)
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
                NuGetImporterSettings.EnsureSetProjectSettingsPath();

                if (NuGetImporterSettings.Instance.AutoPackagePlacementCheck)
                {
                    try
                    {
                        if (await IsNeedToFix())
                        {
                            if (EditorUtility.DisplayDialog("NuGet importer", "We find packages that are not installed. Do you install these packages?", "Yes", "No"))
                            {
                                OperationResult fixResult = await FixPackagesAsync(false);
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
            (InstalledPackages willInstall, InstalledPackages willRoot, InstalledPackages rollbackPackages) = GetRestartInfo();
            await Task.Delay(1000);
            var operation = new RebootProcess(willInstall, willRoot, rollbackPackages);
            OperationResult result = await operation.Execute();

            if (NuGetImporterSettings.Instance.InstallMethod == InstallMethod.AsUPM)
            {
                DeleteAsAssetDirectory();
            }

            if (result.State == OperationState.Success)
            {
                rootPackage.package.Clear();
                rootPackage.package.AddRange(willRoot.package);
            }
            Save();

            EditorUtility.DisplayDialog("NuGet importer", result.Message, "OK");
        }

        private static (InstalledPackages willInstall, InstalledPackages willRoot, InstalledPackages rollbackPackages) GetRestartInfo()
        {
            InstalledPackages willInstall;
            using (var file = new StreamReader(dataPath.Replace("Assets", "WillInstall.xml")))
            {
                willInstall = (InstalledPackages)serializer.Deserialize(file);
            }

            if (willInstall == null || willInstall.package == null || !willInstall.package.Any())
            {
                File.Delete(dataPath.Replace("Assets", "WillInstall.xml"));
                File.Delete(dataPath.Replace("Assets", "WillPackage.xml"));
                File.Delete(dataPath.Replace("Assets", "WillRoot.xml"));
                if (File.Exists(dataPath.Replace("Assets", "RollBackPackage.xml")))
                {
                    File.Delete(dataPath.Replace("Assets", "RollBackPackage.xml"));
                    File.Delete(dataPath.Replace("Assets", "RollBackRoot.xml"));
                }
                return (willInstall, willInstall, willInstall);
            }

            InstalledPackages willRoot;
            using (var file = new StreamReader(dataPath.Replace("Assets", "WillRoot.xml")))
            {
                willRoot = (InstalledPackages)serializer.Deserialize(file);
            }

            if (willRoot == null)
            {
                willRoot = new InstalledPackages();
            }
            if (willRoot.package == null)
            {
                willRoot.package = new List<Package>();
            }

            File.Delete(dataPath.Replace("Assets", "WillInstall.xml"));
            File.Delete(dataPath.Replace("Assets", "WillPackage.xml"));
            File.Delete(dataPath.Replace("Assets", "WillRoot.xml"));

            InstalledPackages rollBackPackage = default;
            if (File.Exists(dataPath.Replace("Assets", "RollBackPackage.xml")))
            {
                using (var file = new StreamReader(dataPath.Replace("Assets", "RollBackPackage.xml")))
                {
                    rollBackPackage = (InstalledPackages)serializer.Deserialize(file);
                }
                File.Delete(dataPath.Replace("Assets", "RollBackPackage.xml"));

                if (rollBackPackage == null)
                {
                    rollBackPackage = new InstalledPackages();
                }
                if (rollBackPackage.package == null)
                {
                    rollBackPackage.package = new List<Package>();
                }
                installed = rollBackPackage;
            }

            if (File.Exists(dataPath.Replace("Assets", "RollBackRoot.xml")))
            {
                InstalledPackages rollBackRoot;
                using (var file = new StreamReader(dataPath.Replace("Assets", "RollBackRoot.xml")))
                {
                    rollBackRoot = (InstalledPackages)serializer.Deserialize(file);
                }
                File.Delete(dataPath.Replace("Assets", "RollBackRoot.xml"));

                if (rollBackRoot == null)
                {
                    rollBackRoot = new InstalledPackages();
                }
                if (rollBackRoot.package == null)
                {
                    rollBackRoot.package = new List<Package>();
                }

                rootPackage = rollBackRoot;
            }

            return (willInstall, willRoot, rollBackPackage);
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
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static async Task<OperationResult> InstallPackageAsync(string packageId, string version, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operation = new InstallPackage(packageId, version, onlyStable, method);
            OperationResult result = await operation.Execute();
            if (result.State == OperationState.Success)
            {
                rootPackage.package.Add(installed.package.First(pkg => pkg.id == packageId));
                var rootId = rootPackage.package.Select(pkg => pkg.id).ToArray();
                rootPackage.package.Clear();
                rootPackage.package.AddRange(installed.package.Where(pkg => rootId.Contains(pkg.id)));
                Save();
            }
            return result;
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
            var operateion = new FixSpecifiedPackage(packageId, confirm);
            return operateion.Execute();
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
            return operateion.Execute();
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
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static async Task<OperationResult> UninstallPackagesAsync(string packageId, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operation = new UninstallPackages(packageId, onlyStable, method);
            OperationResult result = await operation.Execute();
            if (result.State == OperationState.Success)
            {
                rootPackage.package.RemoveAll(pkg => pkg.id == packageId);
                Save();
            }
            return result;
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
        /// </param>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static async Task<OperationResult> ChangePackageVersionAsync(string packageId, string newVersion, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operateion = new ChangePackageVersion(packageId, newVersion, onlyStable, method);
            OperationResult result = await operateion.Execute();
            if (result.State == OperationState.Success)
            {
                var rootId = rootPackage.package.Select(pkg => pkg.id).ToArray();
                rootPackage.package.Clear();
                rootPackage.package.AddRange(installed.package.Where(pkg => rootId.Contains(pkg.id)));
                Save();
            }
            return result;
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
                    return await IsPackageCorrectlyInstalled(pkg);
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
        /// <para>Move the packages from under Asset to under UPM.</para>
        /// <para>パッケージをAsset下からUPM下に移す。</para>
        /// </summary>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static async Task<OperationResult> ConvertToUPM()
        {
            var operation = new ConvertToUPM();
            OperationResult result = await operation.Execute();
            if (result.State == OperationState.Success)
            {
                try
                {
                    File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json"));
                    File.Delete(Path.Combine(DataPath, "Packages", "managedPluginList.json.meta"));
                }
                catch (Exception) { }
                DeleteAsAssetDirectory();
            }
            return result;
        }

        /// <summary>
        /// <para>Move the packages from under UPM to under Asset.</para>
        /// <para>パッケージをUPM下からAsset下に移す。</para>
        /// </summary>
        /// <returns>
        /// <para>Operation result.</para>
        /// <para>操作結果。</para>
        /// </returns>
        public static async Task<OperationResult> ConvertToAssets()
        {
            var operation = new ConvertToAssets();
            OperationResult result = await operation.Execute();
            if (result.State == OperationState.Success)
            {
                if (!Directory.Exists(Path.Combine(DataPath, "Packages")))
                {
                    Directory.CreateDirectory(Path.Combine(DataPath, "Packages"));
                }
                if (!File.Exists(Path.Combine(DataPath, "Packages", "managedPluginList.json")))
                {
                    File.WriteAllText(Path.Combine(DataPath, "Packages", "managedPluginList.json"), "");
                }
            }
            return result;
        }

        /// <summary>
        /// <para>Reinstall based on the current root package.</para>
        /// <para>現在のルートパッケージを元に再インストールする。</para>
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
        public static async Task<OperationResult> ReInstallAllPackages(bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            var operation = new ReInstallAllPackages(onlyStable, method);
            OperationResult result = await operation.Execute();
            if (result.State == OperationState.Success)
            {
                var rootId = rootPackage.package.Select(pkg => pkg.id).ToArray();
                rootPackage.package.Clear();
                rootPackage.package.AddRange(installed.package.Where(pkg => rootId.Contains(pkg.id)));
                Save();
            }
            return result;
        }

        /// <summary>
        /// <para>Uninstall the packages installed with this plugin and initialize the internal data.</para>
        /// <para>このプラグインでインストールしたパッケージを削除し、内部データを初期化する。</para>
        /// </summary>
        public static async Task<OperationResult> CleanUp()
        {
            packageAsmNames.managedList.Clear();
            existingPackage.package.Clear();
            Save();
            var operation = new CleanUp();
            OperationResult result = await operation.Execute();
            installed.package.Clear();
            rootPackage.package.Clear();
            Save();
            return result;
        }

        /// <summary>
        /// <para>Does the directory for the specified package exist?</para>
        /// <para>指定したパッケージのディレクトリは存在するか。</para>
        /// </summary>
        public static async Task<bool> IsPackageCorrectlyInstalled(Package package)
        {
            PackageControllerBase controller = GetPackageController();
            var installPath = await controller.GetInstallPath(package);
            return Directory.Exists(installPath);
        }

        /// <summary>
        /// <para>Installs the specified package. Operation results are not automatically saved. (Low-level API.)</para>
        /// <para>指定したパッケージをインストールする。操作結果は自動的に保存されない。（低レベルAPI。）</para>
        /// </summary>
        /// <param name="packages">
        /// <para>Packages to install.</para>
        /// <para>インストールするパッケージ。</para>
        /// </param>
        /// <param name="loadAssembliesFullName">
        /// <para>List of assembly names loaded into Unity.</para>
        /// <para>Unityに読み込まれているアセンブリ名一覧。</para>
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
        /// <para>Packages that were not installed.</para>
        /// <para>インストールを行わなかったパッケージ。</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <para>Thrown if an invalid lock is given.</para>
        /// <para>不正なロックが与えられた場合発生する。</para>
        /// </exception>
        internal static async Task<IEnumerable<Package>> InstallSelectPackages(IEnumerable<Package> packages, IEnumerable<string> loadAssembliesFullName, OperateLock operateLock, PackageControllerBase controller = default)
        {
            if (operateLock.IsInvalid)
            {
                throw new InvalidOperationException("You operate with invalid operator lock.");
            }

            if (controller == default)
            {
                controller = GetPackageController();
            }
            var tasks = new List<Task<(bool isSkipped, Package package, PackageManagedPluginList asm)>>();
            foreach (Package package in packages)
            {
                tasks.Add(controller.InstallPackageAsync(package, loadAssembliesFullName));
            }
            (bool isSkipped, Package package, PackageManagedPluginList asm)[] result = await Task.WhenAll(tasks);
            var ret = new List<Package>();
            foreach ((var isSkipped, Package package, PackageManagedPluginList asm) in result)
            {
                if (isSkipped)
                {
                    ret.Add(package);
                }
                else
                {
                    Catalog catalog = await NuGet.GetCatalog(package.id);
                    installedCatalog[package.id] = catalog;
                    packageAsmNames.managedList.Add(asm);
                }
            }

            installed.package.AddRange(result.Where(value => !value.isSkipped).Select(value => value.package));
            existingPackage.package.AddRange(ret.Select(r => new Package() { id = r.id, version = "0.0.0" }));

            return ret;
        }

        /// <summary>
        /// <para>Operate packages containing native plugins.(Low-level API.)</para>
        /// <para>ネイティブプラグインを含むパッケージを操作する。（低レベルAPI。）</para>
        /// </summary>
        /// <param name="installs">
        /// <para>Packages to install.</para>
        /// <para>インストールするパッケージ。</para>
        /// </param>
        /// <param name="manageds">
        /// <para>Packages of only managed plugins to be removed.</para>
        /// <para>削除するマネージドプラグインのみのパッケージ。</para>
        /// </param>
        /// <param name="natives">
        /// <para>Packages that contain native plugins to be removed.</para>
        /// <para>削除するネイティブプラグインを含むパッケージ。</para>
        /// </param>
        /// <param name="allInstalled">
        /// <para>All installed packages.</para>
        /// <para>全てのインストールされたパッケージ。</para>
        /// </param>
        /// <param name="root">
        /// <para>Root package.</para>
        /// <para>ルートのパッケージ。</para></param>
        /// <returns>
        /// <param name="operateLock">
        /// <para>Lock for operation.</para>
        /// <para>操作を行うためのロック。</para>
        /// </param>
        /// <param name="controller">
        /// <para>Controller to operate.</para>
        /// <para>操作を行うコントローラー。</para>
        /// </param>
        /// <returns>
        /// <para>The process of deleting native plugins and restarting Unity.</para>
        /// <para>ネイティブプラグインの削除・Unityの再起動を行うプロセス。</para>
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <para>Thrown if an invalid lock is given.</para>
        /// <para>不正なロックが与えられた場合発生する。</para>
        /// </exception>
        internal static async Task<Process> OperateWithNativeAsync(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root, OperateLock operateLock, PackageControllerBase controller = default)
        {
            if (operateLock.IsInvalid)
            {
                throw new InvalidOperationException("You operate with invalid operator lock.");
            }

            if (controller == default)
            {
                controller = GetPackageController();
            }

            using (var file = new StreamWriter(DataPath.Replace("Assets", "RollBackPackage.xml")))
            {
                serializer.Serialize(file, installed);
            }

            using (var file = new StreamWriter(DataPath.Replace("Assets", "RollBackRoot.xml")))
            {
                serializer.Serialize(file, rootPackage);
            }

            Process process = await controller.OperateWithNativeAsync(installs, manageds, natives, allInstalled, root);
            installed.package.RemoveAll(package => manageds.Any(manage => manage.id == package.id) || natives.Any(native => native.id == package.id));
            rootPackage.package.RemoveAll(package => !installed.package.Any(installed => installed.id == package.id));
            packageAsmNames.managedList = packageAsmNames.managedList.Where(pkg => installed.package.Any(installed => installed.id == pkg.packageName)).ToList();
            Save();
            return process;
        }

        /// <summary>
        /// <para>Uninstalls the specified package. Operation results are not automatically saved. (Low-level API.)</para>
        /// <para>指定したパッケージをアンインストールする。操作結果は自動的に保存されない。（低レベルAPI。）</para>
        /// </summary>
        /// <param name="packages">
        /// <para>Packages to uninstall.</para>
        /// <para>アンインストールするパッケージ。</para>
        /// </param>
        /// <param name="operateLock">
        /// <para>Lock for operation.</para>
        /// <para>操作を行うためのロック。</para>
        /// </param>
        /// <param name="controller">
        /// <para>Controller to operate.</para>
        /// <para>操作を行うコントローラー。</para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <para>Thrown if an invalid lock is given.</para>
        /// <para>不正なロックが与えられた場合発生する。</para>
        /// </exception>
        internal static async Task UninstallSelectedPackages(IEnumerable<Package> packages, OperateLock operateLock, PackageControllerBase controller = default)
        {
            if (operateLock.IsInvalid)
            {
                throw new InvalidOperationException("You operate with invalid operator lock.");
            }

            if (controller == default)
            {
                controller = GetPackageController();
            }

            await controller.UninstallManagedPackagesAsync(packages);
            lock (installed)
            {
                lock (installedCatalog)
                {
                    lock (packageAsmNames)
                    {
                        foreach (Package package in packages)
                        {
                            installed.package.RemoveAll(pkg => pkg.id == package.id);
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

        internal static async Task<bool> HasNativeAsync(Package package, PackageControllerBase controller = default)
        {
            if (controller == default)
            {
                controller = GetPackageController();
            }
            var path = await controller.GetInstallPath(package);
            var packageId = "";
            if (controller is PackageControllerAsUPM)
            {
                var jsonPath = Path.Combine(path, "package.json");
                if (File.Exists(jsonPath))
                {
                    var jsonString = File.ReadAllText(jsonPath);
                    try
                    {
                        PackageJson json = JsonUtility.FromJson<PackageJson>(jsonString);
                        packageId = json.name;
                    }
                    catch (Exception) { }
                }
            }
            return HasNative(path, packageId);
        }

        private static string RootPath { get => DataPath.Replace("Assets", ""); }

        internal static bool HasNative(string path, string packageId = "")
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

        internal static async Task DownloadProgress(float startPos, IEnumerable<string> packageNames)
        {
            var allPackageSize = new Dictionary<string, long>();
            var downloadedSumSizeLog = new LinkedList<long>();
            while (true)
            {
                var downloadedSumSize = 0L;
                var finishedCount = 0;
                foreach (var packageName in packageNames)
                {
                    if (NuGet.TryGetDownloadingProgress(packageName, out var packageSize, out var downloadedSize))
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
                foreach (KeyValuePair<string, long> packageSize in allPackageSize)
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

        public sealed class OperateLock : IDisposable
        {
            public OperationState result = OperationState.Progress;
            public bool IsInvalid { get; private set; }

            public OperateLock()
            {
                IsInvalid = working;
                if (IsInvalid)
                {
                    return;
                }
                EditorApplication.LockReloadAssemblies();
                AssetDatabase.StartAssetEditing();
                Load();
                working = true;
            }

            public void Dispose()
            {
                if (IsInvalid)
                {
                    return;
                }
                working = false;
                IsInvalid = true;
                EditorUtility.ClearProgressBar();
                if (result != OperationState.Progress)
                {
                    Save();
                }
                AssetDatabase.StopAssetEditing();
                if (result != OperationState.Cancel)
                {
                    AssetDatabase.Refresh();
#if UNITY_2020_1_OR_NEWER
                    Client.Resolve();
#endif
                }
                EditorApplication.RepaintProjectWindow();
                EditorApplication.UnlockReloadAssemblies();
                if (result == OperationState.Progress)
                {
                    throw new InvalidOperationException("Operation result is not set. However, you are finishing operation.");
                }
                if (result != OperationState.Cancel)
                {
                    CompilationPipeline.RequestScriptCompilation();
                }
            }
        }
    }
}
