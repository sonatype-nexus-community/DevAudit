using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IPackage
    {
        
        string PackageManager { get; set; }

        string Name { get; set; }

        string Version { get; set; }

        string Group { get; set; }

        string Vendor { get; set; }
    }
}
