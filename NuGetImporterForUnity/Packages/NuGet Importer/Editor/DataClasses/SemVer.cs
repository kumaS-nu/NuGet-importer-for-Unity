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
        private string _allowedVersion = "";
        private List<string> _selectedVersion = new List<string>();
        private List<string> _minimumVersion;
        private bool _excludeMinimum;
        private List<string> _maximumVersion;
        private bool _excludeMaximum;
        private List<List<string>> _existVersions;
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
            switch (splitedNotion.Length)
            {
                case 1 when allowedVersion.StartsWith("["):
                    ret = "v = " + allowedVersion.Substring(1, allowedVersion.Length - 2);
                    break;
                case 1:
                    ret = allowedVersion;
                    ret += " <= v";
                    break;
                case 2:
                    {
                        var minimumSymbol = splitedNotion[0].StartsWith("(") ? " < " : " <= ";
                        var maximumSymbol = splitedNotion[1].EndsWith(")") ? " < " : " <= ";
                        var minimum = splitedNotion[0].Length == 1 ? "" : splitedNotion[0].Substring(1) + minimumSymbol;
                        var maximum = splitedNotion[1].Length == 1
                            ? ""
                            : maximumSymbol + splitedNotion[1].Remove(splitedNotion[1].Length - 1);
                        ret = minimum + "v" + maximum;
                        break;
                    }
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
            var sortedVersion = new List<List<string>>();
            foreach (var version in versions)
            {
                var index = sortedVersion.Count;
                var splitedVersion = version.Split('.').ToList();
                for (; index > 0; index--)
                {
                    if (CompareVersion(sortedVersion[index - 1], splitedVersion) >= 0)
                    {
                        break;
                    }
                }

                sortedVersion.Insert(index, splitedVersion);
            }

            return sortedVersion.Select(v => v.Aggregate((now, next) => now + "." + next)).ToList();
        }

        /// <value>
        /// <para>Allowed version</para>
        /// <para>許可されたバージョン。</para>
        /// </value>
        public string AllowedVersion
        {
            get => _allowedVersion;
            set
            {
                _allowedVersion = value;
                var splitedNotion = value.Replace(" ", "").Split(',');
                switch (splitedNotion.Length) {
                    case 1: {
                            _excludeMinimum = false;
                            if (value.StartsWith("["))
                            {
                                _excludeMaximum = false;
                                var version = value.Substring(1, value.Length - 2).Split('.').ToList();
                                _minimumVersion = version;
                                _maximumVersion = version;
                            }
                            else
                            {
                                _minimumVersion = value.Split('.').ToList();
                                _maximumVersion = null;
                            }

                            break;
                        }
                    case 2:
                        _excludeMinimum = splitedNotion[0].StartsWith("(");
                        _excludeMaximum = splitedNotion[1].EndsWith(")");
                        _minimumVersion = splitedNotion[0].Length == 1
                            ? null
                            : splitedNotion[0].Substring(1).Split('.').ToList();
                        _maximumVersion = splitedNotion[1].Length == 1
                            ? null
                            : splitedNotion[1].Remove(splitedNotion[1].Length - 1).Split('.').ToList();
                        break;
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
            get => _selectedVersion.Aggregate((now, next) => now + "." + next);
            set
            {
                if (_existVersion == null)
                {
                    _selectedVersion = value.Split('.').ToList();
                    return;
                }

                if (_existVersion.All(v => v != value))
                {
                    throw new ArgumentException(value + " is not exist version.");
                }

                var splitedVersion = value.Split('.').ToList();
                _selectedVersion = IsAllowedVersion(splitedVersion)
                    ? splitedVersion
                    : throw new IndexOutOfRangeException(value + " is not in allowed version");
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
                _existVersions = _existVersion.Select(version => version.Split('.').ToList()).ToList();
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
        public SemVer Merge(SemVer newVersion, bool onlyStable = true)
        {
            var ret = new SemVer();
            if (!ExistVersion.SequenceEqual(newVersion.ExistVersion))
            {
                throw new ArgumentException("These version can't marge. Because exist versions are different.");
            }

            ret._selectedVersion = _selectedVersion;
            ret._existVersions = _existVersions;
            ret._existVersion = _existVersion;

            if (_minimumVersion == null)
            {
                ret._minimumVersion = newVersion._minimumVersion;
            }
            else if (newVersion._minimumVersion == null)
            {
                ret._minimumVersion = _minimumVersion;
            }
            else
            {
                var minDiff = CompareVersion(_minimumVersion, newVersion._minimumVersion);
                ret._minimumVersion = minDiff > 0 ? _minimumVersion : newVersion._minimumVersion;
                ret._excludeMinimum = minDiff > 0 ? _excludeMinimum :
                    minDiff == 0 ? _excludeMinimum || newVersion._excludeMinimum : newVersion._excludeMinimum;
            }

            if (_maximumVersion == null)
            {
                ret._maximumVersion = newVersion._maximumVersion;
            }
            else if (newVersion._maximumVersion == null)
            {
                ret._maximumVersion = _maximumVersion;
            }
            else
            {
                var maxDiff = CompareVersion(_maximumVersion, newVersion._maximumVersion);
                ret._maximumVersion = maxDiff < 0 ? _maximumVersion : newVersion._maximumVersion;
                ret._excludeMaximum = maxDiff < 0 ? _excludeMaximum :
                    maxDiff == 0 ? _excludeMaximum || newVersion._excludeMaximum : newVersion._excludeMaximum;
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
            if (ret._maximumVersion == null && ret._excludeMinimum == false)
            {
                allowVer = ret._minimumVersion == null
                    ? ""
                    : ret._minimumVersion.Aggregate((now, next) => now + "." + next);
            }
            else if (ret._minimumVersion != null
                     && ret._maximumVersion != null
                     && ret._minimumVersion.Aggregate((now, next) => now + "." + next)
                     == ret._maximumVersion.Aggregate((now, next) => now + "." + next)
                     && ret._excludeMinimum == false
                     && ret._excludeMaximum == false)
            {
                allowVer = "[" + ret._minimumVersion.Aggregate((now, next) => now + "." + next) + "]";
            }
            else
            {
                allowVer += ret._excludeMinimum ? "(" : "[";
                allowVer += ret._minimumVersion == null
                    ? ""
                    : ret._minimumVersion.Aggregate((now, next) => now + "." + next);
                allowVer += ",";
                allowVer += ret._maximumVersion == null
                    ? ""
                    : ret._maximumVersion.Aggregate((now, next) => now + "." + next);
                allowVer += ret._excludeMaximum ? ")" : "]";
            }

            ret._allowedVersion = allowVer;

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
            return method switch
            {
                VersionSelectMethod.Suit => GetSuitVersion(onlyStable),
                VersionSelectMethod.Highest => GetHighestVersion(onlyStable),
                VersionSelectMethod.Lowest => GetLowestVersion(onlyStable),
                _ => throw new ArgumentException("method : " + method + " is invalid")
            };
        }

        private string GetHighestVersion(bool onlyStable)
        {
            List<string> maxVersion = _maximumVersion ?? _existVersions.First();
            foreach (List<string> version in _existVersions.SkipWhile(
                                                               ver => ver.Aggregate((now, next) => now + next)
                                                                      != maxVersion.Aggregate((now, next) => now + next)
                                                           )
                                                           .ToArray())
            {
                if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                {
                    continue;
                }

                var maxDiff = _maximumVersion != null ? CompareVersion(version, _maximumVersion) : -1;
                if (maxDiff == 0 && !_excludeMaximum)
                {
                    var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 1;
                    return minDiff == 0 && !_excludeMinimum
                        ? version.Aggregate((now, next) => now + "." + next)
                        : minDiff > 0
                            ? version.Aggregate((now, next) => now + "." + next)
                            : throw new InvalidOperationException("There is no available version.");
                }

                if (maxDiff < 0)
                {
                    var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 1;
                    return minDiff == 0 && !_excludeMinimum
                        ? version.Aggregate((now, next) => now + "." + next)
                        : minDiff > 0
                            ? version.Aggregate((now, next) => now + "." + next)
                            : throw new InvalidOperationException("There is no available version.");
                }
            }

            throw new InvalidOperationException("There is no available version.");
        }

        private string GetLowestVersion(bool onlyStable)
        {
            List<string> minVersion = _minimumVersion ?? _existVersions.Last();
            foreach (List<string> version in (_existVersions.AsEnumerable() ?? Array.Empty<List<string>>()).Reverse()
                     .SkipWhile(
                         ver => ver.Aggregate((now, next) => now + next)
                                != minVersion.Aggregate((now, next) => now + next)
                     )
                     .ToArray())
            {
                if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                {
                    continue;
                }

                var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 11;
                if (minDiff == 0 && !_excludeMinimum)
                {
                    var maxDiff = _maximumVersion != null ? CompareVersion(version, _maximumVersion) : -1;
                    return maxDiff == 0 && !_excludeMaximum
                        ? version.Aggregate((now, next) => now + "." + next)
                        : maxDiff < 0
                            ? version.Aggregate((now, next) => now + "." + next)
                            : throw new InvalidOperationException("There is no available version.");
                }

                if (minDiff > 0)
                {
                    var maxDiff = _maximumVersion != null ? CompareVersion(version, _maximumVersion) : -1;
                    return maxDiff == 0 && !_excludeMaximum
                        ? version.Aggregate((now, next) => now + "." + next)
                        : maxDiff < 0
                            ? version.Aggregate((now, next) => now + "." + next)
                            : throw new InvalidOperationException("There is no available version.");
                }
            }

            throw new InvalidOperationException("There is no available version.");
        }

        private string GetSuitVersion(bool onlyStable)
        {
            if (_maximumVersion != null)
            {
                foreach (List<string> version in _existVersions.SkipWhile(
                                                                   ver => ver.Aggregate((now, next) => now + next)
                                                                          != _maximumVersion.Aggregate(
                                                                              (now, next) => now + next
                                                                          )
                                                               )
                                                               .ToArray())
                {
                    if (onlyStable && (version.Any(v => v.Contains('-')) || version[0][0] == '0'))
                    {
                        continue;
                    }

                    var maxDiff = CompareVersion(version, _maximumVersion);
                    if (maxDiff == 0 && !_excludeMaximum)
                    {
                        var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 1;
                        return minDiff == 0 && !_excludeMinimum
                            ? version.Aggregate((now, next) => now + "." + next)
                            : minDiff > 0
                                ? version.Aggregate((now, next) => now + "." + next)
                                : throw new InvalidOperationException("There is no available version.");
                    }

                    if (maxDiff < 0)
                    {
                        var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 1;
                        return minDiff == 0 && !_excludeMinimum
                            ? version.Aggregate((now, next) => now + "." + next)
                            : minDiff > 0
                                ? version.Aggregate((now, next) => now + "." + next)
                                : throw new InvalidOperationException("There is no available version.");
                    }
                }

                throw new InvalidOperationException("There is no available version.");
            }

            var highList = new List<List<string>>();
            var majorVersion = new List<string>();

            foreach (List<string> version in _existVersions)
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
            var minDiff = _minimumVersion != null ? CompareVersion(version, _minimumVersion) : 1;
            var maxDiff = _maximumVersion != null ? CompareVersion(version, _maximumVersion) : -1;

            return minDiff >= 0
                   && maxDiff <= 0
                   && ((!_excludeMinimum || minDiff != 0) && (!_excludeMaximum || maxDiff != 0));
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

                switch (isNumberCompare)
                {
                    case true when isNumberBase && c != b: return c - b;
                    case false when isNumberBase == false:
                        {
                            var compare = string.CompareOrdinal(splitedCompare[0], splitedBase[0]);
                            if (compare != 0)
                            {
                                return compare;
                            }

                            break;
                        }
                    default: return isNumberBase ? 1 : -1;
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
            return lengthDiff < 0 ? -1 : lengthDiff > 0 ? 1 : 0;
        }
    }
}
