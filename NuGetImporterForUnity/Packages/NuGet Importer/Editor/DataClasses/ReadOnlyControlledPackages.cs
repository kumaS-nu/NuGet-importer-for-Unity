using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>List of packages under management that can only be read.</para>
    /// <para>読み込みのみ可能な管理下にあるパッケージ一覧。</para>
    /// </summary>
    public class ReadOnlyControlledPackages
    {
        public readonly IList<Package> Installed;
        public readonly IList<Package> Root;
        public readonly IList<Package> Existing;

        public ReadOnlyControlledPackages(
            InstalledPackages installedPackage,
            InstalledPackages rootPackage,
            InstalledPackages existingPackage
        )
        {
            Installed = installedPackage.Package.AsReadOnly();
            Root = rootPackage.Package.AsReadOnly();
            Existing = existingPackage.Package.AsReadOnly();
        }

        private ReadOnlyControlledPackages(
            IEnumerable<Package> installedPackage,
            IEnumerable<Package> rootPackage,
            IEnumerable<Package> existingPackage
        )
        {
            Installed = new List<Package>(installedPackage).AsReadOnly();
            Root = new List<Package>(rootPackage).AsReadOnly();
            Existing = new List<Package>(existingPackage).AsReadOnly();
        }

        public ReadOnlyControlledPackages Clone()
        {
            return new ReadOnlyControlledPackages(Installed, Root, Existing);
        }
    }
}
