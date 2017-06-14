using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{ 
    public class VulnersdotcomAuditQuery
    {
        public VulnersdotcomAuditQuery(string os, string version, List<Package> packages)
        {
            this.os = os;
            this.version = "16.10";
            package = packages.Select(p => p.Name + " " + p.Version + " " + p.Architecture/*(p.Architecture.Contains("64") ? "x86_64" : "x86_32")*/).ToArray();
        }
        public string os { get; set; }
        public string[] package { get; set; }
        public string version { get; set; }
    }

}
