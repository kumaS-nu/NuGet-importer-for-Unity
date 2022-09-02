using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class that provides a method to resolve package dependencies.</para>
    /// <para>依存関係を解決するメソッドを提供するクラス。</para>
    /// </summary>
    public static class DependencySolver
    {
        /// <summary>
        /// <para>Find the required packages in the installed and specified packages.</para>
        /// <para>インストールされたパッケージと指定したパッケージを元に必要なパッケージを探す。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id</para>
        /// <para>パッケージのid</para>
        /// </param>
        /// <param name="version">
        /// <para>Package version</para>
        /// <para>パッケージのバージョン</para>
        /// </param>
        /// <param name="controlledPackages">
        /// <para>List of packages currently under control.</para>
        /// <para>現在制御下にあるパッケージ一覧。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Required packages include installed and specified ones.</para>
        /// <para>インストール済みや、指定したものを含んだ必要なパッケージ。</para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// <para>Thrown when dependency conflict occered.</para>
        /// <para>依存関係に衝突があるときthrowされる。</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when there is a circular reference or there is no version of package.</para>
        /// <para>循環参照があるときまたはパッケージのバージョンがないときthrowされる。</para>
        /// </exception>
        public static async Task<List<Package>> FindRequiredPackages(string packageId, string version, ReadOnlyControlledPackages controlledPackages, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            IEnumerable<(string packageId, string version)> packageList = new List<(string packageId, string version)>() { (packageId, version) };
            packageList = packageList.Concat(controlledPackages.root.Select(package => (package.id, package.version)));
            IEnumerable<Package> requiredPackages = await GetInputedRequiredPackageList(packageList, onlyStable, method);
            return requiredPackages.ToList();
        }

        /// <summary>
        /// <para>Find the required packages when change package version.</para>
        /// <para>パッケージのバージョンを変更する時に必要なパッケージを探す。</para>
        /// </summary>
        /// <param name="packageId">
        /// <para>Package id</para>
        /// <para>パッケージのid</para>
        /// </param>
        /// <param name="version">
        /// <para>Package version</para>
        /// <para>パッケージのバージョン</para>
        /// </param>
        /// <param name="controlledPackages">
        /// <para>List of packages currently under control.</para>
        /// <para>現在制御下にあるパッケージ一覧。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Required packages include installed and specified ones.</para>
        /// <para>インストール済みや、指定したものを含んだ必要なパッケージ。</para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// <para>Thrown when dependency conflict occered.</para>
        /// <para>依存関係に衝突があるときthrowされる。</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when there is a circular reference or there is no version of package.</para>
        /// <para>循環参照があるときまたはパッケージのバージョンがないときthrowされる。</para>
        /// </exception>
        public static async Task<List<Package>> FindRequiredPackagesWhenChangeVersion(string packageId, string version, ReadOnlyControlledPackages controlledPackages, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            IEnumerable<(string packageId, string version)> packageList = new List<(string packageId, string version)>();
            packageList = controlledPackages.root.Where(package => package.id != packageId).Select(package => (package.id, package.version));
            packageList = packageList.Append((packageId, version)).ToArray();
            IEnumerable<Package> requiredPackages = await GetInputedRequiredPackageList(packageList, onlyStable, method);
            return requiredPackages.ToList();
        }

        /// <summary>
        /// <para>Find the required packages in the installed package.</para>
        /// <para>インストール済みのものから必要なパッケージを探す。</para>
        /// </summary>
        /// <param name="controlledPackages">
        /// <para>List of packages currently under control.</para>
        /// <para>現在制御下にあるパッケージ一覧。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// <returns>
        /// <para>Required packages include installed and specified ones.</para>
        /// <para>インストール済みや、指定したものを含んだ必要なパッケージ。</para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// <para>Thrown when dependency conflict occered.</para>
        /// <para>依存関係に衝突があるときthrowされる。</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when there is a circular reference or there is no version of package.</para>
        /// <para>循環参照があるときまたはパッケージのバージョンがないときthrowされる。</para>
        /// </exception>
        public static async Task<List<Package>> CheckAllPackage(ReadOnlyControlledPackages controlledPackages, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            IEnumerable<(string packageId, string version)> packageList = new List<(string packageId, string version)>();
            packageList = controlledPackages.root.Select(package => (package.id, package.version)).ToArray();
            IEnumerable<Package> requiredPackages = await GetInputedRequiredPackageList(packageList, onlyStable, method);
            return requiredPackages.ToList();
        }

        /// <summary>
        /// <para>Find removable packages when the specified package is removed.</para>
        /// <para>特定のパッケージを除去した際取り除けるパッケージを探す。</para>
        /// </summary>
        /// <param name="removePackageId">
        /// <para>Remove package.</para>
        /// <para>除去するパッケージ。</para>
        /// </param>
        /// <param name="controlledPackages">
        /// <para>List of packages currently under control.</para>
        /// <para>現在制御下にあるパッケージ一覧。</para>
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
        /// <para>Removable packages.</para>
        /// <para>除去可能なパッケージ。</para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// <para>Thrown when dependency conflict occered.</para>
        /// <para>依存関係に衝突があるときthrowされる。</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when there is a circular reference or there is no version of package.</para>
        /// <para>循環参照があるときまたはパッケージのバージョンがないときthrowされる。</para>
        /// </exception>
        public static async Task<List<Package>> FindRemovablePackages(string removePackageId, ReadOnlyControlledPackages controlledPackages, bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            IEnumerable<(string packageId, string version)> installed = new List<(string packageId, string version)>();
            installed = controlledPackages.root.Where(package => removePackageId != package.id).Select(package => { return (package.id, package.version); }).ToArray();
            IEnumerable<DependencyNode> allPackages = await GetInputedDependencyList(installed, onlyStable, method);
            IEnumerable<string> allPackagesName = allPackages.Select(package => package.PackageName);
            return PackageManager.Installed.package.Where(pkg => !allPackagesName.Contains(pkg.id)).ToList();
        }

        private static async Task<IEnumerable<Package>> GetInputedRequiredPackageList(IEnumerable<(string, string)> packages, bool onlyStable, VersionSelectMethod method)
        {
            IEnumerable<DependencyNode> dependency = await GetInputedDependencyList(packages, onlyStable, method);
            return dependency.Select(package =>
            {
                var pkg = new Package
                {
                    id = package.PackageName,
                    targetFramework = package.TragetFramework,
                    versionField = package.Version
                };
                return pkg;
            });
        }

        private static async Task<IEnumerable<DependencyNode>> GetInputedDependencyList(IEnumerable<(string, string)> packages, bool onlyStable, VersionSelectMethod method)
        {
            var confirmedNode = new List<DependencyNode>();
            var tasks = new List<Task>();
            var allNode = new Dictionary<string, List<DependencyNode>>();
            List<string> targetFramework = FrameworkName.TARGET;

            // First, search for packages that may be necessary.
            foreach ((var packageId, var version) in packages)
            {
                var node = new DependencyNode(packageId, "[" + version + "]");
                lock (tasks)
                {
                    tasks.Add(FindDependency(node, targetFramework, allNode, onlyStable, method));
                }
            }
            await Task.WhenAll(tasks);

            // Find the package with the top-level dependency.
            foreach (KeyValuePair<string, List<DependencyNode>> dic in allNode)
            {
                if (dic.Value.Count == 1 && dic.Value[0].dependedNode.Count == 0)
                {
                    confirmedNode.Add(dic.Value[0]);
                }
            }

            // NuGet does not allow circular references, so we can always determine the package from the top-level dependency.
            while (confirmedNode.Count != allNode.Count)
            {
                IEnumerable<string> confirmedPackage = confirmedNode.Select(package => package.PackageName);
                var topNodes = confirmedNode.SelectMany(node => node.dependingNode).GroupBy(node => node.PackageName).Select(group => group.First()).Where(node =>
                {
                    IEnumerable<string> topParent = node.dependedNode.Select(parent => parent.PackageName).Except(confirmedPackage);
                    if (topParent == null || !topParent.Any())
                    {
                        if (confirmedPackage.Contains(node.PackageName))
                        {
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToList();

                for (var i = 0; i < topNodes.Count; i++)
                {
                    List<DependencyNode> topNode = allNode[topNodes[i].PackageName];
                    if (topNode.Count == 1)
                    {
                        confirmedNode.Add(topNode[0]);
                    }
                    else
                    {
                        var dependedPackage = new List<DependencyNode>();
                        SemVer margedVersion = topNode[0].Version;
                        dependedPackage.AddRange(topNode[0].dependedNode);
                        foreach (DependencyNode top in topNode)
                        {
                            dependedPackage.AddRange(top.dependedNode);
                            try
                            {
                                margedVersion = margedVersion.Marge(top.Version, onlyStable);
                            }
                            catch (ArgumentException)
                            {
                                throw new InvalidOperationException("Found dependency conflict. At\n" + string.Join("\n", topNode.SelectMany(node => node.dependedNode).Select(depended => depended.PackageName + " " + depended.Version.SelectedVersion)));
                            }
                        }
                        var margeNode = new DependencyNode(topNode[0].PackageName, margedVersion.AllowedVersion);
                        while (topNode != null && topNode.Count != 0)
                        {
                            DeleteDependency(topNode[0], allNode);
                        }

                        margeNode.dependedNode.AddRange(dependedPackage);
                        await FindDependency(margeNode, targetFramework, allNode, onlyStable, method);
                        confirmedNode.Add(margeNode);
                    }
                }
            }

            return confirmedNode;
        }

        private static async Task FindDependency(DependencyNode node, List<string> targetFramework, Dictionary<string, List<DependencyNode>> allNode, bool onlyStable, VersionSelectMethod method)
        {
            var tasks = new List<Task>();
            var isInstalled = false;
            isInstalled = PackageManager.InstalledCatalog.ContainsKey(node.PackageName);
            Catalog catalog = isInstalled ? PackageManager.InstalledCatalog[node.PackageName] : await NuGet.GetCatalog(node.PackageName);
            node.Version.ExistVersion = catalog.GetAllVersion();
            node.Version.SelectedVersion = node.Version.GetSuitVersion(onlyStable, method);
            lock (allNode)
            {
                if (allNode.TryGetValue(node.PackageName, out List<DependencyNode> samePackage))
                {
                    samePackage.Add(node);
                }
                else
                {
                    allNode.Add(node.PackageName, new List<DependencyNode>() { node });
                }

            }

            Catalogentry catalogEntry = catalog.GetAllCatalogEntry().FirstOrDefault(entry => entry.version == node.Version.SelectedVersion);

            if (catalogEntry == default)
            {
                throw new ArgumentException("There is no such version of that package.");
            }

            Dependencygroup[] dependencyGroup = catalogEntry.dependencyGroups;
            if (dependencyGroup == null)
            {
                node.TragetFramework = targetFramework.First();
                return;
            }
            IEnumerable<Dependencygroup> dependencyGroups = dependencyGroup.Where(group => group.targetFramework == null || group.targetFramework == "" || targetFramework.Contains(group.targetFramework));
            if (dependencyGroups == null || !dependencyGroups.Any())
            {
                node.TragetFramework = targetFramework.First();
            }
            else
            {
                var dependencies = new List<Dependency>();
                IEnumerable<Dependencygroup> dependAllGroup = dependencyGroups.Where(depend => depend.targetFramework == null || depend.targetFramework == "");
                if (dependAllGroup.Any())
                {
                    dependencies.AddRange(dependAllGroup.First().dependencies);
                    node.TragetFramework = targetFramework.First();
                }

                IOrderedEnumerable<Dependencygroup> dependGroups = dependencyGroups.Except(dependAllGroup).OrderBy(depend =>
                {
                    var ret = targetFramework.IndexOf(depend.targetFramework);
                    return ret < 0 ? int.MaxValue : ret;
                });

                if (dependGroups.Any())
                {
                    Dependencygroup dependGroup = dependGroups.First();
                    node.TragetFramework = dependGroup.targetFramework;
                    if (dependGroups.First().dependencies != null)
                    {
                        dependencies.AddRange(dependGroup.dependencies);
                    }
                }

                dependencies.RemoveAll(d => StandardLibraries.PackageIds.Contains(d.id));

                if (dependencies.Any())
                {
                    foreach (Dependency dependency in dependencies)
                    {
                        tasks.Add(FindChildDependency(dependency.id, dependency.range, node, targetFramework, allNode, onlyStable, method));
                    }
                }

                if (node.TragetFramework == null || node.TragetFramework == "")
                {
                    node.TragetFramework = targetFramework.First();
                }
            }

            await Task.WhenAll(tasks);
        }

        private static async Task FindChildDependency(string packageName, string allowedVersion, DependencyNode parent, List<string> targetFramework, Dictionary<string, List<DependencyNode>> allNode, bool onlyStable, VersionSelectMethod method)
        {
            var childNode = new DependencyNode(packageName, allowedVersion);
            parent.dependingNode.Add(childNode);
            childNode.dependedNode.Add(parent);
            EnsureNoCircularReference(childNode);
            await FindDependency(childNode, targetFramework, allNode, onlyStable, method);
        }

        private static void EnsureNoCircularReference(DependencyNode node, string targetName = "", List<string> log = null)
        {
            if (targetName == "")
            {
                targetName = node.PackageName;
            }

            if (log == null)
            {
                log = new List<string>() { targetName };
            }

            foreach (DependencyNode depended in node.dependedNode)
            {
                log.Add(depended.PackageName);

                if (depended.PackageName == targetName)
                {
                    log.Reverse();
                    throw new ArgumentException("Find circular reference." + log.Aggregate((now, next) => now + " -> " + next));
                }

                EnsureNoCircularReference(depended, targetName, log);
            }
        }

        private static void DeleteDependency(DependencyNode node, Dictionary<string, List<DependencyNode>> allNode)
        {
            foreach (DependencyNode parent in node.dependedNode)
            {
                parent.dependingNode.Remove(node);
            }

            lock (allNode)
            {
                allNode[node.PackageName].Remove(node);
                if (allNode[node.PackageName].Count == 0)
                {
                    allNode.Remove(node.PackageName);
                }
            }

            // Delete in reverse order so that the index doesn't change.
            for (var i = node.dependingNode.Count - 1; i >= 0; i--)
            {
                DeleteDependency(node.dependingNode[i], allNode);
            }

            node.dependedNode.Clear();
            node.dependingNode.Clear();
        }
    }
}
