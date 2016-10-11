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

		[VerbOption("drupal8", HelpText = "Audit a Drupal 8 application instance. Use the --root option to specify the root directory of the Drupal 8 instance, otherwise the current directory will be used.")]
        public Options AuditDrupal8 { get; set; }

        [VerbOption("drupal7", HelpText = "Audit a Drupal 7 application instance. Use the --root option to specify the root directory of the Drupal 7 instance, otherwise the current directory will be used.")]
        public Options AuditDrupal7 { get; set; }

        [VerbOption("mysql", HelpText = "Audit a MySQL application server instance. Use the --root option to specify the root directory of the MySQL server instance, and the --config-file option to specify the configuration file otherwise my.ini will be used.")]
        public Options MySQL { get; set; }

        [VerbOption("sshd", HelpText = "Audit a OpenSSH SSHD-compatibile application server instance. Use the --root option to specify the root directory of the SSHD server instance, and the --config-file option to specify the configuration file otherwise /etc/sshd/sshd_config will be used.")]
        public Options SSHD { get; set; }

        [VerbOption("httpd", HelpText = "Audit an Apache Httpd server instance. Use the --root option to specify the root directory of the Httpd server instance, and the --config-file option to specify the configuration file otherwise conf/httpd.conf will be used.")]
        public Options Httpd { get; set; }

        [VerbOption("nginx", HelpText = "Audit an Nginx server instance. Use the --root option to specify the root directory of the Httpd server instance, and the --config-file option to specify the configuration file otherwise conf/httpd.conf will be used.")]
        public Options Nginx { get; set; }

        [VerbOption("netfx", HelpText = "Audit a .NET Framework code project. Use the --root option to specify the root directory of the project or solution, and the -f option to specify the solution or project file to use..")]
        public Options NetFx { get; set; }

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

        [Option('p', "password", Required = false, HelpText = "Specifies that a password will be typed for the user name to login to the remote host.")]
        public bool EnterRemotePassword { get; set; }

        [Option("password-text", Required = false, HelpText = "Specifies that a password will be typed for the user name to login to the remote host.")]
        public string RemotePasswordText { get; set; }

        [Option("connect-timeout", Required = false, HelpText = "Specifies the timeout for network connection. Default")]
        public int NetworkConnectTimeout { get; set; }

        [Option('a', "use-agent", Required = false, HelpText = "Attempt to use an agent to retrieve the private key to login to the remote host.")]
        public bool UseAgent { get; set; }

        [Option('k', "key", Required = false, HelpText = "Specifies the private key file for the user to login to the remote host.")]
        public string RemoteKey { get; set; }

        [Option('c', "config-file", Required = false, HelpText = "Specifies the configuration file for the application server to be audited.")]
        public string ConfigurationFile { get; set; }

        [Option('r', "root", Required = false, HelpText = "The root directory of the application instance to audit.")]
        public string RootDirectory { get; set; }

        [Option("use-openssh", Required = false, HelpText = "On Windows use the OpenSSH SSH client bundled with DevAudit.")]
        public bool WindowsUseOpenSsh { get; set; }

        [Option("use-plink", Required = false, HelpText = "On Windows use the Plink SSH client bundled with DevAudit.")]
        public bool WindowsUsePlink { get; set; }

        [Option("docker-container", Required = false, HelpText = "Run the audit on a docker container with this id. The command line docker tools are used to execute the required commands on the docker container.")]
        public string DockerContainerId { get; set; }

        [Option('b', "application-binary", Required = false, HelpText = "The path to the application or server binary.")]
        public string ApplicationBinary { get; set; }

        [Option("list-packages", Required = false, HelpText = "Only list the local packages that will be audited.")]
        public bool ListPackages { get; set; }

        [Option("list-artifacts", Required = false, HelpText = "Only list the artifacts corresponding to local packages found on OSS Index.")]
        public bool ListArtifacts { get; set; }

        [Option("skip-package-audit", Required = false, HelpText = "Skip the package audit for applications or application servers.")]
        public bool SkipPackageAudit { get; set; }

        [Option("list-rules", Required = false, HelpText = "Only list the configuration rules for the application or application server found on OSS Index.")]
        public bool ListRules { get; set; }

        [Option("code-project", Required = false, HelpText = "The name of the code project to audit.")]
        public string CodeProjectName { get; set; }

        [Option("cache", Required = false, HelpText = "Cache results from querying OSS Index. Projects and vulnerabilities will be cached by default for 180 minutes or the duration specified by the --cache-ttl parameter.")]
        public bool Cache { get; set; }

        [Option("cache-ttl", Required = false, HelpText = "Cache TTL - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public string CacheTTL { get; set; }

        [Option("cache-dump", Required = false, HelpText = "Cache Dump - projects and vulnearabilities will be cached for the number of minutes specified here.")]
        public bool CacheDump { get; set; }

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
