using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IArtifact
    {
        string ArtifactId { get; }
        string PackageId { get; set; }
        string PackageName { get; set; }
        string PackageManager { get; set; }
        string PackageUrl { get; set; }
        string Description { get; set; }
        string Version { get; set; }
        ArtifactVersion[] Versions { get; set; }
    }

    public class ArtifactVersion
    {
        public string Version { get; set; }
        public DateTime PublishedDate { get; set; }
        public ArtifactVersion(string version, DateTime date)
        {
            this.Version = version;
            this.PublishedDate = date;
        }
    }

}

