using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public class VulnersdotcomAuditDataSource : HttpDataSource
    {
        #region Constructors
        public VulnersdotcomAuditDataSource(AuditTarget target, AuditEnvironment host_env, Dictionary<string, object> options) : base(target, host_env, options)
        {
        }
        #endregion

        #region Overriden methods
        public override bool IsEligibleForTarget(AuditTarget target)
        {
            if (target is PackageSource)
            {
                PackageSource source = target as PackageSource;
                string[] eligible_sources = { "dpkg", "rpm", "yum" };
                return eligible_sources.Contains(source.PackageManagerId);
            }
            else return false;
        }
        #endregion

        #region Overriden methods
        public override Task<Dictionary<IPackage, List<IVulnerability>>> SearchVulnerabilities(List<Package> packages)
        {
            throw new NotImplementedException();
        }

        public override Task<Dictionary<IPackage, List<IArtifact>>> SearchArtifacts(List<Package> packages)
        {
            throw new NotImplementedException();
        }

        public override int MaxConcurrentSearches
        {
            get
            {
                return 10;
            }
        }
        #endregion

    }
}
