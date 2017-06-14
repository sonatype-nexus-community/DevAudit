using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary.DataSources
{ 
    public class LibdotIOArtifact : IArtifact
    {
        [JsonProperty("name")]
        public string PackageName { get; set; }

        [JsonProperty("platform")]
        public string PackageManager { get; set; }

        [JsonProperty("platform")]
        public string Description { get; set; }

        public string homepage { get; set; }
        public string repository_url { get; set; }
        public string[] normalized_licenses { get; set; }
        public int rank { get; set; }
        public DateTime latest_release_published_at { get; set; }
        public string latest_release_number { get; set; }
        public string language { get; set; }
        public object status { get; set; }

        [JsonProperty("package_manager_url")]
        public string PackageUrl{ get; set; }

        public int stars { get; set; }
        public int forks { get; set; }
        public string[] keywords { get; set; }

        [JsonProperty("Latest_Stable_Release")]
        public LibdotIO_Latest_Stable_Release latest_stable_release { get; set; }

        [JsonProperty("versions")]
        public Version[] _Versions { get; set; }

        [JsonIgnore]
        public ArtifactVersion[] Versions { get; set; }

        [JsonIgnore]
        public ArtifactVersion LatestStableVersion { get; set; }

        [JsonIgnore]
        public string ArtifactId { get; set; }

        [JsonIgnore]
        public string PackageId { get; set; }

        [JsonIgnore]
        public string Version { get; set; }
    }

    public class LibdotIO_Latest_Stable_Release
    {
        public string number { get; set; }
        public DateTime published_at { get; set; }
    }

    public class LibdotIO_Version
    {
        public string number { get; set; }
        public DateTime published_at { get; set; }
    }

}
