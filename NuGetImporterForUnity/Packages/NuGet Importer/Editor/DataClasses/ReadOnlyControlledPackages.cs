using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>List of packages under management that can only be read.</para>
    /// <para>読み込みのみ可能な管理下にあるパッケージ一覧。</para>
    /// </summary>
    public class ReadOnlyControlledPackages
    {
        public readonly IList<Package> installed;
        public readonly IList<Package> root;
        public readonly IList<Package> existing;

        public ReadOnlyControlledPackages(InstalledPackages installedPackage, InstalledPackages rootPackage, InstalledPackages existingPackage)
        {
            installed = installedPackage.package.AsReadOnly();
            root = rootPackage.package.AsReadOnly();
            existing = existingPackage.package.AsReadOnly();
        }

        private ReadOnlyControlledPackages(IList<Package> installedPackage, IList<Package> rootPackage, IList<Package> existingPackage)
        {
            installed = new List<Package>(installedPackage).AsReadOnly();
            root = new List<Package>(rootPackage).AsReadOnly();
            existing = new List<Package>(existingPackage).AsReadOnly();
        }

        public ReadOnlyControlledPackages Clone()
        {
            return new ReadOnlyControlledPackages(installed, root, existing);
        }
    }
}
