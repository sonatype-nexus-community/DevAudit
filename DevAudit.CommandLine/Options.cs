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

        [VerbOption("nuget", HelpText = "Audit NuGetv2 packages installed for .NET Framework libraries or applications. Use the --file option to specify a particular packages.config file otherwise the one in the current directory will be used.")]
        public Options AuditNuGet { get; set; }      

        [VerbOption("choco", HelpText = "Audit Chocolatey packages on Windows. Packages are scanned from C:\\ProgramData\\chocolatey.")]
        public Options AuditChocolatey { get; set; }

        [VerbOption("bower", HelpText = "Audit Bower packages. Use the --file option to specify a particular bower.json file otherwise the one in the current directory will be used.")]
        public Options AuditBower { get; set; }

        [VerbOption("yarn", HelpText = "Audit Yarn or NPM packages. Use the --file option to specify a particular package.json file otherwise the one in the current directory will be used.")]
        public Options AuditYarn { get; set; }

        [VerbOption("oneget", HelpText = "Audit OneGet packages on Windows. Packages are scanned from the system OneGet repository.")]
        public Options AuditOneGet { get; set; }

        [VerbOption("composer", HelpText = "Audit PHP Composer packages. Use the --file option to specify a particular composer.json file otherwise the one in the current directory will be used.")]
        public Options AuditComposer { get; set; }
         
        [VerbOption("rpm", HelpText = "Audit rpm packages on Linux. The packages are scanned from the system rpm repository.")]
        public Options AuditRpm { get; set; }

        [VerbOption("yum", HelpText = "Audit yum packages on Linux. The packages are scanned from the system rpm repository.")]
        public Options AuditYum { get; set; }

        [VerbOption("netcore", HelpText = "Audit a .NET Core application or .NET Standard library's dependencies. Use the -f option to specify the path to the .csproj project file or the .deps.json dependencies manifest file.")]
        public Options NetCore { get; set; }

        [Option('d', "enable-debug", Required = false, HelpText = "Enable printing debug messages and other behavior useful for debugging the program.")]
        public bool EnableDebug { get; set; }

        [Option('n', "non-interactive", Required = false, HelpText = "Disable any interctive console output (for redirecting console output to other devices.)")]
        public bool NonInteractive { get; set; }

        [Option('o', "options", Required = false, HelpText = "Specify a set of comma delimited, key=value options for an audit target. E.g for a mvc5-app audit target you can specify -o package_source=mypackages.config,config_file=myapp.config")]
        public string AuditOptions { get; set; }

        [Option("no-cache", Required = false, HelpText = "Don't use file cache of vulnerabilities data.")]
        public bool NoCache { get; set; }

        [Option("delete-cache", Required = false, HelpText = "Delete file cache of vulnerabilities data.")]
        public bool DeleteCache { get; set; }

        [Option('f', "file", Required = false, HelpText = "For a package source specifies the package manifest or file containing packages to be audited.")]
        public string File { get; set; }

        [Option('l', "lock-file", Required = false, HelpText = "For a package source specifies the lock file containing packages to be audited. For a code project, specifies the code project file.")]
        public string LockFile { get; set; }

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

        [Option('r', "root", Required = false, HelpText = "The root directory of the application instance to audit.")]
        public string RootDirectory { get; set; }

        [Option('i', "container-id", Required = false, HelpText = "Connect to a Docker container with this name or id.")]
        public string DockerContainerId { get; set; }

        [Option('g', "github", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitHub audit environment. You can specify 3 options: Owner=<owner>,Name=<repo>,Branch=<branch> for the GitHub repository owner, name and branch respectively. Omitting the Branch value will specify the master branch by default.")]
        public string GitHubOptions { get; set; }

        [Option("github-token", Required = false, HelpText = "Specify a GitHub personal access token for authenticating with GitHub.")]
        public string GitHubToken { get; set; }

        [Option("github-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitHub audit reporter. You can specify 3 options: Owner=<owner>,Name=<repo>,Title=<title> for the repository GitHub owner, name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string GitHubReporter { get; set; }

        [Option("gitlab", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitLab audit environment. You can specify 3 options: Url=<url>,Project=<project>,Branch=<branch> for the GitLab host url, project name and branch respectively. Omitting the Branch value will specify the master branch by default.")]
        public string GitLabOptions { get; set; }

        [Option("gitlab-token", Required = false, HelpText = "Specify a GitLab OAuth token for authenticating with GitLab.")]
        public string GitLabToken { get; set; }

        [Option("gitlab-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the GitLab audit reporter. You can specify 3 options: Url=<url>,Name=<repo>,Title=<title> for the GitLab host url, project name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string GitLabReporter { get; set; }

        [Option("bitbucket-key", Required = false, HelpText = "Specify the BitBucket OAuth2 token or key to use for authentication with BitBucket. For a OAuth consumer key/secret pair you can use the format key|secret.")]
        public string BitBucketKey { get; set; }

        [Option("bitbucket-report", Required = false, HelpText = "Specify a set of comma delimited, key=value options for the BitBucket audit reporter. You can specify 3 options: Account=<account>,Name=<repo>,Title=<title> for the BitBucket5th account, repository name and issue title respectively. Omitting the Title value will result in the default issue title being used.")]
        public string BitBucketReporter { get; set; }

        //[Option('m', "project-name", Required = false, HelpText = "The name of the code project to audit.")]
        public string ProjectName { get; set; }

        [Option("list-packages", Required = false, HelpText = "Only list the local packages that will be audited.", MutuallyExclusiveSet ="audit-action")]
        public bool ListPackages { get; set; }

        [Option("https-proxy", Required = false, HelpText = "Use the specified Url as the proxy for HTTPS calls made to the OSS Index API.")]
        public string HttpsProxy { get; set; }

        [Option("output-file", Required = false, HelpText = "Path to the output file.")]
        public string OutputFile { get; set; }
        
        [Option("ignore-https-cert-errors", Required = false, HelpText = "Ignore certain certificate errors for HTTPS requests. This is useful for testing but is extremely insecure and should never be used in production.")]
        public bool IgnoreHttpsCertErrors { get; set; }

        [Option("profile", Required = false, HelpText = "Use the specified file as the audit profile for this audit run.")]
        public string Profile { get; set; }

        [Option("ci", Required = false, HelpText = "Run in 'continuous integration' mode. Returns non-zero exist when vulnerabilities found.")]
        public bool CiMode { get; set; }

        [Option("with-vulners", Required = false, HelpText = "Use vulnerability data from the vulners.com API and/or data files.")]
        public bool WithVulners { get; set; }

        public static Dictionary<string, object> Parse(string o)
        {
            Dictionary<string, object> audit_options = new Dictionary<string, object>();
            Regex re = new Regex(@"(\w+)\=([^\,]+)", RegexOptions.Compiled);
            string [] pairs = o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in pairs)
            {
                Match m = re.Match(s);
                if (!m.Success)
                {                    
                    audit_options.Add("_ERROR_", s);
                }
                else if (audit_options.ContainsKey(m.Groups[1].Value))
                {
                    audit_options[m.Groups[1].Value] = m.Groups[2].Value;
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
