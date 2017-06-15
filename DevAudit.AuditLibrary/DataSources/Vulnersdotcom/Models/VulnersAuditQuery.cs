using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{ 
    public class VulnersAuditQuery
    {
        public VulnersAuditQuery(string os, string version, List<Package> packages)
        {
            this.os = os;
            this.version = version;
            package = packages.Select(p => p.Name + " " + p.Version + " " + p.Architecture).ToArray();
        }
        public string os { get; set; }
        public string[] package { get; set; }
        public string version { get; set; }
    }

}
