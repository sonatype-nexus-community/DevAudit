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
        }

        static Options ProgramOptions = new Options();

        static IPackagesAudit PackagesAudit { get; set; }

        static int Main(string[] args)
        {
            if (!CL.Parser.Default.ParseArguments(args, ProgramOptions))
            {
                return (int) ExitCodes.INVALID_ARGUMENTS;
            }

            CL.Parser.Default.ParseArguments(args, ProgramOptions, (verb, options) => 
            {
                if (verb == "msi")
                {
                    PackagesAudit = new MSIPackagesAudit();                    
                }                          
            });                        

            BPlusTree<string, OSSIndexQueryResultObject>.OptionsV2 cache_file_options = new BPlusTree<string, OSSIndexQueryResultObject>.OptionsV2(PrimitiveSerializer.String,
                new BsonSerializer<OSSIndexQueryResultObject>());
            cache_file_options.CalcBTreeOrder(4, 128);
            cache_file_options.CreateFile = CreatePolicy.IfNeeded;
            cache_file_options.FileName = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache"; //Assembly.GetExecutingAssembly().Location
            cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
            BPlusTree<string, OSSIndexQueryResultObject> cache = new BPlusTree<string, OSSIndexQueryResultObject>(cache_file_options);                    
            Console.Write("Scanning {0} packages...", PackagesAudit.PackageManagerLabel);
            Spinner spinner = new Spinner(100);
            spinner.Start();
            PackagesAudit.GetPackagesTask.Start();
            try
            {
                PackagesAudit.GetPackagesTask.Wait();
            }
            catch (AggregateException ae)
            {
                PrintErrorMessage("Error(s) encountered scanning for {0} packages: {1}", PackagesAudit.PackageManagerLabel, ae.InnerException);
                return (int)ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            }
            finally
            {
                spinner.Stop();
                spinner = null;
            }

            Console.WriteLine("\nFound {0} packages.", PackagesAudit.Packages.Count());
            if (PackagesAudit.Packages.Count() == 0)
            {
                Console.WriteLine("Nothing to do, exiting.");
                return 0;
            }
            else
            {
                Console.Write("\nSearching OSS Index for {0} MSI packages...", PackagesAudit.Packages.Count());
            }
            spinner = new Spinner(100);
            spinner.Start();            
            try
            {
                PackagesAudit.GetProjectsTask.Wait();
            }
            catch (AggregateException ae)
            {
                PrintErrorMessage("Error encountered searching OSS Index for {0} packages: {1}", PackagesAudit.PackageManagerLabel, ae.InnerException);
                return (int)ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            }
            finally
            {
                spinner.Stop();
                spinner = null;
            }
            
            return 0;
        }

        static Task<IEnumerable<OSSIndexQueryObject>> GetPackagesTaskForOptions()
        {
            Task<IEnumerable<OSSIndexQueryObject>> task = null;  
           /* if (ProgramOptions.AuditMsi)
            {
                PackageManagerLabel = "MSI";
                PackageManagerId = "msi";
                task = Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => ProgramAudit.GetMSIPackages());
            }
            
            if (ProgramOptions.AuditChocolatey)
            {
                PackageManagerLabel = "Chocolatey";
                PackageManagerId = "choco";
                task = Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => ProgramAudit.GetChocolateyPackages());
            }
            if (ProgramOptions.AuditOneGet)
            {
                PackageManagerLabel = "OneGet";
                PackageManagerId = "oneget";
                task = Task<IEnumerable<OSSIndexQueryObject>>.Factory.StartNew(() => ProgramAudit.GetOneGetPackages());
            }*/
            return task;
        }

        static List<Task<IEnumerable<OSSIndexQueryResultObject>>> GetSearchTasksForPackages()
        {
            return null;
        }

        //ivate static Task<Dictionary<OSSIndexQueryResultObject, IEnumerable<OSSIndexProjectVulnerability>>> GetVulnerabilitiesTasksForPackages

        
        static void PrintErrorMessage(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }


    }
}
