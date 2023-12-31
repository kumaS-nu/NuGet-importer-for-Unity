using System;
using System.Collections.Generic;
using System.Linq;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Identify native plug-in platforms.</para>
    /// <para>ネイティブプラグインのプラットフォームを特定。</para>
    /// </summary>
    public class NativePlatform
    {
        private static readonly Dictionary<string, string> DefaultArch = new Dictionary<string, string>()
        {
            { nameof(OSType.Win), nameof(ArchitectureType.X64) },
            { nameof(OSType.OSX), nameof(ArchitectureType.X64) },
            { nameof(OSType.Android), nameof(ArchitectureType.ARM64) },
            { nameof(OSType.IOS), nameof(ArchitectureType.ARM64) },
            { nameof(OSType.Linux), nameof(ArchitectureType.X64) },
            { nameof(OSType.Ubuntu), nameof(ArchitectureType.X64) },
            { nameof(OSType.Debian), nameof(ArchitectureType.X64) },
            { nameof(OSType.Fedora), nameof(ArchitectureType.X64) },
            { nameof(OSType.Centos), nameof(ArchitectureType.X64) },
            { nameof(OSType.Alpine), nameof(ArchitectureType.X64) },
            { nameof(OSType.Rhel), nameof(ArchitectureType.X64) },
            { nameof(OSType.Arch), nameof(ArchitectureType.X64) },
            { nameof(OSType.Opensuse), nameof(ArchitectureType.X64) },
            { nameof(OSType.Gentoo), nameof(ArchitectureType.X64) }
        };

        private static readonly List<(string, int)> PriorityTable = new List<(string, int)>()
        {
            (nameof(OSType.Win), -1),
            (nameof(OSType.OSX), -1),
            (nameof(OSType.Android), -1),
            (nameof(OSType.IOS), -1),
            (nameof(OSType.Linux), -1),
            (nameof(OSType.Ubuntu), 1),
            (nameof(OSType.Debian), 2),
            (nameof(OSType.Fedora), 3),
            (nameof(OSType.Centos), 4),
            (nameof(OSType.Alpine), 5),
            (nameof(OSType.Rhel), 6),
            (nameof(OSType.Arch), 7),
            (nameof(OSType.Opensuse), 8),
            (nameof(OSType.Gentoo), 9)
        };

        public readonly string Path;
        public readonly string OS;
        public readonly int OSPriority;
        public readonly string Architecture;

        public NativePlatform(string directoryPath)
        {
            Path = directoryPath;
            // https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
            var rid = GetRidFrom(directoryPath);
            (OS, OSPriority) = GetOSInfo(rid);
            Architecture = GetArchInfo(rid);
        }

        private static string GetRidFrom(string path)
        { // https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks#architecture-specific-folders
            // assumes right path is like "Packages/project/runtimes/RID" or +"/native/..."
            path = path.Replace('\\', '/'); // in case of '\'(windows)
            var pattern = "/?runtimes/(.+?)/?";
            var matched = System.Text.RegularExpressions.Regex.Match(
                path,
                pattern,
                System.Text.RegularExpressions.RegexOptions.RightToLeft | // to choose last
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!matched.Success)
                return string.Empty; // failed to find
            return matched.Groups[1].Value.Split('/')[0];
        }

        private (string os, int priority) GetOSInfo(string rid)
        {
            var matchedPriority = PriorityTable.Where(table => rid.StartsWith(table.Item1.ToLowerInvariant()));
            if (matchedPriority.Any())
            {
                return matchedPriority.First();
            }

            return (rid.Split('-')[0], int.MaxValue);
        }

        private string GetArchInfo(string rid)
        {
            var matchedArch = Enum.GetNames(typeof(ArchitectureType)).Where(table => rid.EndsWith(table.ToLowerInvariant()));
            if (matchedArch.Any())
            {
                return matchedArch.First();
            }

            return DefaultArch.TryGetValue(OS, out var arch)
                            ? arch
                            : nameof(ArchitectureType.X64);
        }
    }

    public enum OSType
    {
        Win,
        OSX,
        Android,
        IOS,
        Linux,
        Ubuntu,
        Debian,
        Fedora,
        Centos,
        Alpine,
        Rhel,
        Arch,
        Opensuse,
        Gentoo
    }

    public enum ArchitectureType
    {
        X64,
        X86,
        ARM64,
        ARM
    }
}
