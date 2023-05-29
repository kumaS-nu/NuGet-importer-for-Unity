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
            var directoryName = System.IO.Path.GetFileName(directoryPath).ToLowerInvariant();
            (OS, OSPriority) = GetOSInfo(directoryName);
            Architecture = GetArchInfo(directoryName);
        }

        private (string os, int priority) GetOSInfo(string directoryName)
        {
            var matchedPriority = PriorityTable.Where(table => directoryName.StartsWith(table.Item1.ToLowerInvariant()));
            if (matchedPriority.Any())
            {
                return matchedPriority.First();
            }

            return (directoryName.Split('-')[0], int.MaxValue);
        }

        private string GetArchInfo(string directoryName)
        {
            var matchedArch = Enum.GetNames(typeof(ArchitectureType)).Where(table => directoryName.EndsWith(table.ToLowerInvariant()));
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
