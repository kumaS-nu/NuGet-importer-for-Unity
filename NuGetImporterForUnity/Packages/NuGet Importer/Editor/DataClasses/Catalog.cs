using System;

using UnityEngine;


namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Catalog of package.</para>
    /// <para>パッケージのカタログ。</para>
    /// </summary>
    [Serializable]
    public partial class Catalog
    {
        public string nuget_id;
        public string[] nuget_type;
        public string commitId;
        public string commitTimeStamp;
        public int count;

        /// <value>
        /// <para>CatalogEntrys</para>
        /// <para>CatalogEntryがこの中に入っている。</para>
        /// </value>
        public Item[] items;
    }

    [Serializable]
    public class Item
    {
        public string nuget_id;
        public string nuget_type;
        public string commitId;
        public string commitTimeStamp;
        public int count;

        /// <value>
        /// <para>CatalogEntrys</para>
        /// <para>CatalogEntryがこの中に入っている。</para>
        /// </value>
        public Item1[] items;
        public string parent;
        public string lower;
        public string upper;
    }

    [Serializable]
    public class Item1
    {
        public string nuget_id;
        public string nuget_type;
        public string commitId;
        public string commitTimeStamp;

        /// <value>
        /// <para>CatalogEntry</para>
        /// <para>CatalogEntryがこの中に入っている。</para>
        /// </value>
        public Catalogentry catalogEntry;
        public string packageContent;
        public string registration;
    }

    /// <summary>
    /// <para>Infomation of package.</para>
    /// <para>パッケージのバージョンごとの情報。</para>
    /// </summary>
    [Serializable]
    public class Catalogentry
    {
        public string nuget_id;
        public string nuget_type;
        public string authors;
        public string description;
        public string iconUrl;
        public string id;
        public string language;
        public string licenseExpression;
        public string licenseUrl;
        public bool listed;
        public string minClientVersion;
        public string packageContent;
        public string projectUrl;
        public string published;
        public bool requireLicenseAcceptance;
        public string summary;
        public string[] tags;
        public string title;
        public string version;
        public Dependencygroup[] dependencyGroups;
        [NonSerialized]
        public Texture2D icon;
    }

    [Serializable]
    public class Dependencygroup
    {
        public string nuget_id;
        public string nuget_type;
        public string targetFramework;
        public Dependency[] dependencies;
    }

    [Serializable]
    public class Dependency
    {
        public string nuget_id;
        public string nuget_type;
        public string id;
        public string range;
        public string registration;
    }

}

