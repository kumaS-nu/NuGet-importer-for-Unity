using System;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>List of available API</para>
    /// <para>利用可能なAPIの一覧</para>
    /// </summary>
    [Serializable]
    public class APIList
    {
        public string version;

        /// <value>
        /// <para>Infomation of API</para>
        /// <para>API情報</para>
        /// </value>
        public Resource[] resources;
    }

    [Serializable]
    public class Resource
    {
        /// <value>
        /// <para>Entry point of API</para>
        /// <para>APIのエントリーポイント。URL。</para>
        /// </value>
        public string nuget_id;
        public string nuget_type;

        /// <value>
        /// <para>Comment of API</para>
        /// <para>APIについてのコメント。説明が書いてある。</para>
        /// </value>
        public string comment;
        public string clientVersion;
    }
}