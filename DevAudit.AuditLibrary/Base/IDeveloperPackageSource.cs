using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IDeveloperPackageSource
    {
        string DefaultPackageSourceLockFile { get; }

        string PackageSourceLockFile {get; set;}

        bool PackageVersionIsRange(string version);

        //string GetLockFilePackageVersion(string packageName);
    }
}
