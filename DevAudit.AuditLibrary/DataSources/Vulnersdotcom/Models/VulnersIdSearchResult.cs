using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DevAudit.AuditLibrary
{

    public class VulnersIdSearchResult
    {
        public string result { get; set; }
        public VulnersIdSearchResultData data { get; set; }
    }

    public class VulnersIdSearchResultData
    {
        public Dictionary<string, VulnersIdSearchResultDocument> documents { get; set; }
    }

 

    public class VulnersIdSearchResultDocument
    {
        public string id { get; set; }
        public string bulletinFamily { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTime published { get; set; }
        public DateTime modified { get; set; }
        public VulnersIdSearchResultCvss cvss { get; set; }
        public string href { get; set; }
        public string reporter { get; set; }
        public string[] references { get; set; }
        public string[] cvelist { get; set; }
        public string type { get; set; }
        public DateTime lastseen { get; set; }
        public object[] history { get; set; }
        public int edition { get; set; }
        public VulnersIdSearchResultHashmap[] hashmap { get; set; }
        public string hash { get; set; }
        public int viewCount { get; set; }
        public string objectVersion { get; set; }
        public VulnersIdSearchResultAffectedpackage[] affectedPackage { get; set; }
        public VulnersIdSearchResultEnchantments enchantments { get; set; }
    }

    public class VulnersIdSearchResultCvss
    {
        public float score { get; set; }
        public string vector { get; set; }
    }

    public class VulnersIdSearchResultEnchantments
    {
        public float vulnersScore { get; set; }
    }

    public class VulnersIdSearchResultHashmap
    {
        public string key { get; set; }
        public string hash { get; set; }
    }

    public class VulnersIdSearchResultAffectedpackage
    {
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string arch { get; set; }

        [JsonProperty("operator")]
        public string _operator { get; set; }

        public string packageFilename { get; set; }
        public string packageName { get; set; }
        public string packageVersion { get; set; }
    }

}
