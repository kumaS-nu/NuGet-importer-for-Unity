using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

namespace kumaS.NuGetImporter.Editor.PackageOperation
{
    /// <summary>
    /// <para>Install the package.</para>
    /// <para>パッケージをインストールする。</para>
    /// </summary>
    internal sealed class InstallPackage : OperatePackage
    {
        private readonly string _id;
        private readonly string _version;
        private readonly bool _onlyStable;
        private readonly VersionSelectMethod _method;

        protected override string FinishMessage { get => "Installation finished."; }

        public InstallPackage(
            string packageId,
            string installVersion,
            bool isOnlyStable = true,
            VersionSelectMethod versionSelect = VersionSelectMethod.Suit
        )
        {
            _onlyStable = isOnlyStable;
            _method = versionSelect;
            _id = packageId;
            _version = installVersion;
        }

        protected override async Task<OperationResult> Operate(
            ReadOnlyControlledPackages controlledPackages,
            PackageManager.OperateLock operateLock
        )
        {
            IEnumerable<Package> requiredPackages = await DependencySolver.FindRequiredPackages(
                _id,
                _version,
                controlledPackages,
                _onlyStable,
                _method
            );
            requiredPackages = requiredPackages
                               .Where(package => controlledPackages.Existing.All(exist => package.ID != exist.ID))
                               .ToArray();

            var rootPackages = requiredPackages
                               .Where(req => controlledPackages.Root.Any(root => root.ID == req.ID))
                               .Concat(requiredPackages.Where(req => req.ID == _id))
                               .ToArray();
            var installPackages = requiredPackages.Where(
                                                      package => !controlledPackages.Installed.Any(
                                                          install => install.ID == package.ID
                                                                     && install.Version == package.Version
                                                      )
                                                  )
                                                  .ToArray();
            var deletePackages = controlledPackages.Installed.Where(
                                                       install => requiredPackages.Any(
                                                           dep => dep.ID == install.ID
                                                                  && dep.Version != install.Version
                                                       )
                                                   )
                                                   .ToArray();

            return await ManipulatePackages(
                rootPackages,
                installPackages,
                deletePackages,
                controlledPackages,
                operateLock
            );
        }
    }
}
