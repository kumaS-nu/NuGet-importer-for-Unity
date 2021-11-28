using System.Collections.Generic;
using System.Linq;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    public partial class Catalog
    {
        private Catalogentry[] catalogCache;

        /// <summary>
        /// <para>Get all versions of catalog.</para>
        /// <para>全てのバージョンを取得する。</para>
        /// </summary>
        /// <returns>versions</returns>
        public List<string> GetAllVersion()
        {
            return items.SelectMany(item => item.items).Select(item => item.catalogEntry.version).ToList();
        }

        /// <summary>
        /// <para>Get all catalogEntry.</para>
        /// <para>全てのcatalogEntryを取得する。</para>
        /// </summary>
        /// <returns>catalogEntrys</returns>
        public Catalogentry[] GetAllCatalogEntry()
        {
            if (catalogCache == null || catalogCache.Length == 0)
            {
                catalogCache = items.SelectMany(item => item.items).Select(item => item.catalogEntry).ToArray();
            }
            return catalogCache;
        }
    }

    public partial class Catalogentry
    {
        public PackageJson ToPackageJson()
        {
            var author = new Author()
            {
                name = authors,
                url = projectUrl
            };

            var splited = version.Split('.').ToList();
            while(splited.Count < 3)
            {
                splited.Append("0");
            }

            while(splited.Count > 3)
            {
                splited.RemoveAt(splited.Count - 1);
            }

            var packageJson = new PackageJson()
            {
                displayName = title,
                version = string.Join(".", splited),
                name = "org.nuget." + id.ToLowerInvariant(),
                description = description,
                unity = "2018.3",
                keywords = tags,
                author = author
            };

            return packageJson;
        }
    }
}