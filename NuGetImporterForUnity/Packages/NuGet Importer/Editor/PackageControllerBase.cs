using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor
{
    internal abstract class PackageControllerBase
    {
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(InstalledPackages));
        private readonly string[] deleteDirectories = new string[] { "_rels", "package", "build", "buildMultiTargeting", "buildTransitive" };
        protected internal PackagePathSolverBase pathSolver { protected set; get; }

        /// <summary>
        /// <para>Install the specified package.</para>
        /// <para>指定パッケージをインストールする。</para>
        /// </summary>
        /// <param name="package">
        /// <para>Package to install.</para>
        /// <para>インストールするパッケージ。</para>
        /// </param>
        internal abstract Task<(bool isSkipped, Package package, PackageManagedPluginList asm)> InstallPackageAsync(Package package, IEnumerable<string> loadedAsmName);

        /// <summary>
        /// <para>Remove plugins outside the specified directory.</para>
        /// <para>規定ディレクトリ外のプラグインを削除する。</para>
        /// </summary>
        /// <param name="package">
        /// <para>Package.</para>
        /// <para>パッケージ。</para>
        /// </param>
        internal abstract void DeletePluginsOutOfDirectory(Package package);

        /// <summary>
        /// <para>Uninstall the managed plugin package.</para>
        /// <para>マネージドプラグインのパッケージをアンインストールする。</para>
        /// </summary>
        /// <param name="packages">
        /// <para>Package to be uninstalled.</para>
        /// <para>アンインストールするパッケージ。</para>
        /// </param>
        internal async Task UninstallManagedPackagesAsync(IEnumerable<Package> packages)
        {
            var tasks = new List<Task>();
            foreach (Package package in packages)
            {
                tasks.Add(UninstallManagedPackageAsync(package));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// <para>Operate with remove native plugins.</para>
        /// <para>ネイティブプラグインの削除を含む操作を行う。</para>
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
        /// <para>ルートのパッケージ。</para>
        /// </param>
        /// <returns>
        /// <para>The process of removing native plugins.</para>
        /// <para>ネイティブプラグインを削除するプロセス。</para>
        /// </returns>
        internal async Task<Process> OperateWithNativeAsync(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root)
        {
            using (var file = new StreamWriter(PackageManager.DataPath.Replace("Assets", "WillInstall.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = installs.ToList()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(PackageManager.DataPath.Replace("Assets", "WillPackage.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = allInstalled.ToList()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(PackageManager.DataPath.Replace("Assets", "WillRoot.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = root.ToList()
                };
                serializer.Serialize(file, write);
            }

            await UninstallManagedPackagesAsync(manageds);
            foreach (Package native in natives)
            {
                DeletePluginsOutOfDirectory(native);
            }

            IEnumerable<Task<string>> tasks = natives.Select(package => pathSolver.InstallPath(package));
            IEnumerable<string> nativeDirectory = await Task.WhenAll(tasks);
            IEnumerable<string> nativeNugetDirectory = natives.Select(package => Path.Combine(PackageManager.DataPath.Replace("Assets", "NuGet"), package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant()));

            return CreateDeleteNativeProcess(nativeDirectory.ToArray(), nativeNugetDirectory.ToArray());
        }

        /// <summary>
        /// <para>Get the loadable assemblies in the package.</para>
        /// <para>パッケージにあるロード可能なアセンブリを取得。</para>
        /// </summary>
        /// <param name="searchPath">
        /// <para>Extract path.</para>
        /// <para>展開したパス。</para>
        /// </param>
        /// <param name="asm">
        /// <para>Loadable assemblies.</para>
        /// <para>ロード可能なアセンブリ。</para>
        /// </param>
        protected void GetLoadableAsmInPackage(string searchPath, PackageManagedPluginList asm)
        {
            foreach (var file in Directory.GetFiles(searchPath))
            {
                if (!file.EndsWith(".dll"))
                {
                    continue;
                }
                try
                {
                    asm.fileNames.Add(AssemblyName.GetAssemblyName(file).Name);
                }
                catch (Exception) { }
            }

            foreach (var dir in Directory.GetDirectories(searchPath))
            {
                GetLoadableAsmInPackage(dir, asm);
            }
        }

        /// <summary>
        /// <para>Extract the package to the specified directory.</para>
        /// <para>パッケージを指定のディレクトリに展開する。</para>
        /// </summary>
        /// <param name="package">
        /// <para>Packages to be extracted.</para>
        /// <para>展開するパッケージ。</para>
        /// </param>
        /// <exception cref="System.IO.DirectoryNotFoundException">
        /// <para>Thrown when the target directory path does not exist.</para>
        /// <para>展開先のディレクトリが存在しないときに投げられる。</para>
        /// </exception>
        protected async Task ExtractPackageAsync(Package package)
        {
            var extractPath = await pathSolver.InstallPath(package);
            var nupkgName = package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant() + ".nupkg";
            var tempPath = PackageManager.DataPath.Replace("Assets", "Temp");
            var downloadPath = Path.Combine(tempPath, nupkgName);
            var nugetPath = PackageManager.DataPath.Replace("Assets", "NuGet");
            var nugetPackagePath = Path.Combine(nugetPath, package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());

            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }
            await NuGet.GetPackage(package.id, package.version, tempPath);
            if (!Directory.Exists(nugetPath))
            {
                Directory.CreateDirectory(nugetPath);
            }
            if (Directory.Exists(nugetPackagePath))
            {
                DeleteDirectory(nugetPackagePath);
            }
            Directory.CreateDirectory(nugetPackagePath);
            DeleteDirectory(extractPath);
            ZipFile.ExtractToDirectory(downloadPath, extractPath);
            foreach (var del in deleteDirectories)
            {
                DeleteDirectory(Path.Combine(extractPath, del));
            }

            foreach (var file in Directory.GetFiles(extractPath))
            {
                if (file.Contains(".nuspec") || file.Contains("[Content_Types].xml"))
                {
                    File.Delete(file);
                }
            }

            var managedPath = Path.Combine(extractPath, "lib");
            if (Directory.Exists(managedPath))
            {
                ManagedPluginProcess(package, nugetPackagePath, extractPath, managedPath);
            }

            var nativePath = Path.Combine(extractPath, "runtimes");
            IEnumerable<string> directories = Directory.GetDirectories(extractPath).Select(path => Path.GetFileName(path).ToLowerInvariant());
            if (directories.Any(dir => dir == "unity") && Directory.Exists(nativePath))
            {
                MoveDirectory(nativePath, extractPath, nugetPackagePath);
            }
            else if (Directory.Exists(nativePath))
            {
                NativeProcess(nativePath, nugetPackagePath, extractPath);
            }

            AnalyzerProcess(extractPath, nugetPackagePath);

            foreach (var moveDir in Directory.GetDirectories(extractPath))
            {
                var dirName = Path.GetFileName(moveDir);
                if (dirName == "lib" || dirName == "runtimes" || dirName.ToLowerInvariant() == "unity")
                {
                    continue;
                }
                if (NuGetAnalyzerImportSetting.HasAnalyzerSupport && dirName == "analyzers")
                {
                    continue;
                }
                MoveDirectory(moveDir, extractPath, nugetPackagePath);
            }
        }

        private void ManagedPluginProcess(Package package, string nugetPackagePath, string extractPath, string managedPath)
        {
            List<string[]> frameworkDictionary = FrameworkName.ALLPLATFORM;
            var targetFramework = frameworkDictionary.FirstOrDefault(f => f.Contains(package.targetFramework));
            List<string> frameworkList = FrameworkName.TARGET;

            if (targetFramework == default)
            {
                targetFramework = frameworkDictionary.First(dic => dic.Contains(frameworkList.First()));
            }

            (var leftPath, var framework) = SelectManagedDirectory(managedPath, targetFramework, frameworkList);
            package.targetFramework = framework;

            LocalizeDirectoryProcess(nugetPackagePath, extractPath, leftPath);

            if (!Directory.GetDirectories(managedPath).Any() && !Directory.GetFiles(managedPath).Any())
            {
                DeleteDirectory(managedPath);
            }
        }

        /// <summary>
        /// <para>Delete just the right framework, leaving only the directory.</para>
        /// <para>ちょうどよいフレームワークをディレクトリのみ残して消す。</para>
        /// </summary>
        /// <param name="managedPath">
        /// <para>Directory where managed plugins are located.</para>
        /// <para>マネージドプラグインがあるディレクトリ。</para>
        /// </param>
        /// <param name="targetFramework">
        /// <para>Package target framework.</para>
        /// <para>パッケージのターゲットフレームワーク。</para>
        /// </param>
        /// <param name="frameworkList">
        /// <para>List of frameworks available for Unity.</para>
        /// <para>Unityで利用可能なフレームワーク一覧。</para>
        /// </param>
        /// <returns>
        /// <para>Paths left.Just the right framework.</para>
        /// <para>残したパス。ちょうどよいフレームワーク。</para>
        /// </returns>
        private (string path, string framework) SelectManagedDirectory(string managedPath, string[] targetFramework, List<string> frameworkList)
        {
            IEnumerable<string> forUnityPath = Directory.GetDirectories(managedPath).Where(p => Path.GetFileName(p).ToLowerInvariant() == "unity");
            if (forUnityPath.Any())
            {
                (var suit, List<string> remove) = GetSuitFramework(forUnityPath.First(), targetFramework, frameworkList);
                remove.AddRange(Directory.GetDirectories(managedPath).Where(p => Path.GetFileName(p).ToLowerInvariant() != "unity"));
                foreach (var r in remove)
                {
                    DeleteDirectory(r);
                }
                return (forUnityPath.First(), suit);
            }
            {
                (var suit, List<string> remove) = GetSuitFramework(managedPath, targetFramework, frameworkList);
                foreach (var r in remove)
                {
                    DeleteDirectory(r);
                }
                return (Path.Combine(managedPath, suit), suit);
            }
        }

        /// <summary>
        /// <para>Get just the right framework.</para>
        /// <para>ちょうどよいフレームワークを取得。</para>
        /// </summary>
        /// <param name="frameworkDir">
        /// <para>Directory where the framework is located.</para>
        /// <para>フレームワークがあるディレクトリ。</para>
        /// </param>
        /// <param name="targetFramework">
        /// <para>Package target framework.</para>
        /// <para>パッケージのターゲットフレームワーク。</para>
        /// </param>
        /// <param name="frameworkList">
        /// <para>List of frameworks available for Unity.</para>
        /// <para>Unityで利用可能なフレームワーク一覧。</para>
        /// </param>
        /// <returns>
        /// <para>Just the right framework.Directory to be removed.</para>
        /// <para>ちょうどよいフレームワーク。取り除くディレクトリ。</para>
        /// </returns>
        private (string suit, List<string> removes) GetSuitFramework(string frameworkDir, string[] targetFramework, List<string> frameworkList)
        {
            var removes = new List<string>();
            var frameworkPaths = Directory.GetDirectories(frameworkDir);
            IEnumerable<string> target = frameworkPaths.Where(framework => targetFramework.Contains(Path.GetFileName(framework)));
            if (target.Any())
            {
                removes.AddRange(frameworkPaths);
                removes.Remove(target.First());
                return (Path.GetFileName(target.First()), removes);
            }

            var priority = int.MaxValue;
            var suit = "";
            foreach (var framework in Directory.GetDirectories(frameworkDir))
            {
                var frameworkName = Path.GetFileName(framework);
                if (!frameworkList.Contains(frameworkName))
                {
                    removes.Add(framework);
                }
                else if (frameworkList.IndexOf(frameworkName) < priority)
                {
                    if (priority != int.MaxValue)
                    {
                        removes.Add(suit);
                    }
                    priority = frameworkList.IndexOf(frameworkName);
                    suit = framework;
                }
                else
                {
                    removes.Add(framework);
                }
            }
            return (Path.GetFileName(suit), removes);
        }

        private void LocalizeDirectoryProcess(string nugetPackagePath, string extractPath, string localizePath)
        {
            List<List<string>> groupedDirectories = GroupLocalizedDirectory(localizePath);
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            foreach (List<string> grouped in groupedDirectories)
            {
                if (grouped.Count == 1)
                {
                    continue;
                }

                foreach (var localized in grouped)
                {
                    var localizedName = Path.GetFileName(localized);
                    if (!currentCulture.Equals(CultureInfo.CreateSpecificCulture(localizedName)))
                    {
                        MoveDirectory(localized, extractPath, nugetPackagePath);
                    }
                }
            }
        }

        private void NativeProcess(string nativePath, string nugetPackagePath, string extractPath)
        {
            IEnumerable<NativePlatform> moveList = Directory.GetDirectories(nativePath).Select(native => new NativePlatform(native))
                .Where(native => native.osPriority >= 0).OrderBy(native => native.osPriority).Skip(1);

            foreach (NativePlatform move in moveList)
            {
                MoveDirectory(move.path, extractPath, nugetPackagePath);
            }

            foreach (var directory in Directory.GetDirectories(nativePath))
            {
                var platform = new NativePlatform(directory);
                if (!enableArch.TryGetValue(platform.os, out List<string> _) || !enableArch[platform.os].Contains(platform.architecture))
                {
                    MoveDirectory(platform.path, extractPath, nugetPackagePath);
                }
            }

            IEnumerable<string> nonNativeList = Directory.GetDirectories(nativePath)
                .SelectMany(native => Directory.GetDirectories(native)).Where(native => !native.EndsWith("native"));

            foreach (var move in nonNativeList)
            {
                MoveDirectory(move, extractPath, nugetPackagePath);
            }

            if (!Directory.GetDirectories(nativePath).Any())
            {
                DeleteDirectory(nativePath);
            }
        }

        private void AnalyzerProcess(string extractPath, string nugetPackagePath)
        {
            var analyzerLanguagePath = Path.Combine(extractPath, "analyzers", "dotnet");
            if (Directory.Exists(analyzerLanguagePath))
            {
                var langDir = Directory.GetDirectories(analyzerLanguagePath);
                foreach (var lang in langDir)
                {
                    if (lang.EndsWith("cs"))
                    {
                        continue;
                    }
                    MoveDirectory(lang, extractPath, nugetPackagePath);
                }
            }

            var analyzerLocalizePath = Path.Combine(extractPath, "analyzers", "dotnet", "cs");
            if (Directory.Exists(analyzerLocalizePath))
            {
                var localDir = Directory.GetDirectories(analyzerLocalizePath);
                if (localDir.Length != 0)
                {
                    LocalizeDirectoryProcess(nugetPackagePath, extractPath, analyzerLocalizePath);
                }
            }
        }

        /// <summary>
        /// <para>Group localization with the same resources.</para>
        /// <para>同じリソースのローカライズをグループ化する。</para>
        /// </summary>
        /// <param name="managedPath">
        /// <para>Path of the managed plugin.</para>
        /// <para>マネージドプラグインのパス。</para>
        /// </param>
        /// <returns>
        /// <para>Grouped directories.</para>
        /// <para>グループ化されたディレクトリ。</para>
        /// </returns>
        private List<List<string>> GroupLocalizedDirectory(string managedPath)
        {
            var ret = new List<List<string>>();
            var localizedFileNames = new List<List<string>>();
            foreach (var localizedDir in Directory.GetDirectories(managedPath))
            {
                var localizedFiles = Directory.GetFiles(localizedDir, "*.dll", SearchOption.AllDirectories).Select(file => Path.GetFileName(file)).ToList();
                var addIndex = 0;
                foreach (List<string> localizedFile in localizedFileNames)
                {
                    if (localizedFile.Intersect(localizedFiles).Any())
                    {
                        break;
                    }
                    addIndex++;
                }

                if (addIndex == ret.Count)
                {
                    localizedFileNames.Add(localizedFiles);
                    ret.Add(new List<string> { localizedDir });
                }
                else
                {
                    localizedFileNames[addIndex] = localizedFileNames[addIndex].Union(localizedFiles).ToList();
                    ret[addIndex].Add(localizedDir);
                }
            }

            return ret;
        }

        /// <summary>
        /// <para>Uninstall the managed plugin package.</para>
        /// <para>マネージドプラグインのパッケージをアンインストールする。</para>
        /// </summary>
        /// <param name="packages">
        /// <para>Package to be uninstalled.</para>
        /// <para>アンインストールするパッケージ。</para>
        /// </param>
        private async Task UninstallManagedPackageAsync(Package package)
        {
            var path = await pathSolver.InstallPath(package);
            var nugetPackagePath = Path.Combine(PackageManager.DataPath.Replace("Assets", "NuGet"), package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant());
            var tasks = new List<Task>
            {
                Task.Run(() => DeleteDirectory(nugetPackagePath)),
                Task.Run(() => DeleteDirectory(path)),
                Task.Run(() => DeletePluginsOutOfDirectory(package))
            };
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// <para>Delete directory without native plugins.</para>
        /// <para>ディレクトリを削除する。（ネイティブプラグインのない）</para>
        /// </summary>
        /// <param name="path">
        /// <para>Directory path to delete.</para>
        /// <para>削除するディレクトリのパス。</para>
        /// </param>
        protected void DeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
                File.Delete(path + ".meta");
            }
            catch (Exception e) when (e is ArgumentException || e is DirectoryNotFoundException || e is FileNotFoundException || e is NotSupportedException)
            { }
        }

        /// <summary>
        /// <para>Delete file that is not native plugin.</para>
        /// <para>ファイルを削除する。（ネイティブプラグインでない）</para>
        /// </summary>
        /// <param name="path">
        /// <para>File path to delete.</para>
        /// <para>削除するファイルのパス。</para>
        /// </param>
        protected void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                File.Delete(path + ".meta");
            }
            catch (Exception e) when (e is ArgumentException || e is FileNotFoundException || e is NotSupportedException)
            { }
        }

        /// <summary>
        /// <para>Move the directory out of the project.</para>
        /// <para>ディレクトリをプロジェクト外へ移動する。（ネイティブプラグインのない）</para>
        /// </summary>
        /// <param name="path">
        /// <para>Directory path to move.</para>
        /// <para>移動するディレクトリのパス。</para>
        /// </param>
        /// <param name="extractPath">
        /// <para>The path on which the package was extracted.</para>
        /// <para>パッケージが展開されたパス。</para>
        /// </param>
        /// <param name="nugetPackagePath">
        /// <para>Avoiding paths for packages.</para>
        /// <para>パッケージ用の退避パス。</para>
        /// </param>
        protected void MoveDirectory(string path, string extractPath, string nugetPackagePath)
        {
            var relativePath = path.Replace("\\", "/").Replace(extractPath.Replace("\\", "/"), "");
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }
            var dstPath = Path.Combine(nugetPackagePath, relativePath);
            if (!Directory.Exists(Path.GetDirectoryName(dstPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            }
            try
            {
                Directory.Move(path, dstPath);
                File.Delete(path + ".meta");
            }
            catch (Exception e) when (e is ArgumentException || e is FileNotFoundException || e is NotSupportedException)
            { }
        }

        /// <summary>
        /// <para>Create process that delete native packages.</para>
        /// <para>ネイティブのパッケージを削除するプロセスを作成。</para>
        /// </summary>
        /// <param name="directoryPaths">
        /// <para>Directory paths to delete.</para>
        /// <para>削除するディレクトリのパス。</para>
        /// </param>
        /// <param name="nugetDirectoryPaths">
        /// <para>Directory paths to delete in NuGet/.</para>
        /// <para>削除するNuGet内のディレクトリのパス。</para>
        /// </param>
        /// <returns>
        /// <para>The process of removing native plugins.</para>
        /// <para>ネイティブプラグインを削除するプロセス。</para>
        /// </returns>
        private Process CreateDeleteNativeProcess(IEnumerable<string> directoryPaths, IEnumerable<string> nugetDirectoryPaths)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            // Create and execute the command, and exit the editor.
            var command = new StringBuilder();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                process.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");

                // Wait a moment for exit the editor.
                command.Append("/c timeout 5 && ");
                foreach (var path in directoryPaths)
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
                foreach (var path in nugetDirectoryPaths)
                {
                    command.Append("rd /s /q \"");
                    command.Append(path);
                    command.Append("\"");
                    command.Append(" && ");
                }
                command.Append(Environment.CommandLine);
            }
            else
            {
                process.StartInfo.FileName = "/bin/bash";

                // Wait a moment for exit the editor.
                command.Append("-c \" sleep 5 && ");
                foreach (var path in directoryPaths)
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
                foreach (var path in nugetDirectoryPaths)
                {
                    command.Append("rm -rf '");
                    command.Append(path);
                    command.Append("'");
                    command.Append(" && ");
                }
                command.Append(Environment.CommandLine);
                command.Append("\"");
            }
            process.StartInfo.Arguments = command.ToString();
            return process;
        }

        private static readonly Dictionary<string, List<string>> enableArch = new Dictionary<string, List<string>>
        {
            {
                nameof(OSType.win), new List<string> {
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86),
                    nameof(ArchitectureType.arm64),
                    nameof(ArchitectureType.arm)
                }
            },
            {
                nameof(OSType.osx), new List<string> {
                    nameof(ArchitectureType.x64),
#if UNITY_2020_2_OR_NEWER
                    nameof(ArchitectureType.arm64),
#endif         
                }
            },
            {
                nameof(OSType.android), new List<string>{
                    nameof(ArchitectureType.arm64),
                    nameof(ArchitectureType.arm),
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.ios), new List<string>{
                    nameof(ArchitectureType.arm64),
                    nameof(ArchitectureType.arm)
                }
            },
            {
                nameof(OSType.linux), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.ubuntu), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.debian), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.fedora), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.centos), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.alpine), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.rhel), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.arch), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.opensuse), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
            {
                nameof(OSType.gentoo), new List<string>{
                    nameof(ArchitectureType.x64),
                    nameof(ArchitectureType.x86)
                }
            },
        };
    }
}
