using System.Collections;
using System.Collections.Generic;
using kumaS.NuGetImporter.Editor.DataClasses;
using System.Linq;
using System.Threading.Tasks;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Install the package.</para>
    /// <para>パッケージをインストールする。</para>
    /// </summary>
    internal sealed class InstallPackage : OperatePackage
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

        protected override async Task<OperationResult> Operate(ReadOnlyControlledPackages controlledPackages, PackageManager.OperateLock operateLock)
        {
            IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackages(id, version, controlledPackages, onlyStable, method);
            requiredPackages = requiredPackages.Where(package => !controlledPackages.existing.Any(exist => package.id == exist.id)).ToArray();

            Package[] rootPackages = requiredPackages.Where(req => controlledPackages.root.Any(root => root.id == req.id)).Concat(requiredPackages.Where(req => req.id == id)).ToArray();
            Package[] installPackages = requiredPackages.Where(package => !controlledPackages.installed.Any(install => install.id == package.id && install.version == package.version)).ToArray();
            Package[] deletePackages = controlledPackages.installed.Where(install => requiredPackages.Any(dep => dep.id == install.id && dep.version != install.version)).ToArray();

            return await ManipulatePackages(rootPackages, installPackages, deletePackages, controlledPackages, operateLock);
        }
    }
}
