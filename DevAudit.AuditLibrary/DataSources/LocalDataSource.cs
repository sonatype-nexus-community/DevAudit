using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public abstract class LocalDataSource : IDataSource
    {
        #region Constructors
        public LocalDataSource(AuditTarget target, AuditEnvironment host_env, Dictionary<string, object> datasource_options)
        {
            if (!datasource_options.ContainsKey("DirectoryPath")) throw new ArgumentException("The datasource options does not contain the DirectoryPath");
            this.DataSourceOptions = datasource_options;
            this.Target = target;
            this.HostEnvironment = host_env;
            string dir_path = (string)this.DataSourceOptions["DirectoryPath"];
            try
            {
                DirectoryInfo dir = new DirectoryInfo(dir_path);
                if (!dir.Exists)
                {
                    HostEnvironment.Error("The directory {0} does not exist.", dir.FullName);
                    this.Initialised = false;
                    return;
                }
                else
                {
                    Directory = dir;
                }
            }
            catch (Exception e)
            {
                this.HostEnvironment.Error(e, "An error occurred attempting to access the directory {0}.", dir_path);
                this.Initialised = false;
                return;
            }
        }
        #endregion

        #region Abstract methods
        public abstract Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages);
        public abstract Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages);
        public abstract Task<List<IVulnerability>> UpdateVulnerabilities();
        public abstract bool IsEligibleForTarget(AuditTarget target);
        #endregion

        #region Abstract properties
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract int MaxConcurrentSearches { get; }
        #endregion

        #region Properties
        public AuditTarget Target { get; protected set; }
        public Dictionary<string, object> DataSourceOptions { get; protected set; } 
        public DirectoryInfo Directory { get; protected set; }
        public bool Initialised { get; protected set; } = false;
        protected AuditEnvironment HostEnvironment { get; set; }
        #endregion
    }
}
