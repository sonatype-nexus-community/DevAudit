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
        static int Main(string[] args)
        {
            var options = new Options();

            if (!CL.Parser.Default.ParseArguments(args, options))
            {
                return 1;
            }

            BPlusTree<string, OSSIndexQueryResultObject>.OptionsV2 cache_file_options = new BPlusTree<string, OSSIndexQueryResultObject>.OptionsV2(PrimitiveSerializer.String,
                new BsonSerializer<OSSIndexQueryResultObject>());
            cache_file_options.CalcBTreeOrder(4, 128);
            cache_file_options.CreateFile = CreatePolicy.IfNeeded;
            cache_file_options.FileName = AppDomain.CurrentDomain.BaseDirectory + "winaudit-net.cache"; //Assembly.GetExecutingAssembly().Location
            cache_file_options.StoragePerformance = StoragePerformance.CommitToDisk;
            BPlusTree<string, OSSIndexQueryResultObject> cache = new BPlusTree<string, OSSIndexQueryResultObject>(cache_file_options);
            if (options.AuditOneGet)
            {
                Console.WriteLine("Scanning OneGet packages...");
                Audit audit = new Audit("1.1");
                IEnumerable<OSSIndexQueryObject> packages = audit.GetOneGetPackages();
                Console.WriteLine("Found {0} OneGet packages.", packages.Count());
            }


            return 0;
        }
    }
}
