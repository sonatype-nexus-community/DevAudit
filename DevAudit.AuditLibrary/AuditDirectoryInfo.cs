using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class AuditDirectoryInfo : FileSystemInfo
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

        public IEnumerable<AuditDirectoryInfo> GetDirectories(string path)
        {
            throw new NotImplementedException();
        }
        public AuditDirectoryInfo(AuditEnvironment env, string name)
        {
            this.AuditEnvironment = env;
            this._Name = name;
        }

        public override bool Exists
        {
            get
            {
                return this.AuditEnvironment.DirectoryExists(this.Name);
            }
        }

        protected string _Name;
    }
}
