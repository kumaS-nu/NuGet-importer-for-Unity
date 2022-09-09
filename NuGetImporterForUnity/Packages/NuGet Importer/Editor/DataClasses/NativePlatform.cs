using System.Collections.Generic;
using System.IO;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    /// <summary>
    /// <para>Identify native plug-in platforms.</para>
    /// <para>ネイティブプラグインのプラットフォームを特定。</para>
    /// </summary>
    public class NativePlatform
    {
        private static readonly Dictionary<string, string> defaultArch = new Dictionary<string, string>
        {
            { nameof(OSType.win), nameof(ArchitectureType.x64) },
            { nameof(OSType.osx), nameof(ArchitectureType.x64) },
            { nameof(OSType.android), nameof(ArchitectureType.arm64) },
            { nameof(OSType.ios), nameof(ArchitectureType.arm64) },
            { nameof(OSType.linux), nameof(ArchitectureType.x64) },
            { nameof(OSType.ubuntu), nameof(ArchitectureType.x64) },
            { nameof(OSType.debian), nameof(ArchitectureType.x64) },
            { nameof(OSType.fedora), nameof(ArchitectureType.x64) },
            { nameof(OSType.centos), nameof(ArchitectureType.x64) },
            { nameof(OSType.alpine), nameof(ArchitectureType.x64) },
            { nameof(OSType.rhel), nameof(ArchitectureType.x64) },
            { nameof(OSType.arch), nameof(ArchitectureType.x64) },
            { nameof(OSType.opensuse), nameof(ArchitectureType.x64) },
            { nameof(OSType.gentoo), nameof(ArchitectureType.x64) }
        };

        public readonly string path;
        public readonly string os;
        public readonly int osPriority;
        public readonly string architecture;

        public NativePlatform(string directoryPath)
        {
            path = directoryPath;
            var directoryName = Path.GetFileName(directoryPath).ToLowerInvariant();
            (os, osPriority) = GetOSInfo(directoryName);
            architecture = GetArchInfo(directoryName);
        }

        private (string os, int priority) GetOSInfo(string directoryName)
        {
            if (directoryName.StartsWith(nameof(OSType.win)))
            {
                return (nameof(OSType.win), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.osx)))
            {
                return (nameof(OSType.osx), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.android)))
            {
                return (nameof(OSType.android), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.ios)))
            {
                return (nameof(OSType.ios), -1);
            }

            if (directoryName.StartsWith(nameof(OSType.linux)))
            {
                return (nameof(OSType.linux), 0);
            }

            if (directoryName.StartsWith(nameof(OSType.ubuntu)))
            {
                return (nameof(OSType.ubuntu), 1);
            }

            if (directoryName.StartsWith(nameof(OSType.debian)))
            {
                return (nameof(OSType.debian), 2);
            }

            if (directoryName.StartsWith(nameof(OSType.fedora)))
            {
                return (nameof(OSType.fedora), 3);
            }

            if (directoryName.StartsWith(nameof(OSType.centos)))
            {
                return (nameof(OSType.centos), 4);
            }

            if (directoryName.StartsWith(nameof(OSType.alpine)))
            {
                return (nameof(OSType.alpine), 5);
            }

            return directoryName.StartsWith(nameof(OSType.rhel))
                ? ((string os, int priority))(nameof(OSType.rhel), 6)
                : directoryName.StartsWith(nameof(OSType.arch))
                ? ((string os, int priority))(nameof(OSType.arch), 7)
                : directoryName.StartsWith(nameof(OSType.opensuse))
                ? ((string os, int priority))(nameof(OSType.opensuse), 8)
                : directoryName.StartsWith(nameof(OSType.gentoo)) ? ((string os, int priority))(nameof(OSType.gentoo), 9) : ((string os, int priority))(directoryName.Split('-')[0], int.MaxValue);
        }

        private string GetArchInfo(string directoryName)
        {
            if (directoryName.EndsWith(nameof(ArchitectureType.x64)))
            {
                return nameof(ArchitectureType.x64);
            }

            return directoryName.EndsWith(nameof(ArchitectureType.x86))
                ? nameof(ArchitectureType.x86)
                : directoryName.EndsWith(nameof(ArchitectureType.arm64))
                ? nameof(ArchitectureType.arm64)
                : directoryName.EndsWith(nameof(ArchitectureType.arm))
                ? nameof(ArchitectureType.arm)
                : defaultArch.TryGetValue(os, out var arch) ? arch : nameof(ArchitectureType.x64);
        }
    }

    public enum OSType
    {
        win,
        osx,
        android,
        ios,
        linux,
        ubuntu,
        debian,
        fedora,
        centos,
        alpine,
        rhel,
        arch,
        opensuse,
        gentoo
    }

    public enum ArchitectureType
    {
        x64,
        x86,
        arm64,
        arm
    }
}
