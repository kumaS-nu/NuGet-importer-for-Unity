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
}