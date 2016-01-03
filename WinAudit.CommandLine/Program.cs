using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using CL = CommandLine; //Avoid type name conflict with external CommandLine library
using CommandLine.Text;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Serialization;
using Newtonsoft.Json;

using WinAudit.AuditLibrary;

namespace WinAudit.CommandLine
{
    class Program
    {
        public enum ExitCodes
        {
            INVALID_ARGUMENTS = 1,
            NO_PACKAGE_MANAGER,
            ERROR_SCANNING_FOR_PACKAGES,
            ERROR_SEARCHING_OSS_INDEX
        }

        static Options ProgramOptions = new Options();

        static PackageSource Source { get; set; }

        static int Main(string[] args)
        {
            CL.Parser.Default.ParseArguments(args, ProgramOptions, (verb, options) =>
            {
                if (verb == "nuget")
                {
                    Source = new NuGetPackageSource();
                }
                else if (verb == "msi")
                {
                    Source = new MSIPackageSource();
                }
                else if (verb == "choco")
                {
                    Source = new ChocolateyPackageSource();
                }
                else if (verb == "bower")
                {
                    Source = new BowerPackageSource();
                }
                else if (verb == "oneget")
                {
                    Source = new OneGetPackageSource();
                }
            });
            if (Source == null)
            {
                Console.WriteLine("No package source specified.");
                return (int)ExitCodes.INVALID_ARGUMENTS;
            }
            if (!CL.Parser.Default.ParseArguments(args, ProgramOptions))
            {
                return (int)ExitCodes.INVALID_ARGUMENTS;
            }
            else
            {
                if (!string.IsNullOrEmpty(ProgramOptions.File))
                {
                    if (!File.Exists(ProgramOptions.File))
                    {
                        PrintErrorMessage("Error in parameter: Could not find file {0}.", ProgramOptions.File);
                        return (int)ExitCodes.INVALID_ARGUMENTS;
                    }
                    else
                    {
                        Source.PackageSourceOptions.Add("File", ProgramOptions.File);
                    }
                }
            }
            Spinner spinner = null;
            BPlusTree<string, OSSIndexArtifact>.OptionsV2 cache_file_options = new BPlusTree<string, OSSIndexArtifact>.OptionsV2(PrimitiveSerializer.String,
                new BsonSerializer<OSSIndexArtifact>());
            cache_file_options.CalcBTreeOrder(4, 128);
            cache_file_options.CreateFile = CreatePolicy.IfNeeded;
            cache_file_options.FileName = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache"; //Assembly.GetExecutingAssembly().Location
            cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
            BPlusTree<string, OSSIndexArtifact> cache = new BPlusTree<string, OSSIndexArtifact>(cache_file_options);                    
            Console.Write("Scanning {0} packages...", Source.PackageManagerLabel);
            if (!ProgramOptions.NonInteractive)
            {
                spinner = new Spinner(50);
                spinner.Start();
            }
            try
            {
                Source.PackagesTask.Wait();
            }
            catch (AggregateException ae)
            {
                if (!ProgramOptions.NonInteractive) spinner.Stop();
                PrintErrorMessage("\nError(s) encountered scanning for {0} packages: {1}", Source.PackageManagerLabel, ae.InnerException.Message);
                return (int)ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            }
            finally
            {
                if (!ProgramOptions.NonInteractive)
                {
                    spinner.Stop();
                    spinner = null;
                }             
            }
            Console.WriteLine("\nFound {0} distinct packages.", Source.Packages.Count());             
            if (ProgramOptions.ListPackages)
            {
                int i = 1;
                foreach (OSSIndexQueryObject package in Source.Packages)
                {
                    Console.WriteLine("[{0}/{1}] {2} {3} {4}", i++, Source.Packages.Count(), package.Name,
                        package.Version, package.Vendor);
                }
                return 0;
            }
            if (Source.Packages.Count() == 0)
            {
                Console.WriteLine("Nothing to do, exiting.");
                return 0;
            }
            else
            {
                Console.Write("Searching OSS Index for {0} {1} packages...", Source.Packages.Count(), Source.PackageManagerLabel);
            }
            if (!ProgramOptions.NonInteractive)
            {
                spinner = new Spinner(50);
                spinner.Start();
            }
            try
            {
                Task.WaitAll(Source.ArtifactsTask.ToArray());
            }
            catch (AggregateException ae)
            {
                if (!ProgramOptions.NonInteractive) spinner.Stop();
                PrintErrorMessage("\nError encountered searching OSS Index for {0} packages: {1}...", Source.PackageManagerLabel, ae.InnerException.Message);
                ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                return (int)ExitCodes.ERROR_SEARCHING_OSS_INDEX;
            }
            finally
            {
                if (!ProgramOptions.NonInteractive)
                {
                    spinner.Stop();
                    spinner = null;
                }
            }
            Console.WriteLine("\nFound {0} artifacts, {1} with an OSS Index project id.", Source.Artifacts.Count(), Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)));
            if (Source.Artifacts.Count() == 0)
            {
                Console.WriteLine("Nothing to do, exiting.");
                return 0;
            }
            if (ProgramOptions.ListArtifacts)
            {
                int i = 1;
                foreach (OSSIndexArtifact artifact in Source.Artifacts)
                {
                    Console.Write("[{0}/{1}] {2} ({3}) ", i++, Source.Artifacts.Count(), artifact.PackageName,
                        !string.IsNullOrEmpty(artifact.Version) ? artifact.Version : "No version found");
                    if (!string.IsNullOrEmpty(artifact.ProjectId))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(artifact.ProjectId + "\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write("No project id found.\n");
                        Console.ResetColor();
                    }
                }
                return 0;
            }
            if (Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)) == 0)
            {
                Console.WriteLine("No found artifacts have associated projects.");
                Console.WriteLine("No vulnerability data for you packages currently exists in OSS Index, exiting.");
                return 0;
            }
            Console.WriteLine("Searching OSS Index for vulnerabilities for {0} projects...", Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)));
            int projects_count = Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId));
            int projects_processed = 0;
            int projects_successful = 0;
            while (Source.VulnerabilitiesTask.Count() > 0)
            {
                Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>[] tasks = Source.VulnerabilitiesTask.ToArray();
                try
                {
                    int x = Task.WaitAny(tasks);
                    var task = Source.VulnerabilitiesTask.Find(t => t.Id == tasks[x].Id);
                    KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> vulnerabilities = task.Result;
                    if (projects_processed++ == 0)
                    {
                        Console.WriteLine("\nAudit Results\n=============");
                    }
                    projects_successful++;
                    OSSIndexProject p = vulnerabilities.Key;
                    OSSIndexArtifact a = Source.Artifacts.First(sa => sa.Package.Name == p.Package.Name && sa.Package.Version == p.Package.Version);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("[{0}/{1}] {2}", projects_processed, projects_count, a.PackageName);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.ResetColor();
                    Console.Write(" {0} ", a.Version);
                    if (vulnerabilities.Value.Count() == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("No known vulnerabilities.");
                    }
                    else
                    {
                        List<OSSIndexProjectVulnerability> found_vulnerabilities = new List<OSSIndexProjectVulnerability>(vulnerabilities.Value.Count());
                        foreach (OSSIndexProjectVulnerability vulnerability in vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList())
                        {
                            if (vulnerability.Versions.Any(v => Source.PackageVersionInRange(p.Package.Version, v)))
                            {
                                found_vulnerabilities.Add(vulnerability);
                            }
                        }
                        //found_vulnerabilities = found_vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList();
                        if (found_vulnerabilities.Count() > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[VULNERABLE]");
                        }
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("{0} distinct ({1} total) known vulnerabilities, ", vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).Count(),
                            vulnerabilities.Value.Count());
                        Console.WriteLine("{0} affecting installed version.", found_vulnerabilities.Count());
                        found_vulnerabilities.ForEach(v =>
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            if (!string.IsNullOrEmpty(v.CVEId)) Console.Write("{0} ", v.CVEId);
                            Console.WriteLine(v.Title);
                            Console.ResetColor();
                            Console.WriteLine(v.Summary);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("\nAffected versions: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(string.Join(", ", v.Versions.ToArray()));
                        });
                    }
                    Console.ResetColor();
                    Source.VulnerabilitiesTask.Remove(task);
                }
                catch (AggregateException ae)
                {
                    if (projects_processed++ == 0)
                    {
                        Console.WriteLine("\nAudit Results\n=============");
                    }
                    if (ae.InnerException != null && ae.InnerException is OSSIndexHttpException)
                    {
                        OSSIndexHttpException oe = ae.InnerException as OSSIndexHttpException;
                        OSSIndexArtifact artifact = Source.Artifacts.First(a => a.ProjectId == oe.RequestParameter);
                        Console.Write("[{0}/{1}] {2} ", projects_processed, projects_count, artifact.PackageName, artifact.Version);
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("{0} HTTP Error searching OSS Index...", artifact.Version);
                        Console.ResetColor();
                        ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                    }
                    else
                    {
                        PrintErrorMessage("Unknown error encountered searching OSS Index for vulnerabilities : {0}",
                            ae.Message);
                    }
                    Source.VulnerabilitiesTask.RemoveAll(t => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled);
                }
            }
            return 0;
        }
           
        static void PrintErrorMessage(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static void HandleOSSIndexHttpException(Exception e)
        {
            if (e.GetType() == typeof(OSSIndexHttpException))
            {
                OSSIndexHttpException oe = (OSSIndexHttpException) e;
                PrintErrorMessage("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}", 
                    (int) oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request);
            }

        }


    }
}
