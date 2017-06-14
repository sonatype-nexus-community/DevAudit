using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using CL = CommandLine; //Avoid type name conflict with external CommandLine library
using CC = Colorful; //Avoid type name conflict with System Console class

using DevAudit.AuditLibrary;

namespace DevAudit.CommandLine
{
    class Program
    {
        static string DevAuditDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        static CC.Figlet FigletFont = null;

        static Options ProgramOptions = new Options();

        static CancellationTokenSource CTS = new CancellationTokenSource();

        static PackageSource Source { get; set; }
        
        static Application Application { get; set; }

        static ApplicationServer Server { get; set; }

        static CodeProject CodeProject { get; set; }

        static Exception AuditLibraryException { get; set; }

        static Spinner Spinner { get; set; }

        static string SpinnerText { get; set; }

        static Stopwatch Stopwatch { get; } = new Stopwatch();

        static object ConsoleLock = new object();

        static CC.StyleSheet MessageStyleSheet { get; set; }

        static ConsoleColor OriginalConsoleForeColor;

        static Dictionary<EventMessageType, ConsoleColor> ConsoleMessageColors = new Dictionary<EventMessageType, ConsoleColor>()
        {
            {EventMessageType.ERROR, ConsoleColor.DarkRed },
            {EventMessageType.SUCCESS, ConsoleColor.Cyan },
            {EventMessageType.WARNING, ConsoleColor.DarkYellow },
            {EventMessageType.DEBUG, ConsoleColor.Blue },
        };

        static int SpinnerCursorLeft;

        static int SpinnerCursorTop;

        static AuditTarget.AuditResult Exit;

