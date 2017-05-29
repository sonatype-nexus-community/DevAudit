using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IShellCommandAuditTarget
    {
        Stream GetInputStream();
        Stream GetOuputStream();
    }
}
