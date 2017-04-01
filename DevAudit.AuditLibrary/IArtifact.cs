using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IArtifact
    {
        string PackageName { get; set; }
        string PackageManager { get; set; }
        string PackageUrl { get; set; }
        string Description { get; set; }
        ArtifactVersion[] Versions { get; set; }
        ArtifactVersion LatestStableVersion { get; set; }
    }
}

public interface ArtifactVersion
{
    string Version { get; set; }
    DateTime PublishedDate { get; set; }
}
