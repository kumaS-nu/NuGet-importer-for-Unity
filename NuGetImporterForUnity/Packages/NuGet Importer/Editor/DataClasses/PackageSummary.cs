using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    public class PackageSummary
    {
        public string PackageId { get; private set; }
        public string Title { get; private set; }
        public Texture2D Image { get; private set; }
        public string InstalledVersion { get; set; }
        public string SelectedVersion { get; set; }
        public List<string> AllVersion { get; private set; }
        public List<string> StableVersion { get; private set; }

        internal PackageSummary(Datum data, string installedVersion) : this(data.id, data.title, data.icon, installedVersion, data.GetAllVersion().AsEnumerable().Reverse().ToList(), installedVersion) { }
        internal PackageSummary(Catalog data, string selectedVersion)
        {
            Catalogentry catalogEntry = data.GetAllCatalogEntry().First(entry => entry.version == selectedVersion);
            PackageId = catalogEntry.id;
            Title = catalogEntry.title;
            Image = data.icon;
            SelectedVersion = selectedVersion;
            InstalledVersion = selectedVersion;
            AllVersion = data.GetAllVersion().AsEnumerable().Reverse().ToList();
            StableVersion = AllVersion.Where(version => !version.Contains('-') && version[0] != '0').ToList();
        }

        public PackageSummary(string id, string title, Texture2D image, string selectedVersion, List<string> allVersion, string installedVersion = null)
        {
            PackageId = id;
            Title = title;
            Image = image;
            SelectedVersion = selectedVersion;
            InstalledVersion = installedVersion;
            AllVersion = allVersion;
            StableVersion = AllVersion.Where(version => !version.Contains('-') && version[0] != '0').ToList();
        }
    }
}
