using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class VulnersSearchQuery
    {
        public VulnersSearchQuery(string[] id, bool references)
        {
            this.id = id;
            this.references = references;
        }
        public string[] id { get; set; }
        public bool references { get; set; }
    }

}
