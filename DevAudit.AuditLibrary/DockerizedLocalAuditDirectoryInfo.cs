using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus.IO;

namespace DevAudit.AuditLibrary
{
    public class DockerizedLocalAuditDirectoryInfo : LocalAuditDirectoryInfo
    {
        #region Constructors
        public DockerizedLocalAuditDirectoryInfo(DockerizedLocalEnvironment env, string path) : base(env, path) {}
        #endregion
        
    }
}
