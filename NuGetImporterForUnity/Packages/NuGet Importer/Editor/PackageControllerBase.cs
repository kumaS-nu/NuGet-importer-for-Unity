using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal abstract class PackageControllerBase
    {
        protected readonly static string dataPath = Application.dataPath;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(InstalledPackages));
        private readonly string[] windowsArchs = new string[] { "x86", "x64" };
        private readonly string[] osxArchs = new string[] { "x64"
#if UNITY_2020_2_OR_NEWER
            , "arm64"
#endif
        };
        private readonly string[] androidArchs = new string[] { "arm", "arm64", "x64", "x86" };
        private readonly string[] iosArchs = new string[] { "arm", "arm64", "x64" };
        private readonly List<string> linuxNameList = new List<string> { "linux", "ubuntu", "centos", "debian" };

        /// <summary>
        /// <para>Get the installation path of the package.</para>
        /// <para>パッケージのインストールパスを取得する。</para>
        /// </summary>
        /// <param name="package">
        /// <para>Package.</para>
        /// <para>パッケージ。</para>
        /// </param>
        /// <returns>
        /// <para>Installation path.</para>
        /// <para>インストールパス。</para>
        /// </returns>
        internal abstract Task<string> GetInstallPath(Package package);

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
            foreach (var package in packages)
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
        /// <para>ルートのパッケージ。</para></param>
        /// <returns>
        /// <para>The process of removing native plugins.</para>
        /// <para>ネイティブプラグインを削除するプロセス。</para>
        /// </returns>
        internal async Task<Process> OperateWithNativeAsync(IEnumerable<Package> installs, IEnumerable<Package> manageds, IEnumerable<Package> natives, IEnumerable<Package> allInstalled, IEnumerable<Package> root)
        {
            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillInstall.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = installs.ToArray()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillPackage.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = allInstalled.ToArray()
                };
                serializer.Serialize(file, write);
            }

            using (var file = new StreamWriter(Application.dataPath.Replace("Assets", "WillRoot.xml"), false))
            {
                var write = new InstalledPackages
                {
                    package = root.ToArray()
                };
                serializer.Serialize(file, write);
            }

            await UninstallManagedPackagesAsync(manageds);
            foreach (var native in natives)
            {
                DeletePluginsOutOfDirectory(native);
            }

            var tasks = natives.Select(package => GetInstallPath(package));
            IEnumerable<string> nativeDirectory = await Task.WhenAll(tasks);

            return CreateDeleteNativeProcess(nativeDirectory.ToArray());
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
            var extractPath = await GetInstallPath(package);
            var nupkgName = package.id.ToLowerInvariant() + "." + package.version.ToLowerInvariant() + ".nupkg";
            var tempPath = Application.dataPath.Replace("Assets", "Temp");
            var downloadPath = Path.Combine(tempPath, nupkgName);
            if (!File.Exists(downloadPath))
            {
                await NuGet.GetPackage(package.id, package.version, tempPath);
            }
            DeleteDirectory(extractPath);
            ZipFile.ExtractToDirectory(downloadPath, extractPath);
            DeleteDirectory(Path.Combine(extractPath, "_rels"));
            DeleteDirectory(Path.Combine(extractPath, "ref"));
            DeleteDirectory(Path.Combine(extractPath, "package"));
            DeleteDirectory(Path.Combine(extractPath, "build"));
            DeleteDirectory(Path.Combine(extractPath, "buildMultiTargeting"));
            DeleteDirectory(Path.Combine(extractPath, "buildTransitive"));
            DeleteDirectory(Path.Combine(extractPath, "tools"));

            foreach (var file in Directory.GetFiles(extractPath))
            {
                if (file.Contains(".nuspec") || file.Contains("[Content_Types].xml"))
                {
                    File.Delete(file);
                }
            }

            // Processing Managed Plugins.
            var managedPath = Path.Combine(extractPath, "lib");
            if (Directory.Exists(managedPath))
            {
                var frameworkDictionary = FrameworkName.ALLPLATFORM;
                var targetFramework = frameworkDictionary.FirstOrDefault(framework => framework.Contains(package.targetFramework));
                var frameworkList = FrameworkName.TARGET;
                var target = "";
                var priority = int.MaxValue;
                foreach (var lib in Directory.GetDirectories(managedPath))
                {
                    var dirName = Path.GetFileName(lib);

                    if (targetFramework != default && targetFramework.Contains(dirName))
                    {
                        priority = -1;
                        target = lib;
                    }
                    else if (frameworkList.Contains(dirName) && frameworkList.IndexOf(dirName) < priority)
                    {
                        priority = frameworkList.IndexOf(dirName);
                        target = lib;
                        package.targetFramework = frameworkDictionary.Where(framework => framework.Contains(dirName)).First()[0];
                    }
                }

                foreach (var lib in Directory.GetDirectories(managedPath))
                {
                    if (lib != target)
                    {
                        DeleteDirectory(lib);
                    }
                }
            }

            // Processing Native Plugins
            var nativePath = Path.Combine(extractPath, "runtimes");
            if (Directory.Exists(nativePath))
            {
                var deleteList = new List<string>();
                var target = "";
                var priority = int.MaxValue;
                foreach (var runtime in Directory.GetDirectories(nativePath))
                {
                    DeleteDirectory(Path.Combine(runtime, "lib"));

                    var dirName = Path.GetFileName(runtime);
                    var splitedDirName = dirName.Split('-');

                    void AddDeleteDirectory(string[] arch)
                    {
                        if (splitedDirName.Length > 1 && arch.Contains(splitedDirName[1]))
                        {
                            deleteList.AddRange(Directory.GetDirectories(runtime).Where(path => !path.EndsWith("native")));
                        }
                        else
                        {
                            deleteList.Add(runtime);
                        }
                    }

                    switch (splitedDirName[0])
                    {
                        case "win":
                            AddDeleteDirectory(windowsArchs);
                            break;
                        case "osx":
                            AddDeleteDirectory(osxArchs);
                            break;
                        case "android":
                            AddDeleteDirectory(androidArchs);
                            break;
                        case "ios":
                            AddDeleteDirectory(iosArchs);
                            break;
                        default:
                            var index = linuxNameList.IndexOf(splitedDirName[0]);
                            index = index < 0 ? int.MaxValue : index;
                            if (splitedDirName.Length > 1 && splitedDirName[1] == "x64" && index < priority)
                            {
                                if (target != "")
                                {
                                    deleteList.Add(target);
                                }
                                priority = index;
                                target = runtime;
                            }
                            else
                            {
                                deleteList.Add(runtime);
                            }
                            break;
                    }
                }

                if (target != "" && Directory.GetDirectories(target).Any(path => !path.EndsWith("native")))
                {
                    deleteList.AddRange(Directory.GetDirectories(target).Where(path => !path.EndsWith("native")));
                }

                foreach (var delete in deleteList)
                {
                    DeleteDirectory(delete);
                }

                if (Directory.GetDirectories(nativePath).Length == 0)
                {
                    DeleteDirectory(nativePath);
                }
            }
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
            var path = await GetInstallPath(package);
            var tasks = new List<Task>();
            tasks.Add(Task.Run(() => DeleteDirectory(path)));
            tasks.Add(Task.Run(() => DeletePluginsOutOfDirectory(package)));
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
            {

            }
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
            {

            }
        }

        /// <summary>
        /// <para>Create process that delete native packages.</para>
        /// <para>ネイティブのパッケージを削除するプロセスを作成。</para>
        /// </summary>
        /// <param name="directoryPaths">
        /// <para>Directory paths to delete.</para>
        /// <para>削除するディレクトリのパス。</para>
        /// </param>
        /// <returns>
        /// <para>The process of removing native plugins.</para>
        /// <para>ネイティブプラグインを削除するプロセス。</para>
        /// </returns>
        private Process CreateDeleteNativeProcess(IEnumerable<string> directoryPaths)
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
                command.Append(Environment.CommandLine);
                command.Append("\"");
            }
            process.StartInfo.Arguments = command.ToString();
            return process;
        }
    }
}
