using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary.Reporters.Models
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/bom/1.1")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://cyclonedx.org/schema/bom/1.1", IsNullable = false)]
    public partial class bom
    {

        private bomComponents componentsField;

        private string serialNumberField;

        private byte versionField;

        /// <remarks/>
        public bomComponents components
        {
            get
            {
                return this.componentsField;
            }
            set
            {
                this.componentsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string serialNumber
        {
            get
            {
                return this.serialNumberField;
            }
            set
            {
                this.serialNumberField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/bom/1.1")]
    public partial class bomComponents
    {

        private bomComponentsComponent componentField;

        /// <remarks/>
        public bomComponentsComponent component
        {
            get
            {
                return this.componentField;
            }
            set
            {
                this.componentField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/bom/1.1")]
    public partial class bomComponentsComponent
    {

        private string publisherField;

        private string groupField;

        private string nameField;

        private string versionField;

        private string purlField;

        private vulnerabilities vulnerabilitiesField;

        private string typeField;

        private string bomrefField;

        /// <remarks/>
        public string publisher
        {
            get
            {
                return this.publisherField;
            }
            set
            {
                this.publisherField = value;
            }
        }

        /// <remarks/>
        public string group
        {
            get
            {
                return this.groupField;
            }
            set
            {
                this.groupField = value;
            }
        }

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        public string purl
        {
            get
            {
                return this.purlField;
            }
            set
            {
                this.purlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
        public vulnerabilities vulnerabilities
        {
            get
            {
                return this.vulnerabilitiesField;
            }
            set
            {
                this.vulnerabilitiesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("bom-ref")]
        public string bomref
        {
            get
            {
                return this.bomrefField;
            }
            set
            {
                this.bomrefField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0", IsNullable = false)]
    public partial class vulnerabilities
    {

        private vulnerabilitiesVulnerability vulnerabilityField;

        /// <remarks/>
        public vulnerabilitiesVulnerability vulnerability
        {
            get
            {
                return this.vulnerabilityField;
            }
            set
            {
                this.vulnerabilityField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerability
    {

        private string idField;

        private vulnerabilitiesVulnerabilitySource sourceField;

        private vulnerabilitiesVulnerabilityRatings ratingsField;

        private ushort[] cwesField;

        private string descriptionField;

        private vulnerabilitiesVulnerabilityRecommendations recommendationsField;

        private string[] advisoriesField;

        private string refField;

        /// <remarks/>
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        public vulnerabilitiesVulnerabilitySource source
        {
            get
            {
                return this.sourceField;
            }
            set
            {
                this.sourceField = value;
            }
        }

        /// <remarks/>
        public vulnerabilitiesVulnerabilityRatings ratings
        {
            get
            {
                return this.ratingsField;
            }
            set
            {
                this.ratingsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("cwe", IsNullable = false)]
        public ushort[] cwes
        {
            get
            {
                return this.cwesField;
            }
            set
            {
                this.cwesField = value;
            }
        }

        /// <remarks/>
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        public vulnerabilitiesVulnerabilityRecommendations recommendations
        {
            get
            {
                return this.recommendationsField;
            }
            set
            {
                this.recommendationsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("advisory", IsNullable = false)]
        public string[] advisories
        {
            get
            {
                return this.advisoriesField;
            }
            set
            {
                this.advisoriesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string @ref
        {
            get
            {
                return this.refField;
            }
            set
            {
                this.refField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerabilitySource
    {

        private string urlField;

        private string nameField;

        /// <remarks/>
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerabilityRatings
    {

        private vulnerabilitiesVulnerabilityRatingsRating ratingField;

        /// <remarks/>
        public vulnerabilitiesVulnerabilityRatingsRating rating
        {
            get
            {
                return this.ratingField;
            }
            set
            {
                this.ratingField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerabilityRatingsRating
    {

        private vulnerabilitiesVulnerabilityRatingsRatingScore scoreField;

        private string severityField;

        private string methodField;

        private string vectorField;

        /// <remarks/>
        public vulnerabilitiesVulnerabilityRatingsRatingScore score
        {
            get
            {
                return this.scoreField;
            }
            set
            {
                this.scoreField = value;
            }
        }

        /// <remarks/>
        public string severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }

        /// <remarks/>
        public string method
        {
            get
            {
                return this.methodField;
            }
            set
            {
                this.methodField = value;
            }
        }

        /// <remarks/>
        public string vector
        {
            get
            {
                return this.vectorField;
            }
            set
            {
                this.vectorField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerabilityRatingsRatingScore
    {

        private decimal baseField;

        private decimal impactField;

        private decimal exploitabilityField;

        /// <remarks/>
        public decimal @base
        {
            get
            {
                return this.baseField;
            }
            set
            {
                this.baseField = value;
            }
        }

        /// <remarks/>
        public decimal impact
        {
            get
            {
                return this.impactField;
            }
            set
            {
                this.impactField = value;
            }
        }

        /// <remarks/>
        public decimal exploitability
        {
            get
            {
                return this.exploitabilityField;
            }
            set
            {
                this.exploitabilityField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://cyclonedx.org/schema/ext/vulnerability/1.0")]
    public partial class vulnerabilitiesVulnerabilityRecommendations
    {

        private string recommendationField;

        /// <remarks/>
        public string recommendation
        {
            get
            {
                return this.recommendationField;
            }
            set
            {
                this.recommendationField = value;
            }
        }
    }



}
