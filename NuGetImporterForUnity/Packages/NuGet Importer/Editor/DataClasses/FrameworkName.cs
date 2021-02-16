using System.Collections.Generic;


namespace kumaS.NuGetImporter.Editor.DataClasses
{
    public static class FrameworkName
    {
        public static FrameworkMaxVersion maxVersion = FrameworkMaxVersion.NET471;

        public static readonly string[] net471 = { ".NETFramework4.7.1", "net471" };
        public static readonly string[] net47 = { ".NETFramework4.7", "net47" };
        public static readonly string[] net461 = { ".NETFramework4.6.1", "net461" };
        public static readonly string[] net46 = { ".NETFramework4.6", "net46" };
        public static readonly string[] net452 = { ".NETFramework4.5.2", "net452" };
        public static readonly string[] net451 = { ".NETFramework4.5.1", "net451" };
        public static readonly string[] net45 = { ".NETFramework4.5", "net45" };
        public static readonly string[] net403 = { ".NETFramework4.0.3", "net403" };
        public static readonly string[] net40 = { ".NETFramework4.0", "net40" };
        public static readonly string[] standard20 = { ".NETStandard2.0", "netstandard2.0" };

        /// <summary>
        /// <para>Available .NETFramework name</para>
        /// <para>利用可能な.NETFrameworkの名前</para>
        /// </summary>
        public static List<string> NET
        {
            get
            {
                var ret = new List<string>();
                if (maxVersion >= FrameworkMaxVersion.NET471)
                {
                    ret.AddRange(net471);
                }
                if (maxVersion >= FrameworkMaxVersion.NET47)
                {
                    ret.AddRange(net47);
                }
                if (maxVersion >= FrameworkMaxVersion.NET461)
                {
                    ret.AddRange(net461);
                }
                ret.AddRange(net46);
                ret.AddRange(net452);
                ret.AddRange(net451);
                ret.AddRange(net45);
                ret.AddRange(net403);
                ret.AddRange(net40);
                return ret;
            }
        }

        /// <summary>
        /// <para>Available .NETStandard name</para>
        /// <para>利用可能な.NETStandardの名前</para>
        /// </summary>
        public static List<string> STANDARD
        {
            get => new List<string>(standard20);

        }

        /// <summary>
        /// <para>All framework name</para>
        /// <para>全てのフレームワークの名前</para>
        /// </summary>
        public static List<string[]> ALLPLATFORM
        {
            get
            {
                var ret = new List<string[]>
                {
                    net471,
                    net47,
                    net461,
                    net46,
                    net452,
                    net451,
                    net45,
                    net403,
                    net40,
                    standard20
                };
                return ret;
            }
        }
    }

    public enum FrameworkMaxVersion
    {
        NET46,
        NET461,
        NET47,
        NET471
    }
}