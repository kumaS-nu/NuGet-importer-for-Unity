using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    public static class FrameworkName
    {
        public static readonly string[] net48 = { ".NETFramework4.8", "net48" };
        public static readonly string[] net472 = { ".NETFramework4.7.2", "net472" };
        public static readonly string[] net471 = { ".NETFramework4.7.1", "net471" };
        public static readonly string[] net47 = { ".NETFramework4.7", "net47" };
        public static readonly string[] net462 = { ".NETFramework4.6.2", "net462" };
        public static readonly string[] net461 = { ".NETFramework4.6.1", "net461" };
        public static readonly string[] net46 = { ".NETFramework4.6", "net46" };
        public static readonly string[] net452 = { ".NETFramework4.5.2", "net452" };
        public static readonly string[] net451 = { ".NETFramework4.5.1", "net451" };
        public static readonly string[] net45 = { ".NETFramework4.5", "net45" };
        public static readonly string[] net403 = { ".NETFramework4.0.3", "net403" };
        public static readonly string[] net40 = { ".NETFramework4.0", "net40" };
        public static readonly string[] standard21 = { ".NETStandard2.1", "netstandard2.1" };
        public static readonly string[] standard20 = { ".NETStandard2.0", "netstandard2.0" };

        /// <summary>
        /// <para>Available .NETFramework4.6 or lower names</para>
        /// <para>利用可能な.NETFramework4.6以下のの名前</para>
        /// </summary>
        public static List<string> NET4_6
        {
            get
            {
                var ret = new List<string>();
                ret.AddRange(net462);
                ret.AddRange(net461);
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
        /// <para>Available .NETFramework4.8 or lower names</para>
        /// <para>利用可能な.NETFramework4.8以下のの名前</para>
        /// </summary>
        public static List<string> NET4_8
        {
            get
            {
                var ret = new List<string>();
                ret.AddRange(net48);
                ret.AddRange(net472);
                ret.AddRange(net471);
                ret.AddRange(net47);
                ret.AddRange(NET4_6);
                return ret;
            }
        }

        /// <summary>
        /// <para>Available .NETStandard name</para>
        /// <para>利用可能な.NETStandardの名前</para>
        /// </summary>
        public static List<string> STANDARD2_0
        {
            get => new List<string>(standard20);
        }

        /// <summary>
        /// <para>Available .NETStandard2.1 or lower names</para>
        /// <para>利用可能な.NETStandard2.1以下の名前</para>
        /// </summary>
        public static List<string> STANDARD2_1
        {
            get
            {
                var ret = new List<string>();
                ret.AddRange(standard21);
                ret.AddRange(standard20);
                return ret;
            }
        }

        /// <summary>
        /// <para>List of framework names to target</para>
        /// <para>対象とするフレームワーク名のリスト</para>
        /// </summary>
        /// <exception cref="System.NotSupportedException">
        /// <para>Thrown when the environment is not .Net4.x equivalent.</para>
        /// <para>.Net4.x equivalent以外の環境のときスローされる。</para>
        /// </exception>
        public static List<string> TARGET
        {
            get
            {
                var ret = new List<string>();
                switch (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
                {
                    case ApiCompatibilityLevel.NET_4_6:
                        ret = NET4_6;
                        ret.AddRange(STANDARD2_0);
                        break;
                    case ApiCompatibilityLevel.NET_Standard_2_0:
                        ret = STANDARD2_0;
                        ret.AddRange(NET4_6);
                        break;
#if UNITY_2021_2_1_OR_NEWER
                    case ApiCompatibilityLevel.NET_Unity_4_8:
                        ret = NET4_8;
                        ret.AddRange(STANDARD2_1);
                        break;
                    case ApiCompatibilityLevel.NET_Standard:
                        ret = STANDARD2_1;
                        ret.AddRange(NET4_8);
                        break;
#endif
                    default:
                        throw new NotSupportedException("Now this is only suppoort .Net4.x equivalent");
                }
                return ret;
            }
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
                    net48,
                    net472,
                    net471,
                    net47,
                    net462,
                    net461,
                    net46,
                    net452,
                    net451,
                    net45,
                    net403,
                    net40,
                    standard21,
                    standard20
                };
                return ret;
            }
        }

        /// <summary>
        /// <para>All framework name</para>
        /// <para>全てのフレームワークの名前</para>
        /// </summary>
        public static List<string> ALLFLATPLATFORM
        {
            get => ALLPLATFORM.SelectMany(p => p).ToList();
        }
    }
}