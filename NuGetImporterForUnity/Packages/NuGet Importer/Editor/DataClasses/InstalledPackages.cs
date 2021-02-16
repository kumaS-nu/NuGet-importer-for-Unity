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

        private Package[] packageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("package")]
        public Package[] package
        {
            get => packageField;
            set => packageField = value;
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class Package
    {

        private string idField;

        [System.Xml.Serialization.XmlIgnore]
        public SemVer versionField = new SemVer();

        private string targetFrameworkField;



        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get => idField;
            set => idField = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get => versionField.SelectedVersion;
            set => versionField.SelectedVersion = value;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string targetFramework
        {
            get => targetFrameworkField;
            set => targetFrameworkField = value;
        }


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string allowedVersions
        {
            get => versionField.AllowedVersion;
            set => versionField.AllowedVersion = value;
        }
    }
}
