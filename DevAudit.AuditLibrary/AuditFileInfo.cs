using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class AuditFileInfo : FileSystemInfo
    {
        public AuditEnvironment AuditEnvironment { get; protected set; }

        public override string FullName
        {
            get
            {
                return _Name;
            }
        }
        
        public override string Name
        {
            get
            {
                return _Name;
            }
        }

        public override void Delete()
        {
            throw new NotImplementedException();

        }
        public AuditFileInfo(AuditEnvironment env, string name)
        {
            this.AuditEnvironment = env;
            this._Name = name;
        }

        public override bool Exists
        {
            get
            {
                return this.AuditEnvironment.FileExists(this.Name);
            }
        }

        protected string _Name;
    }
}
