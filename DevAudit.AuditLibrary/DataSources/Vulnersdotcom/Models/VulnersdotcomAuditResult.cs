using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{

    public class VulnersdotcomAuditResult
    {
        public string result { get; set; }
        public VulnersdotcomAuditResultData data { get; set; }
    }

    public class VulnersdotcomAuditResultData
    {
        public Dictionary<string, Dictionary<string, VulnersdotcomAuditResultPackage[]>> packages { get; set; }
        public string[] vulnerabilities { get; set; }
        public VulnersdotcomAuditResultDataReason[] reasons { get; set; }
        public VulnersdotcomAuditResultDataCvss cvss { get; set; }
        public string[] cvelist { get; set; }
        public string id { get; set; }
    }


    public class VulnersdotcomAuditResultCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }


    
    public class VulnersdotcomAuditResultPackage
    {
        public string package { get; set; }
        public string providedVersion { get; set; }
        public string bulletinVersion { get; set; }
        public string providedPackage { get; set; }
        public string bulletinPackage { get; set; }
        public string _operator { get; set; }
        public string bulletinID { get; set; }
    }


    public class VulnersdotcomAuditResultDataCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }

    public class VulnersdotcomAuditResultDataReason
    {
        public string package { get; set; }
        public string providedVersion { get; set; }
        public string bulletinVersion { get; set; }
        public string providedPackage { get; set; }
        public string bulletinPackage { get; set; }
        public string _operator { get; set; }
        public string bulletinID { get; set; }
    }


}
