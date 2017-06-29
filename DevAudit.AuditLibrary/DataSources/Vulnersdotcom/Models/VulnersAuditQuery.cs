using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{ 
    public class VulnersAuditQuery
    {
        public VulnersAuditQuery(string os, string version, string[] p)
        {
            this.os = os;
            switch (this.os)
            {
                
                case "ubuntu":
                    this.version = version;
                    package = p;
                    break;
                case "debian":
                    this.version = version.Split('.').First();
                    package = p;
                    break;
                case "centos":
                case "oraclelinux":
                case "rhel":
                    this.version = version.Split('.').First();
                    package = p;
                    break;
                default:
                    throw new NotSupportedException("Unknown OS: " + this.os);
            }
 
        }
        public string os { get; set; }
        public string[] package { get; set; }
        public string version { get; set; }
    }

}
