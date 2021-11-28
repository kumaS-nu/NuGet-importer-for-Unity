using System;
using System.IO;
using System.Text;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    [Serializable]
    public class NuGetImporterSettings
    {
        [NonSerialized]
        private static NuGetImporterSettings instance;

        public static NuGetImporterSettings Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                var path = Path.Combine(Application.dataPath.Replace("Assets", "ProjectSettings"), "NuGetImporterSettings.json");
                if (!File.Exists(path))
                {
                    instance = new NuGetImporterSettings();
                    return instance;
                }
                var str = File.ReadAllText(path);
                instance = JsonUtility.FromJson<NuGetImporterSettings>(str);
                return instance;
            }
        }

        private void Save()
        {
            var path = Path.Combine(Application.dataPath.Replace("Assets", "ProjectSettings"), "NuGetImporterSettings.json");
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
            get { return searchCacheLimit; }
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
            get { return catalogCacheLimit; }
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
            get { return iconCacheLimit; }
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
            get { return method; }
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
            get { return onlyStable; }
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
            get { return installMethod; }
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
    }
}
