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
                        PrintErrorMessage("Error in parameter: Could not find file {0}", ProgramOptions.File);
                        return (int)ExitCodes.INVALID_ARGUMENTS;
                    }
                    else
                    {
                        Source.PackageSourceOptions.Add("File", ProgramOptions.File);
                    }
                }
            }

            BPlusTree<string, OSSIndexArtifact>.OptionsV2 cache_file_options = new BPlusTree<string, OSSIndexArtifact>.OptionsV2(PrimitiveSerializer.String,
                new BsonSerializer<OSSIndexArtifact>());
            cache_file_options.CalcBTreeOrder(4, 128);
            cache_file_options.CreateFile = CreatePolicy.IfNeeded;
            cache_file_options.FileName = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache"; //Assembly.GetExecutingAssembly().Location
            cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
            BPlusTree<string, OSSIndexArtifact> cache = new BPlusTree<string, OSSIndexArtifact>(cache_file_options);                    
            Console.Write("Scanning {0} packages...", Source.PackageManagerLabel);
            Spinner spinner = new Spinner(50);
            spinner.Start();
            try
            {
                Source.GetPackagesTask.Wait();
            }
            catch (AggregateException ae)
            {
                spinner.Stop();
                PrintErrorMessage("\nError(s) encountered scanning for {0} packages: {1}", Source.PackageManagerLabel, ae.InnerException.Message);
                return (int)ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            }
            finally
            {
                spinner.Stop();
                spinner = null;             
            }
            Console.WriteLine("\nFound {0} packages.", Source.Packages.Count());
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
            spinner = new Spinner(50);
            spinner.Start();
            try
            {
                Source.GetArtifactsTask.Wait();
            }
            catch (AggregateException ae)
            {
                spinner.Stop();
                PrintErrorMessage("\nError encountered searching OSS Index for {0} packages: {1}", Source.PackageManagerLabel, ae.InnerException.Message);
                ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                return (int)ExitCodes.ERROR_SEARCHING_OSS_INDEX;
            }
            finally
            {
                spinner.Stop();
                spinner = null;
            }
            Console.WriteLine("\nFound {0} artifacts.", Source.Artifacts.Count());
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
                    Console.Write("[{0}/{1}] {2} {3} ", i++, Source.Artifacts.Count(), artifact.PackageName,
                        artifact.Version);
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
            Console.WriteLine("Searching OSS Index for vulnerabilities for {0} projects...", Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)));
            int projects_count = Source.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId));
            int projects_processed = 0;
            while (Source.GetVulnerabilitiesTask.Count() > 0)
            {
                Task<IEnumerable<OSSIndexProjectVulnerability>>[] tasks = Source.GetVulnerabilitiesTask.ToArray();
                int x = Task.WaitAny(tasks);
                var task = Source.GetVulnerabilitiesTask.Find(t => t.Id == tasks[x].Id);
                    if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                    {
                        IEnumerable<OSSIndexProjectVulnerability> v = task.Result;
                        OSSIndexProject p = Source.Vulnerabilities.Where(sv => sv.Value == v).First().Key;
                        OSSIndexArtifact a = Source.Artifacts.First(sa => sa.ProjectId == p.Id.ToString());
                        Console.Write("[{0}/{1}] {2}", ++projects_processed, projects_count, a.PackageName);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(" {0} ", a.Version);
                        if (v.Count() == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(" No known vulnerabilities. ");
                        }
                        Console.ResetColor();
                        Console.Write("\n");
                    }
                    else
                    {
                        projects_processed++;
                        if (task.Exception != null)
                        {
                            PrintErrorMessage("\nError encountered searching OSS Index for vulnerabilities: {0}.", task.Exception.Message);
                            task.Exception.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                        }
                        else
                        {
                            PrintErrorMessage("Unknown error encountered searching OSS Index for vulnerabilities on task id {0} with task status {1}.", 
                                task.Id, task.Status.ToString());
                        }
                    }
                    Source.GetVulnerabilitiesTask.Remove(task);
                }
            
            return 0;
        }
           
        static void PrintErrorMessage(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static void HandleOSSIndexHttpException(Exception e)
        {
            if (e.GetType() == typeof(OSSIndexHttpException))
            {
                OSSIndexHttpException oe = (OSSIndexHttpException) e;
                PrintErrorMessage("HTTP error encountered searching OSS Index. HTTP status {0}: {1}", oe.StatusCode, oe.Request);
            }

        }


    }
}
