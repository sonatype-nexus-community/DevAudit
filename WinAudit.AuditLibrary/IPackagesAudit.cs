using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinAudit.AuditLibrary
{
    public interface IPackagesAudit
    {
        #region Public properties
        OSSIndexHttpClient HttpClient { get;  }

        string PackageManagerId { get; }

        string PackageManagerLabel { get; }

        Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTask { get; }

        IEnumerable<OSSIndexQueryObject> Packages { get; set; }

        Task<IEnumerable<OSSIndexArtifact>> GetArtifactsTask { get;}

        IEnumerable<OSSIndexArtifact> Artifacts { get; set; }

        Task<IEnumerable<OSSIndexProjectVulnerability>>[] GetVulnerabilitiesTask { get; }

        ConcurrentDictionary<string, IEnumerable<OSSIndexProjectVulnerability>> Vulnerabilities { get; set; }
        #endregion

    }
}
