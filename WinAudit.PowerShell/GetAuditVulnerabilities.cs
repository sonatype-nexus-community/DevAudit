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
    [OutputType(typeof(List<OSSIndexProjectVulnerability>))]
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
                WriteVerbose(string.Format("Found {0} artifacts, {1} with an OSS Index project id.", base.PackageSource.Artifacts.Count(), base.PackageSource.ArtifactProjects.Count));
            }
            catch (AggregateException ae)
            {

                this.ThrowTerminatingError(new ErrorRecord(new Exception(string.Format("Error encountered searching OSS Index for {0} packages: {1}...", base.PackageSource.PackageManagerLabel, ae.InnerException.Message)),
                    "ArtifactsError", ErrorCategory.InvalidOperation, null));
            }

        }

        protected override void ProcessRecord()
        {
            int projects_count = this.PackageSource.ArtifactProjects.Count;
            int projects_processed = 0;
            int projects_successful = 0;
            while (this.PackageSource.VulnerabilitiesTask.Count() > 0)
            { 
                Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>[] tasks = this.PackageSource.VulnerabilitiesTask.ToArray();
                try
                {
                    int x = Task.WaitAny(tasks);
                    var task = this.PackageSource.VulnerabilitiesTask.Find(t => t.Id == tasks[x].Id);
                    KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> vulnerabilities = task.Result;
                    projects_processed++;
                    projects_successful++;
                    OSSIndexProject p = vulnerabilities.Key;
                    OSSIndexArtifact a = base.PackageSource.Artifacts.First(sa => sa.Package.Name == p.Package.Name && sa.Package.Version == p.Package.Version);                
                    string info = string.Format("[{0}/{1}] {2}", projects_processed, projects_count, a.PackageName);
                    info += string.Format(" {0} ", a.Version);
                    if (vulnerabilities.Value.Count() == 0)
                    {
                        info += string.Format("No known vulnerabilities.");
                    }
                    else
                    {
                        List<OSSIndexProjectVulnerability> found_vulnerabilities = new List<OSSIndexProjectVulnerability>(vulnerabilities.Value.Count());
                        foreach (OSSIndexProjectVulnerability vulnerability in vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList())
                        {
                            if (vulnerability.Versions.Any(v => !string.IsNullOrEmpty(v) && this.PackageSource.IsVulnerabilityVersionInPackageVersionRange(p.Package.Version, v)))
                            {
                                found_vulnerabilities.Add(vulnerability);
                            }
                        }
                        //found_vulnerabilities = found_vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList();
                        if (found_vulnerabilities.Count() > 0)
                        {                            
                            info += string.Format("[VULNERABLE]");
                        }

                        info += string.Format("{0} distinct ({1} total) known vulnerabilities, ", vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).Count(),
                            vulnerabilities.Value.Count());
                        info += string.Format("{0} affecting installed version.\n", found_vulnerabilities.Count());
                        WriteInformation(info, new string[] { "Audit", "Vulnerabilities" });
                        info = "";
                        found_vulnerabilities.ForEach(v =>
                        {                            
                            if (!string.IsNullOrEmpty(v.CVEId)) info += string.Format("{0} ", v.CVEId);
                            info += string.Format(v.Title) + "\n";
                            info += string.Format(v.Summary) + "\n";
                            info += string.Format("Affected versions: ");
                            info += string.Format(string.Join(", ", v.Versions.ToArray()));
                        });
                        WriteInformation(info, new string[] { "Audit", "Vulnerabilities" });
                        WriteObject(found_vulnerabilities);
                    }                    
                    this.PackageSource.VulnerabilitiesTask.Remove(task);
                }
                catch (AggregateException ae)
                {
                    projects_processed++;
                    string error = "";
                    if (ae.InnerException != null && ae.InnerException is OSSIndexHttpException)
                    {
                        OSSIndexHttpException oe = ae.InnerException as OSSIndexHttpException;
                        OSSIndexArtifact artifact = this.PackageSource.Artifacts.First(a => a.ProjectId == oe.RequestParameter);
                        error += string.Format("[{0}/{1}] {2} ", projects_processed, projects_count, artifact.PackageName, artifact.Version);
                        error += string.Format("{0} HTTP Error searching OSS Index...", artifact.Version);
                        Console.ResetColor();
                        ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                    }
                    else
                    {
                        WriteError(new ErrorRecord(new Exception(string.Format("Unknown error encountered searching OSS Index for vulnerabilities : {0}",
                            ae.Message)), "VulnerabilitiesError", ErrorCategory.InvalidOperation, null));
                    }
                    projects_processed += this.PackageSource.VulnerabilitiesTask.Count(t => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled) - 1;
                    this.PackageSource.VulnerabilitiesTask.RemoveAll(t => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled);
                }
            }
        }
        
    }
}
