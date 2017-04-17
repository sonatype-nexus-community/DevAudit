using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        [VerbOption("msi", HelpText = "Audit MSI packages on Windows. Packages are scanned from the registry.")]
        public Options AuditMsi { get; set; }

        [VerbOption("choco", HelpText = "Audit Chocolatey packages on Windows. Packages are scanned from C:\\ProgramData\\chocolatey.")]
        public Options AuditChocolatey { get; set; }

        [VerbOption("bower", HelpText = "Audit Bower packages. Use the --file option to specify a particular bower.json file otherwise the one in the current directory will be used.")]
        public Options AuditBower { get; set; }

        [VerbOption("oneget", HelpText = "Audit OneGet packages on Windows. Packages are scanned from the system OneGet repository.")]
        public Options AuditOneGet { get; set; }

        [VerbOption("composer", HelpText = "Audit PHP Composer packages. Use the --file option to specify a particular composer.json file otherwise the one in the current directory will be used.")]
        public Options AuditComposer { get; set; }

        /* Disabled -- insufficient server support
         * [VerbOption("dpkg", HelpText = "Audit dpkg packages on Linux. The packages are scanned from the system dpkg repository.")]
         * public Options AuditDpkg { get; set; }
         */

        /* Disabled -- insufficient server support
         * [VerbOption("rpm", HelpText = "Audit rpm packages on Linux. The packages are scanned from the system rpm repository.")]
         * public Options AuditRpm { get; set; }
         */

        /* Disabled -- insufficient server support
         * [VerbOption("yum", HelpText = "Audit yum packages on Linux. The packages are scanned from the system rpm repository.")]
         * public Options AuditYum { get; set; }
         */
        
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

        [VerbOption("nginx", HelpText = "Audit a Nginx server instance. Use the -r option to specify the root directory of the server. Use the -b option to specify the path to the httpd server binary and the -c option to specify the configuration file otherwise default values will be used for these 2 parameters.")]
        public Options Nginx { get; set; }

        [VerbOption("pgsql", HelpText = "Audit a PostgreSQL server instance. Use the -r option to specify the root directory of the httpd server. The server binary and configuration files will be autodetected; to override use the -b option to specify the path to the postgres server binary and the -c option to specify the configuration file.")]
        public Options PGSQL { get; set; }

        [VerbOption("netfx", HelpText = "Audit a .NET Framework application. Use the --root option to specify the root directory of the application and the -b option to specify the application .NET assembly.")]
        public Options NetFx { get; set; }

        /* Disabled -- code analysis targets, incomplete
         * [VerbOption("netfx-code", HelpText = "Audit a .NET Framework 4 code project. Use the --root option to specify the root directory of the solution, and the --project-name option to specify the name of the project.")]
         * public Options NetFxCode { get; set; }
         */

        /* Disabled -- code analysis targets, incomplete
         * [VerbOption("aspnet-code", HelpText = "Audit an ASP.NET code project. Use the --root option to specify the root directory of the solution, and the --project-name option to specify the name of the project.")]
         * public Options AspNetCode { get; set; }
         */

        [VerbOption("aspnet", HelpText = "Audit an ASP.NET application or code project deployed to a web server. Use the --root option to specify the root directory of the application and the -b option to specify the application .NET assembly.")]
        public Options AspNet { get; set; }

        /* Disabled -- code analysis targets, incomplete
         * [VerbOption("php", HelpText = "Audit a PHP code project. Use the --root option to specify the root directory of the code project.")]
         * public Options Php { get; set; }
         */

        /* Disabled -- code analysis targets, incomplete
         *[VerbOption("drupal8-module", HelpText = "Audit a Drupal 8 module project. Use the --root option to specify the root directory of the code project and the --code-project option to specify the Drupal 8 module name.")]
         *public Options Drupal8Module { get; set; }
         */
        
        [Option('d', "enable-debug", Required = false, HelpText = "Enable printing debug messages and other behavior useful for debugging the program.")]
        public bool EnableDebug { get; set; }

        [Option('n', "non-interactive", Required = false, HelpText = "Disable any interctive console output (for redirecting console output to other devices.)")]
        public bool NonInteractive { get; set; }

        [Option('o', "options", Required = false, HelpText = "Specify a set of comma delimited, key=value options for an audit target. E.g for a mvc5-app audit target you can specify -o package_source=mypackages.config,config_file=myapp.config")]
        public string AuditOptions { get; set; }

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

        [Option("ssh-port", Required = false, HelpText = "Specifies the SSH port to connect to on the remote host.", DefaultValue = 22)]
        public int RemoteSshPort { get; set; }

        [Option('w', "winrm", Required = false, HelpText = "Connect to the remote host using the WinRM protocol. You must enable WinRM on the remote Windows machine.")]
        public bool WinRm { get; set; }

        [Option('c', "config-file", Required = false, HelpText = "Specifies the configuration file for the application server to be audited.")]
        public string ConfigurationFile { get; set; }

        [Option('r', "root", Required = false, HelpText = "The root directory of the application instance to audit.")]
        public string RootDirectory { get; set; }

        [Option('b', "application-binary", Required = false, HelpText = "The path to the application or server binary.")]
        public string ApplicationBinary { get; set; }

        [Option('i', "docker", Required = false, HelpText = "Run the audit on a Docker container with this name or id.")]
        public string Docker { get; set; }

        [Option('g', "github", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitHub audit environment. You can specify 3 options: Owner=<owner>,Name=<repo>,Branch=<branch> for the GitHub repository owner, name and branch respectively. Omitting the Branch value will specify the master branch by default.")]
        public string GitHubOptions { get; set; }

        [Option("github-token", Required = false, HelpText = "Specify a GitHub personal access token for authenticating with GitHub.")]
        public string GitHubToken { get; set; }

        [Option("github-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitHub audit reporter. You can specify 3 options: Owner=<owner>,Name=<repo>,Title=<title> for the repository GitHub owner, name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string GitHubReporter { get; set; }

        [Option('l', "gitlab", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitLab audit environment. You can specify 3 options: Url=<url>,Project=<project>,Branch=<branch> for the GitLab host url, project name and branch respectively. Omitting the Branch value will specify the master branch by default.")]
        public string GitLabOptions { get; set; }

        [Option("gitlab-token", Required = false, HelpText = "Specify a GitLab OAuth token for authenticating with GitLab.")]
        public string GitLabToken { get; set; }

        [Option("gitlab-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitLab audit reporter. You can specify 3 options: Url=<url>,Name=<repo>,Title=<title> for the GitLab host url, project name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string GitLabReporter { get; set; }

        [Option("bitbucket-key", Required = false, HelpText = "Specify the BitBucket OAuth2 token or key to use for authentication with BitBucket. For a OAuth consumer key/secret pair you can use the format key|secret.")]
        public string BitBucketKey { get; set; }

        [Option("bitbucket-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the BitBucket audit reporter. You can specify 3 options: Account=<account>,Name=<repo>,Title=<title> for the BitBucket5th account, repository name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string BitBucketReporter { get; set; }

        [Option('m', "project-name", Required = false, HelpText = "The name of the code project to audit.")]
        public string ProjectName { get; set; }

        [Option("list-packages", Required = false, HelpText = "Only list the local packages that will be audited.", MutuallyExclusiveSet ="audit-action")]
        public bool ListPackages { get; set; }

        [Option("list-artifacts", Required = false, HelpText = "Only list the artifacts corresponding to local packages found on OSS Index.", MutuallyExclusiveSet = "audit-action")]
        public bool ListArtifacts { get; set; }

        [Option("package-info", Required = false, HelpText = "Retrieve and print detailed package info for each package scanned from the package source.")]
        public bool PackageInfo { get; set; }
        //[Option("list-rules", Required = false, HelpText = "Only list the configuration rules found for the application or application server.", MutuallyExclusiveSet = "audit-action")]
        public bool ListConfigurationRules { get; set; }

        //[Option("list-analyzers", Required = false, HelpText = "Only list the analyzers found for the application or code project.", MutuallyExclusiveSet = "audit-action")]
        public bool ListAnalyzers { get; set; }
        
        [Option("print-configuration", Required = false, HelpText = "Only print the application or application server configuration.", MutuallyExclusiveSet = "audit-action")]
        public bool PrintConfiguration { get; set; }

        [Option("skip-packages-audit", Required = false, HelpText = "Skip the package audit for the application or application server.")]
        public bool SkipPackagesAudit { get; set; }

        [Option("only-local-rules", Required = false, HelpText = "Only use the configuration rules for the application or application server listed in YAML rules files.")]
        public bool OnlyLocalRules { get; set; }

        [Option("https-proxy", Required = false, HelpText = "Use the specified Url as the proxy for HTTPS calls made to the OSS Index API.")]
        public string HttpsProxy { get; set; }

        public static Dictionary<string, object> Parse(string o)
        {
            Dictionary<string, object> audit_options = new Dictionary<string, object>();
            Regex re = new Regex(@"(\w+)\=([A-Za-z0-9_\-\\\/\.\:\+@]+)", RegexOptions.Compiled);
            string [] pairs = o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in pairs)
            {
                Match m = re.Match(s);
                if (!m.Success)
                {                    
                    audit_options.Add("_ERROR_", s);
                }
                else
                {
                    audit_options.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
            }
            return audit_options;
        }

 
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
