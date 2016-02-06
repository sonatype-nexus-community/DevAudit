using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

using CommandLine.Text;

namespace WinAudit.CommandLine
{
    class Options
    {
        public Options()
        {            
            
        }

        [VerbOption("nuget", HelpText = "Audit NuGet packages. Use the --file option to specify a particular packages.config file otherwise the one in the current directory will be used.")]
        public Options AuditNuGet { get; set; }

        [VerbOption("msi", HelpText = "Audit MSI packages. Packages are scanned from the registry.")]
        public Options AuditMsi { get; set; }

        [VerbOption("choco", HelpText = "Audit Chocolatey packages. Packages are scanned from C:\\ProgramData\\chocolatey.")]
        public Options AuditChocolatey { get; set; }

        [VerbOption("bower", HelpText = "Audit Bower packages. Use the --file option to specify a particular bower.json file otherwise the one in the current directory will be used.")]
        public Options AuditBower { get; set; }

        [VerbOption("oneget", HelpText = "Audit OneGet packages. Packages are scanned from the system OneGet repository.")]
        public Options AuditOneGet { get; set; }

        [VerbOption("composer", HelpText = "Audit PHP Composer packages. Use the --file option to specify a particular composer.json file otherwise the one in the current directory will be used.")]
        public Options AuditComposer { get; set; }

        [VerbOption("drupal", HelpText = "Audit a Drupal application instance. Use the --root option to specify the root directory of the Drupal instance, otherwise the current directory will be used.")]
        public Options AuditDrupal { get; set; }

        [Option('f', "file", Required = false, HelpText = "Specifies the file (if any) containing the packages to be audited.")]
        public string File { get; set; }

        [Option('p', "list-packages", Required = false, HelpText = "Only list the local packages that will be audited.")]
        public bool ListPackages { get; set; }

        [Option('a', "list-artifacts", Required = false, HelpText = "Only list the artifacts corresponding to local packages found on OSS Index.")]
        public bool ListArtifacts { get; set; }

        [Option('n', "non-interact", Required = false, HelpText = "Disable any interctive console output (for redirecting console output to other devices.)")]
        public bool NonInteractive { get; set; }

        [Option('c', "cache", Required = false, HelpText = "Cache results from querying OSS Index. Projects and vulnerabilities will be cached by default for 180 minutes or the duration specified by the --cache-ttl parameter.")]
        public bool Cache { get; set; }

        [Option('t', "cache-ttl", Required = false, HelpText = "Cache TTL - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public string CacheTTL { get; set; }

        [Option('k', "cache-dump", Required = false, HelpText = "Cache Dump - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public bool CacheDump { get; set; }

        [Option('r', "root", Required = false, HelpText = "The root directory of the application instance to audit.")]
        public string RootDirectory { get; set; }


        [ParserState]
        public IParserState LastParserState { get; set; }
        
        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
     
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
