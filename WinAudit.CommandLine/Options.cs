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
        [Option('o', "oneget", Required = false, MutuallyExclusiveSet = "PM", HelpText = "Audit OneGet packages.")]
        public bool AuditOneGet { get; set; }

        [Option('m', "msi", Required = false, MutuallyExclusiveSet = "PM", HelpText = "Audit MSI packages.")]
        public bool AuditMsi { get; set; }

        [Option('b', "bower", Required = false, MutuallyExclusiveSet = "PM", HelpText = "Audit Bower packages.")]
        public bool AuditBower { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
