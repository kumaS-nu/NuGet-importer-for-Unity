using System;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Data about package.json.</para>
    /// <para>package.jsonのデータ。</para>
    /// </summary>
    [Serializable]
    public class PackageJson
    {
        public string name { get; set; }
        public string version { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public string unity { get; set; }
        public string[] keywords { get; set; }
        public Author author { get; set; }
    }

    /// <summary>
    /// <para>Data about author.</para>
    /// <para>作者のデータ。</para>
    /// </summary>
    [Serializable]
    public class Author
    {
        public string name { get; set; }
        public string email { get; set; }
        public string url { get; set; }
    }
}
