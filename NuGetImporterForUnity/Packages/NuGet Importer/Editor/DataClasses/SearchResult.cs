using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Search results in NuGet.</para>
    /// <para>NuGetで検索した結果。</para>
    /// </summary>
    [Serializable]
    public class SearchResult
    {
        public int totalHits;

        /// <value>
        /// <para>Packages found in the search</para>
        /// <para>検索にヒットしたパッケージ。</para>
        /// </value>
        public Datum[] data;
    }

    /// <summary>
    /// <para>Package information returned by the search</para>
    /// <para>検索で返ってくるパッケージの情報。</para>
    /// </summary>
    [Serializable]
    public class Datum
    {
        public string nuget_id;
        public string nuget_type;
        public string registration;
        public string id;
        public string version;
        public string description;
        public string summary;
        public string title;
        public string iconUrl;
        public string licenseUrl;
        public string projectUrl;
        public string[] tags;
        public string[] authors;
        public int totalDownloads;
        public bool verified;
        public Packagetype[] packageTypes;
        public PackageVersionInformation[] versions;
        [NonSerialized]
        public Texture2D icon;

        /// <summary>
        /// <para>Get all versions of catalog.</para>
        /// <para>全てのバージョンを取得する。</para>
        /// </summary>
        /// <returns>versions</returns>
        public List<string> GetAllVersion()
        {
            return versions.Select(ver => ver.version).ToList();
        }
    }

    [Serializable]
    public class Packagetype
    {
        public string name;
    }

    [Serializable]
    public class PackageVersionInformation
    {
        public string version;
        public int downloads;
        public string nuget_id;
    }
}
