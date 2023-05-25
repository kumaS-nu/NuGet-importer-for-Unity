using System.Collections.Generic;

namespace kumaS.NuGetImporter.Editor.DataClasses
{
    // メモ: 生成されたコードは、少なくとも .NET Framework 4.5または .NET Core/Standard 2.0 が必要な可能性があります。
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute("packages", Namespace = "", IsNullable = false)]
    public partial class InstalledPackages
    {
        private List<Package> _packageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("package")]
        public List<Package> Package
        {
            get => _packageField;
            set => _packageField = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class Package
    {
        private string _idField;

        [System.Xml.Serialization.XmlIgnore] public SemVer VersionField = new SemVer();

        private string _targetFrameworkField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get => _idField;
            set => _idField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Version
        {
            get => VersionField.SelectedVersion;
            set => VersionField.SelectedVersion = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TargetFramework
        {
            get => _targetFrameworkField;
            set => _targetFrameworkField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AllowedVersions
        {
            get => VersionField.AllowedVersion;
            set => VersionField.AllowedVersion = value;
        }
    }
}
