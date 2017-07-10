using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class ProcessInfo
    {
        #region Constructors
        public ProcessInfo(string user, int pid, string start_time, string cmd_line)
        {
            this.User = user;
            this.Pid = pid;
            this.CommandLine = cmd_line;
            this.StartTime = start_time;
        }
        #endregion

        #region Properties
        public string User { get; set; }
        public int Pid { get; set; }
        public string CommandLine { get; set; }
        public string StartTime { get; set; }
        #endregion
    }
}
