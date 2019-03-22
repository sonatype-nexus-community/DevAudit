using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IDeveloperPackageManager
    {
        string DefaultPackageManagerLockFile { get; }

        string PackageManagerLockFile {get; set;}

        //string GetLockFilePackageVersion(string packageName);
    }
}
