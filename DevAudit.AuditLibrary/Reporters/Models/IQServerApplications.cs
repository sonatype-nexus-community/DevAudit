namespace DevAudit.AuditLibrary.Reporters.Models
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class IQServerApplications
    {
        [JsonProperty("applications")]
        public Application[] Applications { get; set; }
    }

    public partial class Application
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("publicId")]
        public string PublicId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("organizationId")]
        public string OrganizationId { get; set; }

        [JsonProperty("contactUserName")]
        public object ContactUserName { get; set; }

        [JsonProperty("applicationTags")]
        public object[] ApplicationTags { get; set; }
    }
}