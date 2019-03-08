using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IContainerEnvironment
    {
        Tuple<bool, bool> GetContainerStatus(string container_id);
        bool ExecuteCommandInContainer(string command, string arguments, out string process_output);
    }
}