        static int Main(string[] args)
        {
            FigletFont = new CC.Figlet(CC.FigletFont.Load(Path.Combine(DevAuditDirectory, "chunky.flf")));

            #region Setup console colors
            ConsoleMessageColors.Add(EventMessageType.INFO, Console.ForegroundColor);
            ConsoleMessageColors.Add(EventMessageType.PROGRESS, Console.ForegroundColor);
            ConsoleMessageColors.Add(EventMessageType.STATUS, Console.ForegroundColor);
            OriginalConsoleForeColor = Console.ForegroundColor;
            #endregion

            #region Handle command line options
            Exit = AuditTarget.AuditResult.INVALID_AUDIT_TARGET_OPTIONS;
            CL.Parser parser = new CL.Parser((s) =>
            {
                s.CaseSensitive = true;
                s.MutuallyExclusive = true;
                s.HelpWriter = Console.Error;
            });
            if (!parser.ParseArguments(args, ProgramOptions))
            {
                return (int) Exit;
            }
            Dictionary<string, object> audit_options = new Dictionary<string, object>();

            #region Enable debug
            if (!ProgramOptions.EnableDebug)
            {
                AppDomain.CurrentDomain.UnhandledException += Program_UnhandledException;
            }
            #endregion

            if (!string.IsNullOrEmpty(ProgramOptions.HttpsProxy))
            {
                Uri https_proxy = null;
                if (Uri.TryCreate(ProgramOptions.HttpsProxy, UriKind.Absolute, out https_proxy))
                {
                    audit_options.Add("HttpsProxy", https_proxy);
                }
                else
                {
                    PrintErrorMessage("Invalid HTTPS proxy Url: {0}.", ProgramOptions.HttpsProxy);
                    return (int)Exit;
                }
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER")))
            {
                audit_options.Add("Dockerized", true);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.Docker))
            {
                audit_options.Add("DockerContainer", ProgramOptions.Docker);
            }
            else if (!string.IsNullOrEmpty(ProgramOptions.RemoteHost) && ProgramOptions.WinRm)
            {
                if (Uri.CheckHostName(ProgramOptions.RemoteHost) == UriHostNameType.IPv4 || Uri.CheckHostName(ProgramOptions.RemoteHost) == UriHostNameType.IPv6)
                {
                    IPAddress address = null;
                    if (IPAddress.TryParse(ProgramOptions.RemoteHost, out address))
                    {
                        audit_options.Add("WinRmRemoteIp", address);
                    }       
                    else
                    {
                        PrintErrorMessage("Invalid IP address: {0}.", ProgramOptions.RemoteHost);
                        return (int)Exit;
                    }
                }
                else if (Uri.CheckHostName(ProgramOptions.RemoteHost) != UriHostNameType.Unknown)
                {
                    audit_options.Add("WinRmRemoteHost", new Uri(ProgramOptions.RemoteHost));
                }
                else
                {
                    PrintErrorMessage("Invalid host name: {0}.", ProgramOptions.RemoteHost);
                    return (int)Exit;
                }

                #region User and password
                if (!string.IsNullOrEmpty(ProgramOptions.RemoteUser))
                {
                    audit_options.Add("RemoteUser", ProgramOptions.RemoteUser);
                    if (ProgramOptions.EnterRemotePassword)
                    {
                        SecureString p = ReadPassword('*');
                        audit_options.Add("RemotePass", p);
                    }
                    else if (!string.IsNullOrEmpty(ProgramOptions.RemotePasswordText))
                    {
                        audit_options.Add("RemotePass", ToSecureString(ProgramOptions.RemotePasswordText));
                    }
                    else
                    {
                        PrintErrorMessage("You must specify the Windows password to authenticate with the remote host.");
                        return (int)Exit;
                    }

                }
                else
                {
                    PrintErrorMessage("You must specify the Windows user to authenticate with the remote host.");
                    return (int)Exit;

                }
                #endregion

            }
            else if (!string.IsNullOrEmpty(ProgramOptions.RemoteHost))
            {
                if (Uri.CheckHostName(ProgramOptions.RemoteHost) == UriHostNameType.Unknown)
                {
                    PrintErrorMessage("Unknown host name type: {0}.", ProgramOptions.RemoteHost);
                    return (int)Exit; 
                }
                else
                {
                    audit_options.Add("RemoteHost", ProgramOptions.RemoteHost);
                    if (ProgramOptions.RemoteSshPort < 0 || ProgramOptions.RemoteSshPort > 65535)
                    {
                        PrintErrorMessage("Invalid port number: {0}.", ProgramOptions.RemoteSshPort);
                        return (int)Exit;
                    }
                    audit_options.Add("RemoteSshPort", ProgramOptions.RemoteSshPort);
                }

                #region User and password or key file
                if (!string.IsNullOrEmpty(ProgramOptions.RemoteUser))
                {
                    audit_options.Add("RemoteUser", ProgramOptions.RemoteUser);

                    if (!string.IsNullOrEmpty(ProgramOptions.RemoteKey))
                    {
                        if (!File.Exists(ProgramOptions.RemoteKey))
                        {
                            PrintErrorMessage("Error in parameter: Could not find file {0}.", ProgramOptions.RemoteKey);
                            return (int)Exit;
                        }                      
                        audit_options.Add("RemoteKeyFile", ProgramOptions.RemoteKey);
                        if (ProgramOptions.EnterRemotePassword)
                        {
                            SecureString p = ReadPassword('*');                            
                            audit_options.Add("RemoteKeyPassPhrase", p);
                        }
                        else if (!string.IsNullOrEmpty(ProgramOptions.RemotePasswordText))
                        {
                            audit_options.Add("RemoteKeyPassPhrase", ToSecureString(ProgramOptions.RemotePasswordText));
                        }
                    }
                    else if (ProgramOptions.EnterRemotePassword)
                    {
                        SecureString p = ReadPassword('*');
                        audit_options.Add("RemotePass", p);
                    }
                    else if (!string.IsNullOrEmpty(ProgramOptions.RemotePasswordText))
                    {
                        audit_options.Add("RemotePass", ToSecureString(ProgramOptions.RemotePasswordText));
                    }                    
                    else
                    {
                        //audit_options.Add("RemoteUseAgent", true);
                        PrintErrorMessage("You must specify either a password or private key file and pass phrase to authenticate with the remote host.");
                        return (int)Exit;
                    }

                }
            }
            #endregion

            #region GitHub
            if (!string.IsNullOrEmpty(ProgramOptions.GitHubToken))
            {
                audit_options.Add("GitHubToken", ProgramOptions.GitHubToken);
            }
            if (!string.IsNullOrEmpty(ProgramOptions.GitHubOptions))
            {
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.GitHubOptions);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the GitHub options {0}.", ProgramOptions.GitHubOptions);
                    return (int)Exit;
                }
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                if (!parsed_options.ContainsKey("Owner"))
                {
                    PrintErrorMessage("You must specify the repository owner as Owner=<owner> in the GitHub options {0}.", ProgramOptions.GitHubOptions);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Name"))
                {
                    PrintErrorMessage("You must specify the repository name as Name=<name> in the GitHub options {0}.", ProgramOptions.GitHubOptions);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Branch"))
                {
                    parsed_options.Add("Branch", "master");
                }
                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey("GitHubRepo" + kvp.Key))
                    {
                        audit_options["GitHubRepo" + kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add("GitHubRepo" + kvp.Key, kvp.Value);
                    }
                }
                if (audit_options.ContainsKey("GitHubRepoReport") && !audit_options.ContainsKey("GitHubToken"))
                {
                    PrintErrorMessage("You must specify a GitHub token using --github-token when using the GitHub reporter.");
                    return (int)Exit;
                }
                else if (audit_options.ContainsKey("GitHubRepoReport"))
                {
                    audit_options.Add("GitHubReportName", audit_options["GitHubRepoName"]);
                    audit_options.Add("GitHubReportOwner", audit_options["GitHubRepoOwner"]);
                }
            }
            if (!string.IsNullOrEmpty(ProgramOptions.GitHubReporter))
            {
                if (!audit_options.ContainsKey("GitHubToken"))
                {
                    PrintErrorMessage("You must specify a GitHub user token with --github-token when using the GitHub reporter.");
                    return (int)Exit;
                }
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.GitHubReporter);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the GitHub reporter options {0}.", ProgramOptions.GitHubReporter);
                    return (int)Exit;
                }
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                if (!parsed_options.ContainsKey("Owner"))
                {
                    PrintErrorMessage("You must specify the repository owner as Owner=<owner> in the GitHub reporter options {0}.", ProgramOptions.GitHubReporter);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Name"))
                {
                    PrintErrorMessage("You must specify the repository name as Name=<name> in the GitHub reporter options {0}.", ProgramOptions.GitHubReporter);
                    return (int)Exit;
                }

                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey("GitHubReport" + kvp.Key))
                    {
                        audit_options["GitHubReport" + kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add("GitHubReport" + kvp.Key, kvp.Value);
                    }
                }
            }
            #endregion

            #region GitLab
            if (!string.IsNullOrEmpty(ProgramOptions.GitLabToken))
            {
                audit_options.Add("GitLabToken", ProgramOptions.GitLabToken);
            }
            if (!string.IsNullOrEmpty(ProgramOptions.GitLabOptions))
            {
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.GitLabOptions);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the GitLab options {0}.", ProgramOptions.GitLabOptions);
                    return (int)Exit;
                }
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                if (!parsed_options.ContainsKey("Url"))
                {
                    PrintErrorMessage("You must specify the GitLab host url as Url=<url> in the GitLab options {0}.", ProgramOptions.GitLabOptions);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Name"))
                {
                    PrintErrorMessage("You must specify the GitLab project namespaced name as Name=<namespace/project_name> in the GitLab options {0}.", ProgramOptions.GitLabOptions);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Branch"))
                {
                    parsed_options.Add("Branch", "master");
                }
                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey("GitLabRepo" + kvp.Key))
                    {
                        audit_options["GitLabRepo" + kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add("GitLabRepo" + kvp.Key, kvp.Value);
                    }
                }
                if (audit_options.ContainsKey("GitLabRepoReport") && !audit_options.ContainsKey("GitLabToken"))
                {
                    PrintErrorMessage("You must specify a GitLab token using --GitLab-token when using the GitLab reporter.");
                    return (int)Exit;
                }
                else if (audit_options.ContainsKey("GitLabRepoReport"))
                {
                    audit_options.Add("GitLabReportUrl", audit_options["GitLabRepoUrl"]);
                    audit_options.Add("GitLabReportName", audit_options["GitLabRepoName"]);
                }
            }
            if (!string.IsNullOrEmpty(ProgramOptions.GitLabReporter))
            {
                if (!audit_options.ContainsKey("GitLabToken"))
                {
                    PrintErrorMessage("You must specify a GitLab user token with --GitLab-token when using the GitLab reporter.");
                    return (int)Exit;
                }
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.GitLabReporter);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the GitLab reporter options {0}.", ProgramOptions.GitLabReporter);
                    return (int)Exit;
                }
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                if (!parsed_options.ContainsKey("Url"))
                {
                    PrintErrorMessage("You must specify the repository owner as Owner=<owner> in the GitLab reporter options {0}.", ProgramOptions.GitLabReporter);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Name"))
                {
                    PrintErrorMessage("You must specify the GitLab project namespaced name as Name=<namespace/project_name> in the GitLab options {0}.", ProgramOptions.GitLabReporter);
                    return (int)Exit;
                }

                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey("GitLabReport" + kvp.Key))
                    {
                        audit_options["GitLabReport" + kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add("GitLabReport" + kvp.Key, kvp.Value);
                    }
                }
            }
            #endregion

            #region BitBucket
            if (!string.IsNullOrEmpty(ProgramOptions.BitBucketKey))
            {
                string[] components = ProgramOptions.BitBucketKey.Split('|');
                if (components.Count() != 2)
                {
                    PrintErrorMessage("There was an error parsing the BitBucket key {0}. The key must be specified in the format <consumer_key>|<secret_key>", ProgramOptions.BitBucketKey);
                    return (int)Exit;
                }
                else
                {
                    audit_options.Add("BitBucketKey", ProgramOptions.BitBucketKey);
                }
            }
            if (!string.IsNullOrEmpty(ProgramOptions.BitBucketReporter))
            {
                if (!audit_options.ContainsKey("BitBucketKey"))
                {
                    PrintErrorMessage("You must specify a BitBucket OAuth consumer key/secret with --bitbucket-key when using the BitBucket reporter.");
                    return (int)Exit;
                }
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.BitBucketReporter);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the BitBucket reporter options {0}.", ProgramOptions.BitBucketReporter);
                    return (int)Exit;
                }
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                if (!parsed_options.ContainsKey("Account"))
                {
                    PrintErrorMessage("You must specify the account as Account=<account> in the BitBucket reporter options {0}.", ProgramOptions.BitBucketReporter);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Name"))
                {
                    PrintErrorMessage("You must specify the BitBucket repository name as Name=<name> in the BitBucket options {0}.", ProgramOptions.BitBucketReporter);
                    return (int)Exit;
                }

                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey("BitBucketReport" + kvp.Key))
                    {
                        audit_options["BitBucketReport" + kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add("BitBucketReport" + kvp.Key, kvp.Value);
                    }
                }
            }
            #endregion

            if (ProgramOptions.SkipPackagesAudit && ProgramOptions.ListPackages)
            {
                PrintErrorMessage("You can't specify both --skip-packages-audit and --list-packages.");
                return (int)Exit;
            }
            if (ProgramOptions.SkipPackagesAudit)
            {
                audit_options.Add("SkipPackagesAudit", ProgramOptions.SkipPackagesAudit);
            }
            if (ProgramOptions.ListPackages)
            {
                audit_options.Add("ListPackages", ProgramOptions.ListPackages);
            }
            if (ProgramOptions.ListArtifacts)
            {
                audit_options.Add("ListArtifacts", ProgramOptions.ListArtifacts);
            }
            if (ProgramOptions.PrintConfiguration)
            {
                audit_options.Add("PrintConfiguration", ProgramOptions.PrintConfiguration);
            }
            
            if (ProgramOptions.ListConfigurationRules)
            {
                audit_options.Add("ListConfigurationRules", ProgramOptions.ListConfigurationRules);
            }
            if (ProgramOptions.ListAnalyzers)
            {
                audit_options.Add("ListAnalyzers", ProgramOptions.ListAnalyzers);
            }
            
            if (ProgramOptions.OnlyLocalRules)
            {
                audit_options.Add("OnlyLocalRules", ProgramOptions.OnlyLocalRules);
            }
            
            if (!string.IsNullOrEmpty(ProgramOptions.File))
            {
                audit_options.Add("File", ProgramOptions.File);
            }

            #region Cache stuff
            /*
            if (ProgramOptions.Cache)
            {
                audit_options.Add("Cache", true);
            }
            
            if (!string.IsNullOrEmpty(ProgramOptions.CacheTTL))
            {
                audit_options.Add("CacheTTL", ProgramOptions.CacheTTL);
            }
            */
            #endregion

            if (!string.IsNullOrEmpty(ProgramOptions.RootDirectory))
            {
                audit_options.Add("RootDirectory", ProgramOptions.RootDirectory);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.ConfigurationFile))
            {
                audit_options.Add("ConfigurationFile", ProgramOptions.ConfigurationFile);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.ApplicationBinary))
            {
                audit_options.Add("ApplicationBinary", ProgramOptions.ApplicationBinary);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.ProjectName))
            {
                audit_options.Add("ProjectName", ProgramOptions.ProjectName);
            }
            if(!string.IsNullOrEmpty(ProgramOptions.AuditOptions))
            {
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.AuditOptions);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the options string {0}.", ProgramOptions.AuditOptions);
                    return (int)Exit;
                }                
                else if (parsed_options.Where(o => o.Key == "_ERROR_").Count() > 0)
                {

                    string error_options = parsed_options.Where(o => o.Key == "_ERROR_").Select(kv => (string)kv.Value).Aggregate((s1, s2) => s1 + Environment.NewLine + s2);
                    PrintErrorMessage("There was an error parsing the following options {0}.", error_options);
                    parsed_options = parsed_options.Where(o => o.Key != "_ERROR_").ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                }
                foreach (KeyValuePair<string, object> kvp in parsed_options)
                {
                    if (audit_options.ContainsKey(kvp.Key))
                    {
                        audit_options[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        audit_options.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            #region Profile
            if (!string.IsNullOrEmpty(ProgramOptions.Profile))
            {
                audit_options.Add("Profile", ProgramOptions.Profile);
            }
            #endregion

            #region Data sources
            if (ProgramOptions.WithOSSI)
            {
                audit_options.Add("WithOSSI", true);
            }
            if (ProgramOptions.WithVulners)
            {
                audit_options.Add("WithVulners", true);
            }
            if (ProgramOptions.WithLibIO)
            {
                audit_options.Add("WithLibIO", true);
            }
            #endregion

            #endregion

            PrintBanner();
            if (!ProgramOptions.NonInteractive) Console.CursorVisible = false;

            #region Handle command line verbs
            Exit = AuditTarget.AuditResult.ERROR_CREATING_AUDIT_TARGET;
            CL.Parser.Default.ParseArguments(args, ProgramOptions, (verb, options) =>
            {
                try
                {
                    Stopwatch.Start();
                    if (verb == "nuget")
                    {
                        Source = new NuGetPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "msi")
                    {
                        Source = new MSIPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "choco")
                    {
                        Source = new ChocolateyPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "bower")
                    {
                        Source = new BowerPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "yarn")
                    {
                        Source = new YarnPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "oneget")
                    {
                        Source = new OneGetPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "composer")
                    {
                        Source = new ComposerPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "dpkg")
                    {
                        Source = new DpkgPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "rpm")
                    {
                        Source = new RpmPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "yum")
                    {
                        Source = new YumPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "drupal8")
                    {
                        Application = new Drupal8Application(audit_options, EnvironmentMessageHandler);
                        Source = Application.PackageSource;
                    }
                    else if (verb == "drupal7")
                    {
                        Application = new Drupal7Application(audit_options, EnvironmentMessageHandler);
                        Source = Application.PackageSource;
                    }
                    else if (verb == "netfx")
                    {
                        Application = new NetFx4Application(audit_options, EnvironmentMessageHandler);
                        Source = Application.PackageSource;

                    }
                    else if (verb == "aspnet")
                    {                        
                        Application = new AspNetApplication(audit_options, EnvironmentMessageHandler);
                        Source = Application.PackageSource; 
                        
                    }
                    else if (verb == "mysql")
                    {
                        Server = new MySQLServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "mariadb")
                    {
                        Server = new MariaDBServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "sshd")
                    {
                        Server = new SSHDServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "httpd")
                    {
                        Server = new HttpdServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "nginx")
                    {
                        Server = new NginxServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "pgsql")
                    {
                        Server = new PostgreSQLServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "iis6")
                    {
                        Server = new IIS6Server(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server.PackageSource;
                    }
                    else if (verb == "netfx-code")
                    {
                        CodeProject = new NetFxCodeProject(audit_options, EnvironmentMessageHandler);
                        Application = CodeProject.Application;                        
                        Source = CodeProject.PackageSource;
                    }
                    else if (verb == "aspnet-code")
                    {
                        CodeProject = new AspNetCodeProject(audit_options, EnvironmentMessageHandler);
                        Application = CodeProject.ApplicationInitialised ? CodeProject.Application : null;
                        Source = CodeProject.PackageSource;
                    }

                    else if (verb == "drupal8-module")
                    {
                        CodeProject = new Drupal8ModuleCodeProject(audit_options, EnvironmentMessageHandler);                       
                    }
                }
                catch (ArgumentException ae)
                {
                    Stopwatch.Stop();
                    AuditLibraryException = ae;
                    return;
                }
                catch (Exception e)
                {
                    Stopwatch.Stop();
                    AuditLibraryException = e;
                    return;
                }
            });

            if (Source == null && Application == null && Server == null && CodeProject == null)
            {
                if (AuditLibraryException == null)
                {
                    if (Stopwatch.IsRunning) Stopwatch.Stop();
                    Console.WriteLine("No audit target specified.");
                    return (int) ExitApplication(Exit);
                }
                else if (AuditLibraryException != null && AuditLibraryException is ArgumentException)
                {
                    ArgumentException arge = AuditLibraryException as ArgumentException;
                    if (arge.ParamName == "package_source_options")
                    {
                        PrintErrorMessage("Error initialzing audit library for package source audit target: {0}.", arge.Message);
                    }
                    else if (arge.ParamName == "application_options")
                    {
                        PrintErrorMessage("Error initialzing audit library for application audit target: {0}.", arge.Message);
                    }
                    else if (arge.ParamName == "server_options")
                    {
                        PrintErrorMessage("Error initialzing audit library for application server audit target: {0}.", arge.Message);
                    }
                    else if (arge.ParamName == "project_options")
                    {
                        PrintErrorMessage("Error initialzing audit library for code project audit target: {0}.", arge.Message);
                    }
                    else
                    {
                        PrintErrorMessage(AuditLibraryException);
                    }
                    return (int)ExitApplication(Exit);
                }
                else
                {
                    PrintErrorMessage(AuditLibraryException);
                    return (int)ExitApplication(Exit);
                }
            }
            #endregion

            #region Print audit results
            if (CodeProject == null && Application == null && Source != null) //Auditing a package source
            {
                AuditTarget.AuditResult ar = Source.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();

                if (ar != AuditTarget.AuditResult.SUCCESS)
                {
                    Exit = ar;
                }
                else
                {
                    PrintPackageSourceAuditResults(ar, out Exit);
                }
                Source.Dispose();
            }

            else if (CodeProject == null && Server == null && Application != null) //Auditing an application
            {
                AuditTarget.AuditResult aar = Application.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                if (aar != AuditTarget.AuditResult.SUCCESS)
                {
                    Exit = aar;
                }
                else
                {
                    if (Source != null)
                    {
                        PrintPackageSourceAuditResults(aar, out Exit);
                    }
                    PrintApplicationAuditResults(aar, out Exit);
                }
                if (Source != null)
                {
                    Source.Dispose();
                }
                Application.Dispose();
            }
            else if (CodeProject == null && Server != null) //Auditing server
            {
                AuditTarget.AuditResult aar = Application.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                if (aar != AuditTarget.AuditResult.SUCCESS)
                {
                    Exit = aar;
                }
                else
                {
                    if (Source != null)
                    {
                        PrintPackageSourceAuditResults(aar, out Exit);
                    }

                    PrintApplicationAuditResults(aar, out Exit);
                }
                if (Source != null)
                {
                    Source.Dispose();
                }
                Server.Dispose();
            }
            else if (CodeProject != null && Server == null) //auditing code project
            {
                AuditTarget.AuditResult cpar = CodeProject.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                if (cpar != AuditTarget.AuditResult.SUCCESS)
                {
                    Exit = cpar;
                }
                else
                {
                    if (Source != null)
                    {
                        PrintPackageSourceAuditResults(cpar, out Exit);
                    }
                    if (Application != null)
                    {
                        PrintApplicationAuditResults(cpar, out Exit);

                    }

                    PrintCodeProjectAuditResults(cpar, out Exit);
                }
                if (Source != null)
                {
                    Source.Dispose();
                }
                if (Application != null)
                {
                    Application.Dispose();
                }
                CodeProject.Dispose();
            }
            #endregion

            return (int) ExitApplication(Exit);
        }

        #region Private methods
        static int ExitApplication(AuditTarget.AuditResult exit)
        {
            if (Spinner != null) StopSpinner();
            if (!ProgramOptions.NonInteractive) Console.CursorVisible = true;
            return (int) exit;
        }

        static void PrintPackageSourceAuditResults(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            exit = ar;
            if (Spinner != null) StopSpinner();
            if (ProgramOptions.ListPackages)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Source.Packages.Count() > 0)
                {
                    int i = 1;
                    foreach (IPackage package in Source.Packages.OrderBy(p => p.Name))
                    {
                        PrintMessageLine("[{0}/{1}] {2} {3} {4}", i++, Source.Packages.Count(), package.Name,
                            package.Version, package.Vendor);
                    }
                    return;
                }
                else if (ar == AuditTarget.AuditResult.SUCCESS && Source.Packages.Count() == 0)
                {
                    PrintMessageLine("No packages found for {0}. ", Source.PackageManagerLabel);
                    return;
                }
                else
                {
                    return;
                }
            }
            else if (ProgramOptions.ListArtifacts)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Source.Artifacts.Count() > 0)
                {
                    int i = 1;
                    foreach (IArtifact artifact in Source.Artifacts.Values)
                    {
                        PrintMessage("[{0}/{1}] {2} ({3}) ", i++, Source.Artifacts.Count(), artifact.PackageName,
                            !string.IsNullOrEmpty(artifact.Version) ? artifact.Version : string.Format("No version reported for package {0}", artifact.PackageName));
                        if (!string.IsNullOrEmpty(artifact.ArtifactId))
                        {
                            PrintMessage(ConsoleColor.Blue, artifact.ArtifactId + "\n");
                        }
                        else
                        {
                            PrintMessage(ConsoleColor.DarkRed, "No project id found.\n");
                        }
                    }
                    return;
                }
                else if (ar == AuditTarget.AuditResult.SUCCESS && Source.Artifacts.Count() == 0)
                {
                    PrintMessageLine("No artifacts found for {0}. ", Source.PackageManagerLabel);
                    return;
                }
                else
                {
                    return;
                }
            }
            else if (ProgramOptions.SkipPackagesAudit || ProgramOptions.ListConfigurationRules || ProgramOptions.PrintConfiguration || ProgramOptions.ListAnalyzers)
            {
                return;
            }
            int total_vulnerabilities = Source.Vulnerabilities.Sum(v => v.Value != null ? v.Value.Count(pv => pv.PackageVersionIsInRange) : 0);
            PrintMessageLine(ConsoleColor.White, "\nPackage Source Audit Results\n============================");
            PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilit{3} found in {1} package source audit. Total time for audit: {2} ms.\n", total_vulnerabilities, Source.PackageManagerLabel, Stopwatch.ElapsedMilliseconds, total_vulnerabilities == 0 || total_vulnerabilities > 1 ? "ies" : "y");
            int packages_count = Source.Vulnerabilities.Count;
            int packages_processed = 0;
            foreach (var pv in Source.Vulnerabilities.OrderByDescending(sv => sv.Value.Count(v => v.PackageVersionIsInRange)))
            {
                IPackage package = pv.Key;
                List<IVulnerability> package_vulnerabilities = pv.Value;
                PrintMessage(ConsoleColor.White, "[{0}/{1}] {2}", ++packages_processed, packages_count, package.Name);
                if (package_vulnerabilities.Count() == 0)
                {
                    PrintMessage(" no known vulnerabilities.");
                }
                else if (package_vulnerabilities.Count(v => v.PackageVersionIsInRange) == 0)
                {
                    PrintMessage(" {0} known vulnerabilit{1}, 0 affecting installed package version(s).", package_vulnerabilities.Count(), package_vulnerabilities.Count() > 1 ? "ies" : "y");
                }
                else
                {
                    PrintMessage(ConsoleColor.Red, " [VULNERABLE] ");
                    PrintMessage(" {0} known vulnerabilities, ", package_vulnerabilities.Count());
                    PrintMessageLine(ConsoleColor.Magenta, " {0} affecting installed package version(s): [{1}]", package_vulnerabilities.Count(v => v.PackageVersionIsInRange), 
                        package_vulnerabilities.Where(v => v.PackageVersionIsInRange).Select(v => v.Package.Version).Distinct().Aggregate((s1, s2) => s1 + "," + s2));
                    var matched_vulnerabilities = package_vulnerabilities.Where(v => v.PackageVersionIsInRange).ToList();
                    int matched_vulnerabilities_count = matched_vulnerabilities.Count;
                    int c = 0;
                    matched_vulnerabilities.ForEach(v =>
                    {
                        PrintMessage(ConsoleColor.White, "--[{0}/{1}] ", ++c, matched_vulnerabilities_count);
                        PrintMessageLine(ConsoleColor.Red, "{0} ", v.Title.Trim());
                        PrintMessageLine(ConsoleColor.White, "  --Description: {0}", v.Description.Trim());
                        PrintMessage(ConsoleColor.White, "  --Affected versions: ");
                        PrintMessageLine(ConsoleColor.Red, "{0}", string.Join(", ", v.Versions.ToArray()));
                    });
                }
                PrintMessageLine("");
            }
            Source.Dispose();
            return;
        }
  
        static void PrintApplicationAuditResults(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            if (Stopwatch.IsRunning) Stopwatch.Stop();
            exit = ar;
            if (Spinner != null) StopSpinner();
            if (ProgramOptions.ListPackages || ProgramOptions.ListArtifacts)
            {
                return;
            }
            else if (ar == AuditTarget.AuditResult.SUCCESS && ProgramOptions.PrintConfiguration)
            {
                if (Application.ConfigurationInitialised)
                {
                    PrintMessageLine(Application.XmlConfiguration.ToString());
                }
                else
                {
                    PrintErrorMessage("Configuration was not initialised.");
                }
                return;
            }
            else if (ProgramOptions.ListConfigurationRules)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Application.ConfigurationRules.Count() > 0)
                {
                    int i = 1;
                    foreach (var project_rule in Application.ConfigurationRules)
                    {
                        if (project_rule.Key == Application.ApplicationId + "_" + "default") continue;
                        PrintMessage("[{0}/{1}]", i, Application.ConfigurationRules.Count);
                        PrintMessageLine(ConsoleColor.Blue, " {0} ", project_rule.Key);
                        int j = 1;
                        foreach (ConfigurationRule rule in project_rule.Value)
                        {
                            PrintMessageLine("  [{0}/{1}] {2}", j++, project_rule.Value.Count(), rule.Title);
                        }
                    }
                    return;
                }
                else if (ar == AuditTarget.AuditResult.SUCCESS && Application.ConfigurationRules.Count() == 0)
                {
                    PrintMessageLine("No configuration rules found for {0}. ", Application.ApplicationLabel);
                    return;
                }
            }
            else if (Application.ConfigurationRules.Count() > 0)
            {
                PrintMessageLine(ConsoleColor.White, "\nApplication Configuration Audit Results\n=======================================");
                if (Application.AppDevMode && Application.DisabledRules.Count > 0)
                {
                    PrintMessageLine("{0} rules disabled for application development mode:", Application.DisabledRules.Count);
                    int rdfadm = 0;
                    foreach(ConfigurationRule rule in Application.DisabledRules)
                    {
                        PrintMessageLine(ConsoleColor.DarkGray, "--[{0}/{1}] {2}", ++rdfadm, Application.DisabledRules.Count, rule.Title);
                    }
                }
                PrintMessageLine(ConsoleColor.White, "{0} {3} found in {1} application configuration audit. Total time for audit: {2} ms.\n", 
                    Application.ConfigurationRulesEvaluations.Values.Where(v => v.Item1).Count(), Application.ApplicationLabel, Stopwatch.ElapsedMilliseconds,
                    Application.ConfigurationRulesEvaluations.Values.Where(v => v.Item1).Count() == 1 ? "vulnerability" : "total vulnerabilities");
                int projects_count = Application.ConfigurationRules.Count, projects_processed = 0;
                foreach (KeyValuePair<string, IEnumerable<ConfigurationRule>> module_rule in Application.ConfigurationRules)
                {
                    IEnumerable<KeyValuePair<ConfigurationRule, Tuple<bool, List<string>, string>>> evals = 
                        Application.ConfigurationRulesEvaluations.Where(cre => cre.Key.ModuleName == module_rule.Key);
                    PrintMessage("[{0}/{1}] Module: ", ++projects_processed, projects_count);
                    PrintMessage(ConsoleColor.Blue, "{0}. ", module_rule.Key);
                    int total_project_rules = module_rule.Value.Count();
                    int succeded_project_rules = evals.Count() > 0 ? evals.Count(ev => ev.Value.Item1) : 0;
                    int processed_project_rules = 0;
                    PrintMessage("{0} rule(s). ", total_project_rules);
                    if (succeded_project_rules > 0)
                    {
                        PrintMessage(ConsoleColor.Magenta, " {0} rule(s) succeeded. ", succeded_project_rules);
                        PrintMessageLine(ConsoleColor.Red, "[VULNERABLE]");
                    }
                    else
                    {
                        PrintMessage(ConsoleColor.DarkGreen, " {0} rule(s) succeeded. \n", succeded_project_rules);
                    }
                    foreach (KeyValuePair<ConfigurationRule, Tuple<bool, List<string>, string>> e in evals)
                    {
                        ++processed_project_rules;
                        bool vulnerable = e.Value.Item1;
                        ConfigurationRule _rule = e.Key;
                        if (!vulnerable)
                        {
                            PrintMessage("--[{0}/{1}] Rule: {2}. Result: ", processed_project_rules, total_project_rules, _rule.Title);
                        }
                        else
                        {
                            PrintMessage(ConsoleColor.White, "--[{0}/{1}] Rule: {2}. Result: ", processed_project_rules, total_project_rules, _rule.Title);
                        }
                        PrintMessageLine(vulnerable ? ConsoleColor.Red : ConsoleColor.DarkGreen, "{0}.", vulnerable);
                        if (vulnerable)
                        {
                            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Summary", _rule.Summary);
                            if (e.Value.Item2 != null && e.Value.Item2.Count > 0)
                            {
                                PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Results", e.Value.Item2);
                            }
                            if (_rule.Tags != null && _rule.Tags.Count > 0)
                            {
                                PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Tags", _rule.Tags);
                            }
                            if (_rule.Severity > 0)
                            {
                                PrintMessage(ConsoleColor.White, "  --Severity: ");
                                ConsoleColor severity_color = _rule.Severity == 1 ? ConsoleColor.DarkYellow : _rule.Severity == 2 ? ConsoleColor.DarkRed : ConsoleColor.Red;
                                PrintMessageLine(severity_color, "{0}", _rule.Severity);
                            }
                            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.Magenta, 2, "Resolution", _rule.Resolution);
                            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Urls", _rule.Urls);
                            PrintMessageLine(string.Empty);
                        }
                    }
                }
            }
            else
            {
                PrintMessageLine(ConsoleColor.White, "\nApplication Configuration Audit Results\n=======================================");
                PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilities found in {1} application configuration audit. Total time for audit: {2} ms.\n", Application.ConfigurationRulesEvaluations.Values.Where(v => v.Item1).Count(), Application.ApplicationLabel, Stopwatch.ElapsedMilliseconds);
            }
            if (Application.AnalyzerResults != null && Application.AnalyzerResults.Count > 0)
            {
                IEnumerable<ByteCodeAnalyzerResult> code_analysis_results = Application.AnalyzerResults;
                if (code_analysis_results != null && code_analysis_results.Count() > 0)
                {
                    int bcar_count = code_analysis_results.Count();
                    int bcar_succeded_count = code_analysis_results.Count(car => car.Succeded);
                    int bcar_vulnerable_count = code_analysis_results.Count(car => car.IsVulnerable);
                    PrintMessageLine(ConsoleColor.White, "\nApplication Code Analysis Results\n=======================================");
                    PrintMessageLine(ConsoleColor.White, "{0} {1} found in {2} application code analysis audit. Total time for audit: {3} ms.\n",
                        bcar_vulnerable_count, bcar_vulnerable_count == 0 || bcar_vulnerable_count > 1 ? "vulnerabilities" : "vulnerability", Application.ApplicationLabel, Stopwatch.ElapsedMilliseconds);
                    int processed_code_analysis_results = 0;
                    foreach (ByteCodeAnalyzerResult bcar in code_analysis_results)
                    {
                        ++processed_code_analysis_results;
                        if (!bcar.Executed)
                        {
                            PrintMessage("--[{0}/{1}] Analyzer: {2}. Result: ", processed_code_analysis_results, bcar_count, bcar.Analyzer.Summary);
                            PrintMessageLine(ConsoleColor.DarkGray, "Not executed");
                            if (bcar.Exceptions != null && bcar.Exceptions.Any())
                            {
                                foreach (Exception e in bcar.Exceptions)
                                {
                                    PrintMessageLine("  --Exception {0} {1}.", e.Message, e.StackTrace);
                                }
                            }
                        }
                        else if (!bcar.Succeded)
                        {
                            PrintMessage("--[{0}/{1}] Analyzer: {2}. Result: ", processed_code_analysis_results, bcar_count, bcar.Analyzer.Summary);
                            PrintMessageLine(ConsoleColor.Yellow, "Failed");
                            if (bcar.Exceptions != null && bcar.Exceptions.Any())
                            {
                                foreach (Exception e in bcar.Exceptions)
                                {
                                    PrintMessageLine("  --Exception {0} {1}.", e.Message, e.StackTrace);
                                }
                            }
                        }
                        else
                        {
                            PrintMessage("--[{0}/{1}] Analyzer: {2}. Result: ", processed_code_analysis_results, bcar_count, bcar.Analyzer.Summary);
                            PrintMessageLine(bcar.IsVulnerable ? ConsoleColor.Red : ConsoleColor.DarkGreen, "{0}.", bcar.IsVulnerable);
                            PrintMessageLine("  --Description: {0}", bcar.Analyzer.Description);
                            PrintMessageLine("  --Module: {0}", bcar.ModuleName);
                            PrintMessageLine("  --Location: {0}", bcar.LocationDescription);
                            if (bcar.DiagnosticMessages != null && bcar.DiagnosticMessages.Any())
                            {
                                if (bcar.DiagnosticMessages != null && bcar.DiagnosticMessages.Any())
                                {
                                    PrintMessageLine("  --Diagnostics");
                                    int dmc_total = bcar.DiagnosticMessages.Count, dmc_count = 0;
                                    foreach (string diagnostics in bcar.DiagnosticMessages)
                                    {
                                        List<string> messages = diagnostics.Split("\n".ToCharArray()).ToList();
                                        string name = messages.Where(d => d.StartsWith("Name:")).FirstOrDefault();
                                        string severity = messages.Where(d => d.StartsWith("Severity:")).FirstOrDefault();
                                        PrintMessage("    --[{0}/{1}] {2} Severity: ", ++dmc_count, dmc_total, name);
                                        ConsoleColor severity_color = severity.Contains("Low") ? ConsoleColor.DarkYellow : severity.Contains("Medium") ? ConsoleColor.DarkRed : ConsoleColor.Red;
                                        PrintMessageLine(severity_color, "{0}", severity.Replace("Severity: ", string.Empty));
                                        messages.RemoveAll(m => m == name || m == severity);
                                        foreach (string m in messages)
                                        {
                                            PrintMessageLine("      --{0}", m);
                                        }
                                    }
                                }
                            }
                        }
                        
                    }
                }
            }
        }

        static void PrintCodeProjectAuditResults(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            if (Stopwatch.IsRunning) Stopwatch.Stop();
            exit = ar;
            if (Spinner != null) StopSpinner();
            PrintMessageLine(ConsoleColor.White, "\nCode Project Audit Results\n============================");
        }

        static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            lock (ConsoleLock)
            {

                if (e.MessageType == EventMessageType.DEBUG && !ProgramOptions.EnableDebug)
                {
                    return;
                }
                if (ProgramOptions.NonInteractive)
                {
                    PrintMessageLine("{0:HH:mm:ss}<{4,2:##}> [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message, e.CurrentThread.ManagedThreadId.ToString("D2"));
                }
                else if (e.MessageType == EventMessageType.STATUS)
                {
                    if (Spinner != null)
                    {
                        PauseSpinner();

                        SpinnerText = e.Message + "..";
                    }
                    else
                    {
                        PrintMessageLine(ConsoleMessageColors[e.MessageType], "{0:HH:mm:ss}<{4,2:##}> [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message, e.CurrentThread.ManagedThreadId.ToString("D2"));
                        SpinnerText = e.Message + "..";
                        StartSpinner();
                    }
                }
                else if (e.MessageType == EventMessageType.PROGRESS)
                {
                    if (Spinner != null)
                    {
                        PauseSpinner(false);
                        //PrintMessage(GetProgressOutput(e.Progress.Value));
                    }
                    else
                    {
                        PrintMessageLine(ConsoleMessageColors[e.MessageType], "{0:HH:mm:ss}<{4,2:##}> [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message, e.CurrentThread.ManagedThreadId.ToString("D2"));
                    }
                }
                else
                {
                    if (Spinner != null)
                    {
                        PauseSpinner();
                    }
                    PrintMessageLine(ConsoleMessageColors[e.MessageType], "{0:HH:mm:ss}<{4,2:##}> [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message, e.CurrentThread.ManagedThreadId.ToString("D2"));
                    if (e.MessageType == EventMessageType.ERROR && e.Exception != null)
                    {
                        PrintErrorMessage(e.Exception);
                    }
                }

                if (e.Caller.HasValue && ProgramOptions.EnableDebug)
                {
                    if (ProgramOptions.NonInteractive)
                    {
                        PrintMessageLine("Caller: {0}\nLine: {1}\nFile: {2}", e.Caller.Value.Name, e.Caller.Value.LineNumber, e.Caller.Value.File);
                    }
                    else
                    {
                        PrintMessageLine(ConsoleMessageColors[e.MessageType], "Caller: {0}\nLine: {1}\nFile: {2}", e.Caller.Value.Name,
                            e.Caller.Value.LineNumber, e.Caller.Value.File);
                    }

                }
                if (!ProgramOptions.NonInteractive && Spinner != null)
                {
                    if (e.MessageType == EventMessageType.SUCCESS)
                    {
                        SpinnerText = string.Empty;
                    }
                    else
                    {
                        UnPauseSpinner();
                    }
                }
            }
        }

        static void PrintMessage(string format, params object[] args)
        {
            Console.Write(format, args);
        }

        static void PrintMessage(ConsoleColor color, string format, params object[] args)
        {
            if (!ProgramOptions.NonInteractive)
            {
                ConsoleColor o = Console.ForegroundColor;
                Console.ForegroundColor = color;
                PrintMessage(format, args);
                Console.ForegroundColor = o;
            }
            else
            {
                Console.Write(format, args);
            }
        }

        static void PrintMessageLine(string format)
        {
            Console.WriteLine(format);
        }

        static void PrintMessageLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        static void PrintMessageLine(ConsoleColor color, string format, params object[] args)
        {
            if (!ProgramOptions.NonInteractive)
            {
                ConsoleColor o = Console.ForegroundColor;
                Console.ForegroundColor = color;
                PrintMessageLine(format, args);
                Console.ForegroundColor = o;
            }
            else
            {
               PrintMessageLine(format, args);
            }
        }

        static void PrintErrorMessage(string format, params object[] args)
        {
            PrintMessageLine(ConsoleColor.DarkRed, format, args);
        }

        static void PrintErrorMessage(Exception e)
        {
            if (e is OSSIndexHttpException)
            {
                HandleOSSIndexHttpException(e as OSSIndexHttpException);
            }
            else
            {
                PrintMessageLine(ConsoleColor.DarkRed, "Exception: {0}", e.Message);
                PrintMessageLine(ConsoleColor.DarkRed, "Stack trace: {0}", e.StackTrace);
                if (e.InnerException != null)
                {
                    PrintErrorMessage(e.InnerException);
                }
            }
        }

        static void PrintProjectConfigurationRuleMultiLineField(int indent, string field, string value)
        {
            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, indent, field, value);
        }

        static void PrintProjectConfigurationRuleMultiLineField(ConsoleColor color, int indent, string field, string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            sb.Append(' ', indent);
            sb.Append("--");
            sb.Append(field);
            sb.AppendLine(":");
            string[] lines = value.Split(Environment.NewLine.ToCharArray());
            foreach (string l in lines.TakeWhile(s => !string.IsNullOrEmpty(s)))
            {
                sb.Append(' ', indent * 2);
                sb.Append("--");
                sb.AppendLine(l);
            }
            PrintMessage(color, sb.ToString());
        }

        static void PrintProjectConfigurationRuleMultiLineField(ConsoleColor color, int indent, string field, List<string> values)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indent);
            sb.Append("--");
            sb.Append(field);
            sb.AppendLine(":");
            foreach (string l in values.TakeWhile(s => !string.IsNullOrEmpty(s)))
            {
                sb.Append(' ', indent * 2);
                sb.Append("--");
                sb.AppendLine(l);
            }
            PrintMessage(color, sb.ToString());
        }

        static void PrintProjectConfigurationRuleMultiLineField(int indent, string field, List<string> values)
        {
            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, indent, field, values);
        }

        static void PrintBanner()
        {
            if (ProgramOptions.NonInteractive) return;
            /*
            Color banner_color = Color.Green;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                banner_color = Color.White;
            }
            else
            {
                banner_color = Color.White;
            }
            */
            Console.ForegroundColor = ConsoleColor.Cyan;
            CC.Console.WriteLine(FigletFont.ToAscii("DevAudit"));
            CC.Console.WriteLine("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.ForegroundColor = OriginalConsoleForeColor;
        }

        static string GetProgressOutput(OperationProgress progress)
        {
            string percent_done = (progress.Complete / progress.Total).ToString("00.0%");
            return percent_done;
        }

        static void HandleOSSIndexHttpException(Exception e)
        {
            if (e is OSSIndexHttpException)
            {
                OSSIndexHttpException oe = (OSSIndexHttpException) e;
                PrintErrorMessage("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}", (int) oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request);
            }

        }

        static void StartSpinner()
        {
            if (!string.IsNullOrEmpty(SpinnerText))
            {
                PrintMessage(ConsoleMessageColors[EventMessageType.INFO], SpinnerText);
            }
            if (!ProgramOptions.NonInteractive)
            {
                Spinner = new Spinner(50);
                Spinner.Start();
            }
        }

        static void StopSpinner()
        {
            if (!ProgramOptions.NonInteractive)
            {
                if (ReferenceEquals(null, Spinner)) throw new ArgumentNullException();
                Spinner.Stop();
                Spinner = null;
                SpinnerText = string.Empty;
            }
        }

        static void UnPauseSpinner()
        {
            if (!ProgramOptions.NonInteractive)
            {
                if (ReferenceEquals(null, Spinner)) throw new ArgumentNullException();
                Console.SetCursorPosition(0, Console.CursorTop);
                PrintMessage(ConsoleMessageColors[EventMessageType.INFO], SpinnerText);
                Spinner.UnPause();
            }
            else
            {
                PrintMessage(ConsoleMessageColors[EventMessageType.INFO], SpinnerText);
            }
        }

        static void PauseSpinner(bool break_line = true)
        {
            if (!ProgramOptions.NonInteractive)
            {
                if (ReferenceEquals(null, Spinner)) throw new ArgumentNullException();
                SpinnerCursorLeft = Console.CursorLeft;
                SpinnerCursorTop = Console.CursorTop;
                Spinner.Pause(break_line);
            }

        }

        static SecureString ReadPassword(char mask)
        {
            int[] Filtered_Chars = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const
            //string debug = "";
            SecureString pass = new SecureString();
            ConsoleKeyInfo cki;
            Console.Write("Password: ");
            while ((cki = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if ((cki.Key == ConsoleKey.Backspace) && (pass.Length > 0))
                {
                    //Console.Write("\b \b");
                    pass.RemoveAt(pass.Length - 1);
                    //debug.Remove(debug.Length - 1);

                }
                // Don't append * when length is 0 and backspace is selected
                else if ((cki.Key == ConsoleKey.Backspace) && (pass.Length == 0))
                {
                    continue;
                }

                // Don't append when a filtered char is detected
                else if (Filtered_Chars.Any(f => f == cki.KeyChar))
                {
                    continue;
                }

                // Append and write * mask
                else
                {
                    pass.AppendChar(cki.KeyChar);
                    //debug += cki.KeyChar;
                    //Console.Write(mask);
                }
            }
            Console.Write(Environment.NewLine);
            return pass;
        }

        public static SecureString ToSecureString(string s)
        {
            SecureString r = new SecureString();
            foreach (char c in s)
            {
                r.AppendChar(c);
            }
            r.MakeReadOnly();
            return r;
        }


        static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            PrintErrorMessage("Runtime error! {0}", e.IsTerminating ? "DevAudit will now terminate." : "");
            if (Console.CursorVisible == false) Console.CursorVisible = true;
        }

        #endregion

    }
}
