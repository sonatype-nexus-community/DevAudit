using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IDataSource
    {
        #region Public methods
        Task<IArtifact> SearchArtifacts(List<IPackage> packages);
        Task<List<IVulnerability>> SearchVulnerabilities(List<IPackage> packages);
        #endregion
    }
}
