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
        Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages);
        Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages);
        bool IsEligibleForTarget(AuditTarget target);
        #endregion

        #region Public properties
        bool Initialised { get; }
        string Name { get; }
        string Description { get; }
        int MaxConcurrentSearches { get; }
        #endregion
    }
}
