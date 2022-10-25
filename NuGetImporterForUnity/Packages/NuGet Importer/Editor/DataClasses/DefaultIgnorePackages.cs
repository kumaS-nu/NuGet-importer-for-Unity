using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Class of package to ignore by default.</para>
    /// <para>デフォルトで無視するパッケージの一覧。</para>
    /// </summary>
    public static class DefaultIgnorePackages
    {
        private static readonly IReadOnlyList<string> names = new List<string>()
        {
            "Microsoft.CSharp"
        };

        /// <summary>
        /// <para>Package names.</para>
        /// <para>パッケージ名。</para>
        /// </summary>
        public static IReadOnlyList<string> Names { get => names; }
    }
}
