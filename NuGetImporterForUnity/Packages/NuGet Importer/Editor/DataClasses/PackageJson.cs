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
        public string name;
        public string version;
        public string displayName;
        public string description;
        public string unity;
        public string[] keywords;
        public Author author;
    }

    /// <summary>
    /// <para>Data about author.</para>
    /// <para>作者のデータ。</para>
    /// </summary>
    [Serializable]
    public class Author
    {
        public string name;
        public string email;
        public string url;
    }
}
