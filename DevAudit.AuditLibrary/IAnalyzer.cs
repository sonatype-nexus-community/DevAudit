using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IAnalyzer
    {
        string Name { get; }
        ScriptEnvironment ScriptEnvironment { get; set; }
        object Workspace { get; set; }
        object Project { get; set; }
        object Compilation { get; set; }

    }
}
