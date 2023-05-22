using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    [Serializable]
    public class NuGetImporterSettings
    {
        [NonSerialized]
        private static NuGetImporterSettings instance;

        [NonSerialized]
        private static string projectSettingsPath;

        [InitializeOnLoadMethod]
        private static void SetProjectSettingsPath()
        {
            projectSettingsPath = Application.dataPath.Replace("Assets", "ProjectSettings");
        }

        internal static void EnsureSetProjectSettingsPath()
        {
            if (projectSettingsPath == null)
            {
                SetProjectSettingsPath();
            }
        }

        public static NuGetImporterSettings Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var path = Path.Combine(projectSettingsPath, "NuGetImporterSettings.json");
                if (!File.Exists(path))
                {
                    instance = new NuGetImporterSettings();
                    return instance;
                }
                var str = File.ReadAllText(path);
                instance = JsonUtility.FromJson<NuGetImporterSettings>(str);
                if (!str.Contains("\"ignorePackages\""))
                {
                    instance.ignorePackages = default;
                }
                return instance;
            }
        }

        private void Save()
        {
            var path = Path.Combine(projectSettingsPath, "NuGetImporterSettings.json");
            File.WriteAllText(path, JsonUtility.ToJson(this, true));
        }

        [SerializeField]
        private int searchCacheLimit = 500;

        /// <value>
        /// <para>Limit of search cache.</para>
        /// <para>検索のキャッシュの最大数。</para>
        /// </value>
        public int SearchCacheLimit
        {
            get => searchCacheLimit;
            set
            {
                var changed = searchCacheLimit != value;
                searchCacheLimit = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private int catalogCacheLimit = 300;

        /// <value>
        /// <para>Limit of catalog cache.</para>
        /// <para>カタログのキャッシュの最大数。</para>
        /// </value>
        public int CatalogCacheLimit
        {
            get => catalogCacheLimit;
            set
            {
                var changed = catalogCacheLimit != value;
                catalogCacheLimit = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private int iconCacheLimit = 500;

        /// <value>
        /// <para>Limit of icon cache.</para>
        /// <para>アイコンのキャッシュの最大数。</para>
        /// </value>
        public int IconCacheLimit
        {
            get => iconCacheLimit;
            set
            {
                var changed = iconCacheLimit != value;
                iconCacheLimit = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private VersionSelectMethod method = VersionSelectMethod.Suit;

        /// <value>
        /// <para>Method to select a version.</para>
        /// <para>バージョンを選択する方法。</para>
        /// </value>
        public VersionSelectMethod Method
        {
            get => method;
            set
            {
                var changed = method != value;
                method = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private bool onlyStable = true;

        /// <value>
        /// <para>Use the stable version only?</para>
        /// <para>安定版のみ使用するか。</para>
        /// </value>
        public bool OnlyStable
        {
            get => onlyStable;
            set
            {
                var changed = onlyStable != value;
                onlyStable = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private InstallMethod installMethod = InstallMethod.AsUPM;

        /// <value>
        /// <para>Install method of NuGet package.</para>
        /// <para>NuGetパッケージのインストール方法。</para>
        /// </value>
        public InstallMethod InstallMethod
        {
            get => installMethod;
            set
            {
                var changed = installMethod != value;
                installMethod = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private bool autoPackagePlacementCheck = true;

        /// <summary>
        /// <para>Is package placement checked at startup?</para>
        /// <para>起動時にパッケージの配置をチェックするか。</para>
        /// </summary>
        public bool AutoPackagePlacementCheck
        {
            get => autoPackagePlacementCheck;
            set
            {
                var changed = autoPackagePlacementCheck != value;
                autoPackagePlacementCheck = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private int retryLimit = 1;

        /// <summary>
        /// <para>How many retries are allowed over a network connection.</para>
        /// <para>ネットワーク接続で何回までリトライするか。</para>
        /// </summary>
        public int RetryLimit
        {
            get => retryLimit;
            set
            {
                if (value < 0)
                {
                    return;
                }
                var changed = retryLimit != value;
                retryLimit = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private int timeout = 100;

        /// <summary>
        /// <para>Network timeout time.</para>
        /// <para>ネットワークのタイムアウト時間。</para>
        /// </summary>
        public int Timeout
        {
            get => timeout;
            set
            {
                if (value < 100)
                {
                    return;
                }
                var changed = timeout != value;
                timeout = value;
                _ = NuGet.SetTimeout(TimeSpan.FromSeconds(value));
                _ = PackageDataExtentionToGUI.SetTimeout(TimeSpan.FromSeconds(value));
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private bool isNetworkSavemode = false;

        /// <summary>
        /// <para>A mode that reduces the network connections.</para>
        /// <para>ネットワーク接続を少なくするモード。</para>
        /// </summary>
        public bool IsNetworkSavemode
        {
            get => isNetworkSavemode;
            set
            {
                var changed = isNetworkSavemode != value;
                isNetworkSavemode = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private List<string> ignorePackages = default;

        /// <summary>
        /// <para>Ignore pacakges.</para>
        /// <para>無視するパッケージ。</para>
        /// </summary>
        public List<string> IgnorePackages
        {
            get
            {
                if (ignorePackages == default)
                {
                    ignorePackages = DefaultIgnorePackages.Names.ToList();
                    Save();
                }
                return new List<string>(ignorePackages);
            }
            set
            {
                var changed = !ignorePackages.SequenceEqual(value);
                ignorePackages = value;
                if (changed)
                {
                    Save();
                }
            }
        }

        [SerializeField]
        private bool isCreateAsmdefForAnalyzer = true;

        public bool IsCreateAsmdefForAnalyzer
        {
            get => isCreateAsmdefForAnalyzer;
            set
            {
                var changed = isCreateAsmdefForAnalyzer != value;
                isCreateAsmdefForAnalyzer = value;
                if (changed)
                {
                    Save();
                }
            }
        }
    }
}
