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
            if (this.os == "ubuntu")
            {
                this.version = version;

                package = p;
            }
            else if (this.os == "centos")
            {
                this.version = version.Split('.').First();
                package = p;
            }
        }
        public string os { get; set; }
        public string[] package { get; set; }
        public string version { get; set; }
    }

}
