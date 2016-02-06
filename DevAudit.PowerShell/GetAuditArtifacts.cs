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
    [Cmdlet(VerbsCommon.Get, "AuditArtifacts")]
    [OutputType(typeof(OSSIndexArtifact))]
    public class GetAuditArtifacts : DevAuditCmdlet
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
            int i = 1;
            foreach (OSSIndexArtifact artifact in this.PackageSource.Artifacts)
            {
                WriteObject(artifact);
                WriteInformation(string.Format("[{0}/{1}] {2} ({3}) {4} ", i++, this.PackageSource.Artifacts.Count(), artifact.PackageName,
                    !string.IsNullOrEmpty(artifact.Version) ? artifact.Version : "No version found", 
                    !string.IsNullOrEmpty(artifact.ProjectId) ? artifact.ProjectId : "No project id found."), new string[] { "Audit", "Artifact" });                
            }
            
        }
        
    }
}
