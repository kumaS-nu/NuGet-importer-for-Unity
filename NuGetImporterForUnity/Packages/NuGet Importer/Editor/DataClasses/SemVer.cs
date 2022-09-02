using System;
using System.Collections.Generic;
using System.Linq;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Simple Semantic Versioning</para>
    /// <para>簡易的なセマンティックバージョニング(2.0.0)</para>
    /// </summary>
    public class SemVer
    {
        private string allowedVersion = "";
        private List<string> selectedVersion = new List<string>();
        private List<string> minimumVersion;
        private bool excludeMinimum = false;
        private List<string> maximumVersion;
        private bool excludeMaximum = false;
        private List<List<string>> existVersion;
        private List<string> _existVersion;

        /// <summary>
        /// <para>Returns a mathematical representation of given versions.</para>
        /// <para>与えられた許可されたバージョンを数学的表現に直して返す。</para>
        /// </summary>
        /// <param name="allowedVersion">
        /// <para>Interval notation for specifying version ranges in NuGet.<see cref="https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges"/></para>
        /// <para>NuGetで用いられるバージョン範囲を指定するために間隔表記。<see cref="https://docs.microsoft.com/ja-jp/nuget/concepts/package-versioning#version-ranges"/></para>
        /// </param>
        /// <returns>
        /// <para>Mathematical representation</para>
        /// <para>数学的表現</para>
        /// </returns>
        public static string ToMathExpression(string allowedVersion)
        {
            var ret = "";
            var splitedNotion = allowedVersion.Replace(" ", "").Split(',');
            if (splitedNotion.Length == 1)
            {
                if (allowedVersion.StartsWith("["))
                {
                    ret = "v = " + allowedVersion.Substring(1, allowedVersion.Length - 2);
                }
                else
                {
                    ret = allowedVersion;
                    ret += " <= v";
                }
            }
            else if (splitedNotion.Length == 2)
            {
                var minimumSynbol = splitedNotion[0].StartsWith("(") ? " < " : " <= ";
                var maximumSynbol = splitedNotion[1].EndsWith(")") ? " < " : " <= ";
                var minimum = splitedNotion[0].Length == 1 ? "" : splitedNotion[0].Substring(1) + minimumSynbol;
                var maximum = splitedNotion[1].Length == 1 ? "" : maximumSynbol + splitedNotion[1].Remove(splitedNotion[1].Length - 1);
                ret = minimum + "v" + maximum;
            }

            return ret;
        }

        /// <summary>
        /// <para>Sort the given version in descending order.</para>
        /// <para>与えられたバージョンを降順にソートする。</para>
        /// </summary>
        /// <param name="versions">
        /// <para>Versions to be sorted.</para>
        /// <para>ソートされるバージョン。</para>
        /// </param>
        /// <returns>
        /// <para>Sorted version</para>
        /// <para>ソートされたバージョン</para>
        /// </returns>
        public static List<string> SortVersion(IEnumerable<string> versions)
        {
            var SortedVersion = new List<List<string>>();
            foreach (var version in versions)
            {
                var index = SortedVersion.Count;
                var splitedVersion = version.Split('.').ToList();
                for (; index > 0; index--)
                {
                    if (CompareVersion(SortedVersion[index - 1], splitedVersion) >= 0)
                    {
                        break;
                    }
                }

                SortedVersion.Insert(index, splitedVersion);
            }

            return SortedVersion.Select(v => v.Aggregate((now, next) => now + "." + next)).ToList();
        }

        /// <value>
        /// <para>Allowed version</para>
        /// <para>許可されたバージョン。</para>
        /// </value>
        public string AllowedVersion
        {
            get => allowedVersion;
            set
            {
                allowedVersion = value;
                var splitedNotion = value.Replace(" ", "").Split(',');
                if (splitedNotion.Length == 1)
                {
                    excludeMinimum = false;
                    if (value.StartsWith("["))
                    {
                        excludeMaximum = false;
                        var version = value.Substring(1, value.Length - 2).Split('.').ToList();
                        minimumVersion = version;
                        maximumVersion = version;
                    }
                    else
                    {
                        minimumVersion = value.Split('.').ToList();
                        maximumVersion = null;
                    }
                }
                else if (splitedNotion.Length == 2)
                {
                    excludeMinimum = splitedNotion[0].StartsWith("(");
                    excludeMaximum = splitedNotion[1].EndsWith(")");
                    minimumVersion = splitedNotion[0].Length == 1 ? null : splitedNotion[0].Substring(1).Split('.').ToList();
                    maximumVersion = splitedNotion[1].Length == 1 ? null : splitedNotion[1].Remove(splitedNotion[1].Length - 1).Split('.').ToList();
                }
            }
        }

        /// <value>
        /// <para>Selected version</para>
        /// <para>選択されたバージョン</para>
        /// </value>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when given version is not in the exist version.</para>
        /// <para>与えられたversionがexist versionの中にない場合にthrowされる。</para>
        /// </exception>
        /// <exception cref="System.IndexOutOfRangeException">
        /// <para>Thrown when given version is not allowed.</para>
        /// <para>与えられたバージョンが許可されたものの中にない場合にthrowされる。</para>
        /// </exception>
        public string SelectedVersion
        {
            get => selectedVersion.Aggregate((now, next) => now + "." + next);
            set
            {
                if (_existVersion == null)
                {
                    selectedVersion = value.Split('.').ToList();
                    return;
                }

                if (_existVersion.All(v => v != value))
                {
                    throw new ArgumentException(value + " is not exist version.");
                }

                var splitedVersion = value.Split('.').ToList();
                if (IsAllowedVersion(splitedVersion))
                {
                    selectedVersion = splitedVersion;
                }
                else
                {
                    throw new IndexOutOfRangeException(value + " is not in allowed version");
                }
            }
        }

        /// <value>
        /// <para>Exist versions. When set, ascending order. Returns in descending order.</para>
        /// <para>存在するバージョン。セットする時は昇順。返るのは降順。</para>
        /// </summary>
        public List<string> ExistVersion
        {
            get => new List<string>(_existVersion);
            set
            {
                _existVersion = value.AsEnumerable().Reverse().ToList();
                existVersion = _existVersion.Select(version => version.Split('.').ToList()).ToList();
            }
        }

        /// <summary>
        /// <para>Return a merged version of this object and the given object.</para>
        /// <para>このオブジェクトと引数のオブジェクトをマージしたものを返す。</para>
        /// </summary>
        /// <param name="newVersion">
        /// <para>Objects to be merged.</para>
        /// <para>マージされるオブジェクト。</para>
        /// </param>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <returns>
        /// <para>Marged Semantic Versioning.</para>
        /// <para>マージされたセマンティックバージョニング。</para>
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when different exist version or no suite version.</para>
        /// <para>exist versionが違うときや最適なバージョンがないときにthrowされる。</para>
        /// </exception>
        public SemVer Marge(SemVer newVersion, bool onlyStable = true)
        {
            var ret = new SemVer();
            if (!ExistVersion.SequenceEqual(newVersion.ExistVersion))
            {
                throw new ArgumentException("These version can't marge. Because exist versions are different.");
            }

            ret.selectedVersion = selectedVersion;
            ret.existVersion = existVersion;
            ret._existVersion = _existVersion;

            if (minimumVersion == null)
            {
                ret.minimumVersion = newVersion.minimumVersion;
            }
            else if (newVersion.minimumVersion == null)
            {
                ret.minimumVersion = minimumVersion;
            }
            else
            {
                var minDiff = CompareVersion(minimumVersion, newVersion.minimumVersion);
                ret.minimumVersion = minDiff > 0 ? minimumVersion : newVersion.minimumVersion;
                if (minDiff > 0)
                {
                    ret.excludeMinimum = excludeMinimum;
                }
                else if (minDiff == 0)
                {
                    ret.excludeMinimum = excludeMinimum || newVersion.excludeMinimum;
                }
                else
                {
                    ret.excludeMinimum = newVersion.excludeMinimum;
                }
            }

            if (maximumVersion == null)
            {
                ret.maximumVersion = newVersion.maximumVersion;
            }
            else if (newVersion.maximumVersion == null)
            {
                ret.maximumVersion = maximumVersion;
            }
            else
            {
                var maxDiff = CompareVersion(maximumVersion, newVersion.maximumVersion);
                ret.maximumVersion = maxDiff < 0 ? maximumVersion : newVersion.maximumVersion;
                if (maxDiff < 0)
                {
                    ret.excludeMaximum = excludeMaximum;
                }
                else if (maxDiff == 0)
                {
                    ret.excludeMaximum = excludeMaximum || newVersion.excludeMaximum;
                }
                else
                {
                    ret.excludeMaximum = newVersion.excludeMaximum;
                }
            }

            try
            {
                ret.GetSuitVersion(onlyStable);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("These version can't marge. Because there is no suitable version.");
            }

            var allowVer = "";
            if (ret.maximumVersion == null && ret.excludeMinimum == false)
            {
                allowVer = ret.minimumVersion == null ? "" : ret.minimumVersion.Aggregate((now, next) => now + "." + next);
            }
            else if (ret.minimumVersion != null && ret.maximumVersion != null && ret.minimumVersion.Aggregate((now, next) => now + "." + next) == ret.maximumVersion.Aggregate((now, next) => now + "." + next) && ret.excludeMinimum == false && ret.excludeMaximum == false)
            {
                allowVer = "[" + ret.minimumVersion.Aggregate((now, next) => now + "." + next) + "]";
            }
            else
            {
                allowVer += ret.excludeMinimum ? "(" : "[";
                allowVer += ret.minimumVersion == null ? "" : ret.minimumVersion.Aggregate((now, next) => now + "." + next);
                allowVer += ",";
                allowVer += ret.maximumVersion == null ? "" : ret.maximumVersion.Aggregate((now, next) => now + "." + next);
                allowVer += ret.excludeMaximum ? ")" : "]";
            }
            ret.allowedVersion = allowVer;

            return ret;
        }

        /// <summary>
        /// <para>Get suitable version.</para>
        /// <para>最適なバージョンを取得する。</para>
        /// </summary>
        /// <param name="onlyStable">
        /// <para>Whether use only stable version.</para>
        /// <para>安定版のみつかうか。</para>
        /// </param>
        /// <param name="method">
        /// <para>How to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// </param>
        /// <returns>
        /// <para>Suitable version</para>
        /// <para>最適なバージョン</para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// <para>Thrown when there is no suitable version.</para>
        /// <para>最適なバージョンがないときthrowされる。</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <para>Thrown when given invalid method.</para>
        /// <para>無効な選択方法が与えられたときthrowされる。</para>
        /// </exception>
        public string GetSuitVersion(bool onlyStable = true, VersionSelectMethod method = VersionSelectMethod.Suit)
        {
            switch (method)
            {
                case VersionSelectMethod.Suit:
                    return GetSuitVersion(onlyStable);
                case VersionSelectMethod.Highest:
                    return GetHighestVersion(onlyStable);
                case VersionSelectMethod.Lowest:
                    return GetLowestVersion(onlyStable);
            }

            throw new ArgumentException("method : " + method + " is invalid");
        }

        private string GetHighestVersion(bool onlyStable)
        {
            List<string> maxVersion = maximumVersion ?? existVersion.First();
            foreach (List<string> version in existVersion.SkipWhile(ver => ver.Aggregate((now, next) => now + next) != maxVersion.Aggregate((now, next) => now + next)).ToArray())
            {
                if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                {
                    continue;
                }

                var maxDiff = maximumVersion != null ? CompareVersion(version, maximumVersion) : -1;
                if (maxDiff == 0 && !excludeMaximum)
                {
                    var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 1;
                    if (minDiff == 0 && !excludeMinimum)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }
                    if (minDiff > 0)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }

                    throw new InvalidOperationException("There is no available version.");
                }
                if (maxDiff < 0)
                {
                    var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 1;
                    if (minDiff == 0 && !excludeMinimum)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }
                    if (minDiff > 0)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }

                    throw new InvalidOperationException("There is no available version.");
                }
            }

            throw new InvalidOperationException("There is no available version.");
        }

        private string GetLowestVersion(bool onlyStable)
        {
            List<string> minVersion = minimumVersion ?? existVersion.Last();
            foreach (List<string> version in existVersion.AsEnumerable().Reverse().SkipWhile(ver => ver.Aggregate((now, next) => now + next) != minVersion.Aggregate((now, next) => now + next)).ToArray())
            {
                if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                {
                    continue;
                }

                var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 11;
                if (minDiff == 0 && !excludeMinimum)
                {
                    var maxDiff = maximumVersion != null ? CompareVersion(version, maximumVersion) : -1;
                    if (maxDiff == 0 && !excludeMaximum)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }
                    if (maxDiff < 0)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }

                    throw new InvalidOperationException("There is no available version.");
                }
                if (minDiff > 0)
                {
                    var maxDiff = maximumVersion != null ? CompareVersion(version, maximumVersion) : -1;
                    if (maxDiff == 0 && !excludeMaximum)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }
                    if (maxDiff < 0)
                    {
                        return version.Aggregate((now, next) => now + "." + next);
                    }

                    throw new InvalidOperationException("There is no available version.");
                }
            }

            throw new InvalidOperationException("There is no available version.");
        }

        private string GetSuitVersion(bool onlyStable)
        {
            if (maximumVersion != null)
            {
                foreach (List<string> version in existVersion.SkipWhile(ver => ver.Aggregate((now, next) => now + next) != maximumVersion.Aggregate((now, next) => now + next)).ToArray())
                {
                    if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                    {
                        continue;
                    }

                    var maxDiff = CompareVersion(version, maximumVersion);
                    if (maxDiff == 0 && !excludeMaximum)
                    {
                        var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 1;
                        if (minDiff == 0 && !excludeMinimum)
                        {
                            return version.Aggregate((now, next) => now + "." + next);
                        }
                        if (minDiff > 0)
                        {
                            return version.Aggregate((now, next) => now + "." + next);
                        }

                        throw new InvalidOperationException("There is no available version.");
                    }
                    if (maxDiff < 0)
                    {
                        var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 1;
                        if (minDiff == 0 && !excludeMinimum)
                        {
                            return version.Aggregate((now, next) => now + "." + next);
                        }
                        if (minDiff > 0)
                        {
                            return version.Aggregate((now, next) => now + "." + next);
                        }

                        throw new InvalidOperationException("There is no available version.");
                    }
                }
                throw new InvalidOperationException("There is no available version.");
            }

            var highList = new List<List<string>>();
            var majorVersion = new List<string>();

            foreach (List<string> version in existVersion)
            {
                if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                {
                    continue;
                }

                if (!majorVersion.Contains(version[0]))
                {
                    highList.Add(version);
                    majorVersion.Add(version[0]);
                }
            }
            highList.Reverse();

            foreach (List<string> high in highList)
            {
                if (IsAllowedVersion(high))
                {
                    return high.Aggregate((now, next) => now + "." + next);
                }
            }

            throw new InvalidOperationException("There is no available version.");
        }

        /// <summary>
        /// <para>Wherether given version is allowed version.</para>
        /// <para>与えられたバージョンが許可されているか。</para>
        /// </summary>
        /// <param name="version">
        /// <para>Version to be checked.</para>
        /// <para>調べるバージョン。</para>
        /// </param>
        /// <returns>
        /// <para>Returns <c>true</c> if allowed.</para>
        /// <para>許可されていれば<c>true</c>が返る。</para>
        /// </returns>
        internal bool IsAllowedVersion(List<string> version)
        {
            var minDiff = minimumVersion != null ? CompareVersion(version, minimumVersion) : 1;
            var maxDiff = maximumVersion != null ? CompareVersion(version, maximumVersion) : -1;

            if (minDiff < 0 || maxDiff > 0)
            {
                return false;
            }

            if (excludeMinimum && minDiff == 0)
            {
                return false;
            }

            if (excludeMaximum && maxDiff == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// <para>Compare version</para>
        /// </summary>
        /// <param name="compareVersion">compare version</param>
        /// <param name="baseVersion">base version</param>
        /// <returns>If compareVersion is newer version than baseVersion, return positive number. If baseVersion is newer version than compareVersion, return negative number. If both are same version, return 0.</returns>
        private static int CompareVersion(List<string> compareVersion, List<string> baseVersion)
        {
            var minLength = compareVersion.Count < baseVersion.Count ? compareVersion.Count : baseVersion.Count;
            for (var i = 0; i < minLength; i++)
            {
                var splitedCompare = compareVersion[i].Split('+').First().Split('-');
                var splitedBase = baseVersion[i].Split('+').First().Split('-');
                var isNumberCompare = int.TryParse(splitedCompare.First(), out var c);
                var isNumberBase = int.TryParse(splitedBase.First(), out var b);

                if (isNumberCompare == true && isNumberBase == true)
                {
                    if (c != b)
                    {
                        return c - b;
                    }
                }
                else if (isNumberCompare == false && isNumberBase == false)
                {
                    var compare = string.CompareOrdinal(splitedCompare[0], splitedBase[0]);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }
                else if (isNumberBase == true)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }

                if (splitedCompare.Length >= 2 && splitedBase.Length >= 2)
                {
                    var compare = string.CompareOrdinal(splitedCompare[1], splitedBase[1]);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }
                else
                {
                    if (splitedCompare.Length >= 2)
                    {
                        return -1;
                    }
                    if (splitedBase.Length >= 2)
                    {
                        return 1;
                    }
                }
            }

            var lengthDiff = compareVersion.Count - baseVersion.Count;
            if (lengthDiff < 0)
            {
                return -1;
            }
            if (lengthDiff > 0)
            {
                return 1;
            }

            return 0;
        }
    }
}