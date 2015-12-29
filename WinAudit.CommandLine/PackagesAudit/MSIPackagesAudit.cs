using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinAudit.AuditLibrary;

namespace WinAudit.CommandLine
{
    class MSIPackagesAudit : IPackagesAudit
    {
        public Audit PackagesAudit { get; set; }
        public string PackageManagerId { get { return "msi"; } }
        public string PackageManagerLabel { get { return "MSI"; } }
        public Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask { get; set; }
        public IEnumerable<OSSIndexQueryObject> Packages { get; set; }
        public IEnumerable<OSSIndexQueryResultObject> Projects { get; set; }
        public Task<IEnumerable<OSSIndexQueryResultObject>> GetProjectsTask
        {
            get
            {
                return Task<IEnumerable<OSSIndexQueryResultObject>>.Run(async () =>
                    this.Projects = await this.PackagesAudit.SearchOSSIndexAsync("msi", this.Packages));
            }
        }

        
        #region Constructors
        public MSIPackagesAudit()
        {
            this.PackagesAudit = new Audit("1.1");
            this.GetPackagesTask = new Task<IEnumerable<OSSIndexQueryObject>>(() => this.Packages = this.PackagesAudit.GetMSIPackages());                                 
        }
        #endregion

    }
}
