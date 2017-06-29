using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;
namespace DevAudit.AuditLibrary
{
    public class VulnerableCredentialStorage
    {
        public string File { get; set; }
        public IConfiguration Contents { get; set; }
        public string Location { get; set; }
        public string Value { get; set; }
        public double Entropy { get; set; }
    }
}
