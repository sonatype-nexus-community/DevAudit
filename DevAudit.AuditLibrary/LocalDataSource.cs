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
        #region Abstract methods
        public abstract Task<IArtifact> SearchArtifacts(List<IPackage> packages);
        public abstract Task<List<IVulnerability>> SearchVulnerabilities(List<IPackage> packages);
        public abstract Task<List<IVulnerability>> UpdateVulnerabilities();
        #endregion

        #region Constructors
        public LocalDataSource(AuditEnvironment host_env, Dictionary<string, object> datasource_options)
        {
            if (!datasource_options.ContainsKey("DirectoryPath")) throw new ArgumentException("The datasource options does not contain the DirectoryPath");
            this.DataSourceOptions = datasource_options;
            this.HostEnvironment = host_env;
            string dir_path = (string)this.DataSourceOptions["DirectoryPath"];
            try
            {
                DirectoryInfo dir = new DirectoryInfo(dir_path);
                if (!dir.Exists)
                {
                    HostEnvironment.Error("The directory {0} does not exist.", dir.FullName);
                    this.DataSourceInitialised = false;
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
                this.DataSourceInitialised = false;
                return;
            }
        }
        #endregion

        #region Properties
        
        public Dictionary<string, object> DataSourceOptions { get; protected set; } 
        public DirectoryInfo Directory { get; protected set; }
        public bool DataSourceInitialised { get; protected set; } = false;
        protected AuditEnvironment HostEnvironment { get; set; }
        #endregion
    }
}
