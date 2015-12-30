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

        static PackageSource PackagesAudit { get; set; }

        static int Main(string[] args)
        {
            if (!CL.Parser.Default.ParseArguments(args, ProgramOptions))
            {
                return (int) ExitCodes.INVALID_ARGUMENTS;
            }

            CL.Parser.Default.ParseArguments(args, ProgramOptions, (verb, options) => 
            {
                if (verb == "nuget")
                {
                    PackagesAudit = new NuGetPackageSource();
                }
                else if (verb == "msi")
                {
                    PackagesAudit = new MSIPackageSource();                    
                }                          
            });

            if (PackagesAudit == null)
            {
                Console.WriteLine("No package source specified.");
                return (int) ExitCodes.INVALID_ARGUMENTS;

            }
            BPlusTree<string, OSSIndexArtifact>.OptionsV2 cache_file_options = new BPlusTree<string, OSSIndexArtifact>.OptionsV2(PrimitiveSerializer.String,
                new BsonSerializer<OSSIndexArtifact>());
            cache_file_options.CalcBTreeOrder(4, 128);
            cache_file_options.CreateFile = CreatePolicy.IfNeeded;
            cache_file_options.FileName = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache"; //Assembly.GetExecutingAssembly().Location
            cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
            BPlusTree<string, OSSIndexArtifact> cache = new BPlusTree<string, OSSIndexArtifact>(cache_file_options);                    
            Console.Write("Scanning {0} packages...", PackagesAudit.PackageManagerLabel);
            Spinner spinner = new Spinner(100);
            spinner.Start();
            try
            {
                PackagesAudit.GetPackagesTask.Wait();
            }
            catch (AggregateException ae)
            {
                spinner.Stop();
                PrintErrorMessage("\nError(s) encountered scanning for {0} packages: {1}", PackagesAudit.PackageManagerLabel, ae.InnerException.Message);
                return (int)ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            }
            finally
            {
                spinner.Stop();
                spinner = null;             
            }
            Console.WriteLine("\nFound {0} packages.", PackagesAudit.Packages.Count());
            if (ProgramOptions.ListPackages)
            {
                int i = 1;
                foreach (OSSIndexQueryObject package in PackagesAudit.Packages)
                {
                    Console.WriteLine("[{0}/{1}] {2} {3} {4}", i++, PackagesAudit.Packages.Count(), package.Name,
                        package.Version, package.Vendor);
                }
                return 0;
            }
            if (PackagesAudit.Packages.Count() == 0)
            {
                Console.WriteLine("Nothing to do, exiting.");
                return 0;
            }
            else
            {
                Console.Write("Searching OSS Index for {0} {1} packages...", PackagesAudit.Packages.Count(), PackagesAudit.PackageManagerLabel);
            }
            spinner = new Spinner(100);
            spinner.Start();
            try
            {
                PackagesAudit.GetArtifactsTask.Wait();
            }
            catch (AggregateException ae)
            {
                spinner.Stop();
                PrintErrorMessage("\nError encountered searching OSS Index for {0} packages: {1}", PackagesAudit.PackageManagerLabel, ae.InnerException.Message);
                return (int)ExitCodes.ERROR_SEARCHING_OSS_INDEX;
            }
            finally
            {
                spinner.Stop();
                spinner = null;
            }
            Console.WriteLine("\nFound {0} artifacts.", PackagesAudit.Artifacts.Count());
            if (ProgramOptions.ListArtifacts)
            {
                int i = 1;
                foreach (OSSIndexArtifact artifact in PackagesAudit.Artifacts)
                {
                    Console.Write("[{0}/{1}] {2} {3} ", i++, PackagesAudit.Artifacts.Count(), artifact.PackageName,
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
                Console.WriteLine("Found {0} projects.", PackagesAudit.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)));
                return 0;
            }
            Console.WriteLine("Searching OSS Index for vulnerabilities for {0} projects...", PackagesAudit.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId)));
            spinner = new Spinner(100);
            spinner.Start();
            int projects_count = PackagesAudit.Artifacts.Count(r => !string.IsNullOrEmpty(r.ProjectId));
            int projects_processed = 0;
            while (projects_processed < projects_count)
            {
                try
                {
                    int x = Task.WaitAny(PackagesAudit.GetVulnerabilitiesTask);
                    Task<IEnumerable<OSSIndexProjectVulnerability>> completed = PackagesAudit.GetVulnerabilitiesTask[x];
                    ++projects_processed;
                }
                catch (AggregateException ae)
                {
                    PrintErrorMessage("\nError encountered searching OSS Index for vulnerabilities for project id {0}: {1}", 
                        ae.Message, ae.InnerException.Message);
                    ++projects_processed;
                }
                finally
                {
                    
                }
                
            }
            spinner.Stop();
            spinner = null;

            //int i = 0;

            /*
            foreach (OSSIndexQueryResultObject r in PackagesAudit.Projects)
            {
                i++;
                Console.Write("[{0}/{1}] {2} {3} ", i, packages_count, r.PackageName, r.PackageVersion);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ResetColor();
                if (!string.IsNullOrEmpty(r.PackageSCMId))
                {
                    Console.Write("SCM Id: {0} ", r.PackageSCMId);
                    try
                    {
                        IEnumerable<OSSIndexSCMVulnerability> vulns = audit.GetVulnerabilityForSCMId(r.PackageSCMId);
                        if (vulns.Count() == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("No known vulnerabilities.\n");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.Write("{0} known vulnerabilities.\n", vulns.Count());
                            Console.ResetColor();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write("Error retrieving vulnerabilities: {0}.\n", e.Message);
                        Console.ResetColor();

                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("No SCM Id found\n");
                    Console.ResetColor();
                }
            }
            */
            return 0;
        }
           
        static void PrintErrorMessage(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }


    }
}
