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

        [VerbOption("nuget", HelpText = "Audit NuGet packages.")]
        public Options AuditNuGet { get; set; }

        [VerbOption("msi", HelpText = "Audit MSI packages.")]
        public Options AuditMsi { get; set; }

        [Option('p', "list-packages", Required = false, HelpText = "Only list the local packages that will be audited.")]
        public bool ListPackages { get; set; }

        [Option('a', "list-artifacts", Required = false, HelpText = "Only list the artifacts corresponding to local packages found on OSS Index.")]
        public bool ListArtifacts { get; set; }


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
