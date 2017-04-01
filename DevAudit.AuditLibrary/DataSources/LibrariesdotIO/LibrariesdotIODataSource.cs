using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary.DataSources.LibrariesdotIO
{
    public class LibrariesdotIODataSource : HttpDataSource
    {
        #region Constructors
        public LibrariesdotIODataSource(AuditEnvironment host_env, Dictionary<string, object> datasource_options) : base(host_env, datasource_options)
        {

        }
        #endregion

        #region Overriden methods
        public override Task<IArtifact> SearchArtifacts(List<IPackage> packages)
        {
            throw new NotImplementedException();
        }

        public override Task<List<IVulnerability>> SearchVulnerabilities(List<IPackage> packages)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
