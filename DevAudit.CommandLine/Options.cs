using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;

using CommandLine.Text;

namespace DevAudit.CommandLine
{
    class Options
    {
        public Options() {}

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

		[VerbOption("dpkg", HelpText = "Audit dpkg packages. The packages are scanned from the system dpkg repository.")]
		public Options AuditDpkg { get; set; }

		[VerbOption("drupal8", HelpText = "Audit a Drupal 8 application instance. Use the -r option to specify the root directory of the Drupal 8 instance, otherwise the current directory will be used.")]
        public Options AuditDrupal8 { get; set; }

        [VerbOption("drupal7", HelpText = "Audit a Drupal 7 application instance. Use the -r option to specify the root directory of the Drupal 7 instance, otherwise the current directory will be used.")]
        public Options AuditDrupal7 { get; set; }

        [VerbOption("mysql", HelpText = "Audit a MySQL application server instance. Use the -r option to specify the root directory of the mysqld server. Use the -b option to specify the path to the mysqld server binary and the -c option to specify the configuration file otherwise default values will be used for these 2 parameters.")]
        public Options MySQL { get; set; }

        [VerbOption("sshd", HelpText = "Audit an OpenSSH sshd-compatibile application server instance. Use the -r option to specify the root directory of the sshd server. Use the -b option to specify the path to the sshd server binary, and the -c option to specify the configuration file otherwise default values will be used for these 2 parameters.")]
        public Options SSHD { get; set; }

        [VerbOption("httpd", HelpText = "Audit an Apache httpd server instance. Use the -r option to specify the root directory of the httpd server. Use the -b option to specify the path to the httpd server binary and the -c option to specify the configuration file otherwise default values will be used for these 2 parameters.")]
        public Options Httpd { get; set; }

        [VerbOption("nginx", HelpText = "Audit an Nginx server instance. Use the -r option to specify the root directory of the httpd server. Use the -b option to specify the path to the httpd server binary and the -c option to specify the configuration file otherwise default values will be used for these 2 parameters.")]
        public Options Nginx { get; set; }

        [VerbOption("netfx", HelpText = "Audit a .NET Framework code project. Use the --root option to specify the root directory of the solution, and the --code-project option to specify the name of the project.")]
        public Options NetFx { get; set; }

        [VerbOption("php", HelpText = "Audit a PHP code project. Use the --root option to specify the root directory of the code project.")]
        public Options Php { get; set; }

        [VerbOption("drupal8-module", HelpText = "Audit a Drupal 8 module project. Use the --root option to specify the root directory of the code project and the --code-project option to specify the Drupal 8 module name.")]
        public Options Drupal8Module { get; set; }

        [Option('d', "enable-debug", Required = false, HelpText = "Enable printing debug messages and other behavior useful for debugging the program.")]
        public bool EnableDebug { get; set; }

        [Option('n', "non-interact", Required = false, HelpText = "Disable any interctive console output (for redirecting console output to other devices.)")]
        public bool NonInteractive { get; set; }
       
        [Option('f', "file", Required = false, HelpText = "For a package source, specifies the file containing packages to be audited. For a code project, specifies the code project file.")]
        public string File { get; set; }

        [Option('s', "host", Required = false, HelpText = "Specifies the remote host that will be audited.")]
        public string RemoteHost { get; set; }

        [Option('u', "user", Required = false, HelpText = "Specifies the user name to login to the remote host.")]
        public string RemoteUser { get; set; }

        [Option('p', "password", Required = false, HelpText = "Specifies that a password will be entered interactively for the user name or as a pass-phrase for the user's private-key authentication file to login to the remote host.")]
        public bool EnterRemotePassword { get; set; }

        [Option("password-text", Required = false, HelpText = "Specifies the password text for the user name or pass-phrase for the user's private-key authentication file to login to the remote host.")]
        public string RemotePasswordText { get; set; }
              
        [Option('k', "key", Required = false, HelpText = "Specifies the private-key file for the user to login to the remote host. Use the -p or --password-text option to specify the pass-phrase for the file if needed." )]
        public string RemoteKey { get; set; }

        [Option('c', "config-file", Required = false, HelpText = "Specifies the configuration file for the application server to be audited.")]
        public string ConfigurationFile { get; set; }

        [Option('r', "root", Required = false, HelpText = "The root directory of the application instance to audit.")]
        public string RootDirectory { get; set; }

        [Option('b', "application-binary", Required = false, HelpText = "The path to the application or server binary.")]
        public string ApplicationBinary { get; set; }

		[Option('i', "docker", Required = false, HelpText = "Run the audit on a Docker container with this name or id.")]
		public string Docker { get; set; }

        [Option("list-packages", Required = false, HelpText = "Only list the local packages that will be audited.")]
        public bool ListPackages { get; set; }

        [Option("list-artifacts", Required = false, HelpText = "Only list the artifacts corresponding to local packages found on OSS Index.")]
        public bool ListArtifacts { get; set; }

        [Option("list-rules", Required = false, HelpText = "Only list the configuration rules found for the application or application server.")]
        public bool ListConfigurationRules { get; set; }

        [Option("list-analyzers", Required = false, HelpText = "Only list the analyzers found for the code project.")]
        public bool ListCodeProjectAnalyzers { get; set; }

        [Option("skip-packages-audit", Required = false, HelpText = "Skip the package audit for applications or application servers.")]
        public bool SkipPackagesAudit { get; set; }

        [Option("only-local-rules", Required = false, HelpText = "Only use the configuration rules for the application or application server listed in YAML rules files.")]
        public bool OnlyLocalRules { get; set; }

        [Option("code-project", Required = false, HelpText = "The name of the code project to audit.")]
        public string CodeProjectName { get; set; }

        #region Cache stuff
        /*
        [Option("cache", Required = false, HelpText = "Cache results from querying OSS Index. Projects and vulnerabilities will be cached by default for 180 minutes or the duration specified by the --cache-ttl parameter.")]
        public bool Cache { get; set; }

        [Option("cache-ttl", Required = false, HelpText = "Cache TTL - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public string CacheTTL { get; set; }

        [Option("cache-dump", Required = false, HelpText = "Cache Dump - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public bool CacheDump { get; set; }
        */
        #endregion

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
            //DevAudit.AuditLibrary.
            //BaseSentenceBuilder b = BaseSentenceBuilder.CreateBuiltIn();
            //HelpText help_text = new HelpText(b, "")
            
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current), true);
        }
    }
}
