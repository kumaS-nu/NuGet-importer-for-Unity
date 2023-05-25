using System.Collections.Generic;

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
            if (directoryName.StartsWith(nameof(OSType.Win)))
            {
                return (nameof(OSType.Win), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.OSX)))
            {
                return (nameof(OSType.OSX), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.Android)))
            {
                return (nameof(OSType.Android), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.IOS)))
            {
                return (nameof(OSType.IOS), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.Linux)))
            {
                return (nameof(OSType.Linux), 0);
            }

            if (directoryName.StartsWith(nameof(OSType.Ubuntu)))
            {
                return (nameof(OSType.Ubuntu), 1);
            }

            if (directoryName.StartsWith(nameof(OSType.Debian)))
            {
                return (nameof(OSType.Debian), 2);
            }

            if (directoryName.StartsWith(nameof(OSType.Fedora)))
            {
                return (nameof(OSType.Fedora), 3);
            }

            if (directoryName.StartsWith(nameof(OSType.Centos)))
            {
                return (nameof(OSType.Centos), 4);
            }

            if (directoryName.StartsWith(nameof(OSType.Alpine)))
            {
                return (nameof(OSType.Alpine), 5);
            }

            return directoryName.StartsWith(nameof(OSType.Rhel))
                ? ((string os, int priority))(nameof(OSType.Rhel), 6)
                : directoryName.StartsWith(nameof(OSType.Arch))
                    ? ((string os, int priority))(nameof(OSType.Arch), 7)
                    : directoryName.StartsWith(nameof(OSType.Opensuse))
                        ? ((string os, int priority))(nameof(OSType.Opensuse), 8)
                        : directoryName.StartsWith(nameof(OSType.Gentoo))
                            ? ((string os, int priority))(nameof(OSType.Gentoo), 9)
                            : ((string os, int priority))(directoryName.Split('-')[0], int.MaxValue);
        }

        private string GetArchInfo(string directoryName)
        {
            if (directoryName.EndsWith(nameof(ArchitectureType.X64)))
            {
                return nameof(ArchitectureType.X64);
            }

            return directoryName.EndsWith(nameof(ArchitectureType.X86))
                ? nameof(ArchitectureType.X86)
                : directoryName.EndsWith(nameof(ArchitectureType.ARM64))
                    ? nameof(ArchitectureType.ARM64)
                    : directoryName.EndsWith(nameof(ArchitectureType.ARM))
                        ? nameof(ArchitectureType.ARM)
                        : DefaultArch.TryGetValue(OS, out var arch)
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
