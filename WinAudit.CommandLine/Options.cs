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

        [Option('l', "list", Required = false, HelpText = "Only list the packages that will be audited.")]
        public bool List { get; set; }

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
