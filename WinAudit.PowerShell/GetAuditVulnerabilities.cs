using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;

using WinAudit.AuditLibrary;

namespace WinAudit.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "AuditVulnerabilities")]
    [OutputType(typeof(IEnumerable<OSSIndexArtifact>))]
    public class GetAuditVulnerabillities : WinAuditCmdlet
    {

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            WriteVerbose(string.Format("Scanning {0} packages...", this.Source));
            try
            {
                base.PackageSource.PackagesTask.Wait();
            }
            catch (AggregateException ae)
            {

                this.ThrowTerminatingError(new ErrorRecord(new Exception(string.Format("Error(s) encountered scanning for {0} packages: {1}", PackageSource.PackageManagerLabel, ae.InnerException.Message)),
                    "PackagesError", ErrorCategory.InvalidOperation, null));
            }
            try
            {
                WriteVerbose(string.Format("Searching OSS Index for {0} {1} packages...", base.PackageSource.Packages.Count(), base.PackageSource.PackageManagerLabel));
                Task.WaitAll(base.PackageSource.ArtifactsTask.ToArray());
                WriteVerbose(string.Format("\nFound {0} artifacts, {1} with an OSS Index project id.", base.PackageSource.Artifacts.Count(), base.PackageSource.ArtifactProjects.Count));
            }
            catch (AggregateException ae)
            {

                this.ThrowTerminatingError(new ErrorRecord(new Exception(string.Format("\nError encountered searching OSS Index for {0} packages: {1}...", base.PackageSource.PackageManagerLabel, ae.InnerException.Message)),
                    "ArtifactsError", ErrorCategory.InvalidOperation, null));
            }

        }

        protected override void ProcessRecord()
        {            
            WriteObject(this.PackageSource.Artifacts);
        }
        
    }
}
