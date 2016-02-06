using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using DevAudit.AuditLibrary;

namespace DevAudit.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "AuditPackages")]
    [OutputType(typeof(OSSIndexQueryObject))]
    public class GetAuditPackages : DevAuditCmdlet
    {

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            WriteVerbose(string.Format("Scanning {0} packages...", this.Source));
            try
            {
                base.PackageSource.PackagesTask.Wait();
                WriteVerbose(string.Format("Found {0} distinct packages.", base.PackageSource.Packages.Count()));
            }
            catch (AggregateException ae)
            {

                this.ThrowTerminatingError(new ErrorRecord(new Exception(string.Format("Error(s) encountered scanning for {0} packages: {1}", PackageSource.PackageManagerLabel, ae.InnerException.Message)),
                    "PackagesError", ErrorCategory.InvalidOperation, null));
            }

        }

        protected override void ProcessRecord()
        {
            int i = 1;
            foreach (OSSIndexQueryObject package in this.PackageSource.Packages)
            {          
                WriteInformation(string.Format("[{0}/{1}] {2} {3} {4}", i++, this.PackageSource.Packages.Count(), package.Name,
                        package.Version, package.Vendor), new string[] {"Audit", "Package"});
                WriteObject(package);
            }
        }
        
    }
}
