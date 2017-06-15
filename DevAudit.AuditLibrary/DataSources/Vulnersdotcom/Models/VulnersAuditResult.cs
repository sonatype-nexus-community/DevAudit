using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{

    public class VulnersAuditResult
    {
        public string result { get; set; }
        public VulnersAuditResultData data { get; set; }
    }

    public class VulnersAuditResultData
    {
        public Dictionary<string, Dictionary<string, VulnersAuditResultPackage[]>> packages { get; set; }
        public string[] vulnerabilities { get; set; }
        public VulnersAuditResultDataReason[] reasons { get; set; }
        public VulnersAuditResultDataCvss cvss { get; set; }
        public string[] cvelist { get; set; }
        public string id { get; set; }
    }


    public class VulnersAuditResultCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }


    
    public class VulnersAuditResultPackage
    {
        public string package { get; set; }
        public string providedVersion { get; set; }
        public string bulletinVersion { get; set; }
        public string providedPackage { get; set; }
        public string bulletinPackage { get; set; }

        [JsonProperty("operator")]
        public string _operator { get; set; }

        public string bulletinID { get; set; }
    }


    public class VulnersAuditResultDataCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }

    public class VulnersAuditResultDataReason
    {
        public string package { get; set; }
        public string providedVersion { get; set; }
        public string bulletinVersion { get; set; }
        public string providedPackage { get; set; }
        public string bulletinPackage { get; set; }

        [JsonProperty("operator")]
        public string _operator { get; set; }

        public string bulletinID { get; set; }
    }


}
