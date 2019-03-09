using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IOperatingSystemEnvironment
    {
        string GetOSName();
        string GetOSVersion();
    }
}
