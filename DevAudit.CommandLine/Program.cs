using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

using CL = CommandLine; //Avoid type name conflict with external CommandLine library
using CC = Colorful; //Avoid type name conflict with System Console class

using DevAudit.AuditLibrary;
using DevAudit.AuditLibrary.Serializers;

namespace DevAudit.CommandLine
{
    class Program
    {
        static string DevAuditDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        static CC.Figlet FigletFont = null;

        static Options ProgramOptions = new Options();

        static CancellationTokenSource CTS = new CancellationTokenSource();

        static PackageSource Source { get; set; }
        
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
            #region Setup font and console colors
            FigletFont = new CC.Figlet(CC.FigletFont.Load(Path.Combine(DevAuditDirectory, "chunky.flf")));

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

            #region Https proxy
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
            #endregion

            #region File cache
            if (ProgramOptions.NoCache)
            {
                audit_options.Add("NoCache", true);
            }

            if (ProgramOptions.DeleteCache)
            {
                audit_options.Add("DeleteCache", true);
            }
            #endregion

            #region Docker container host environment
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER")))
            {
                audit_options.Add("Dockerized", true);
            }
            #endregion

            #region Local Docker container audit environment
            if (!string.IsNullOrEmpty(ProgramOptions.DockerContainerId) && string.IsNullOrEmpty(ProgramOptions.RemoteHost))
            {
                audit_options.Add("DockerContainer", ProgramOptions.DockerContainerId);
            }
            #endregion

            #region Remote Docker container audit environment
            else if (!string.IsNullOrEmpty(ProgramOptions.DockerContainerId) && !string.IsNullOrEmpty(ProgramOptions.RemoteHost))
            {
                audit_options.Add("DockerContainer", ProgramOptions.DockerContainerId);
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
                #endregion
            }
            #endregion

            #region WinRM audit environment
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

                #region Remote user and password
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
            #endregion

            #region SSH audit environment
            else if (!string.IsNullOrEmpty(ProgramOptions.RemoteHost) && string.IsNullOrEmpty(ProgramOptions.DockerContainerId))
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

                #region Remote user and password or key file
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
                #endregion
            }
            #endregion

            #region GitHub audit environment
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

            #region GitLab audit environment
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

            #region BitBucket audit environment
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
            #endregion
            
            #region Package source options
            if (ProgramOptions.ListPackages)
            {
                audit_options.Add("ListPackages", ProgramOptions.ListPackages);
            }
            
            if (!string.IsNullOrEmpty(ProgramOptions.File))
            {
                audit_options.Add("File", ProgramOptions.File);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.LockFile))
            {
                audit_options.Add("LockFile", ProgramOptions.LockFile);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.RootDirectory))
            {
                audit_options.Add("RootDirectory", ProgramOptions.RootDirectory);
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
            #endregion

            #region Audit profile
            if (!string.IsNullOrEmpty(ProgramOptions.Profile))
            {
                audit_options.Add("Profile", ProgramOptions.Profile);
            }

            #endregion

            #region Data sources
            if (ProgramOptions.WithVulners)
            {
                audit_options.Add("WithVulners", true);
            }
            if (ProgramOptions.IgnoreHttpsCertErrors)
            {
                audit_options.Add("IgnoreHttpsCertErrors", true);
            }
            if (!string.IsNullOrEmpty(ProgramOptions.ApiUser))
            {
                audit_options.Add("ApiUser", ProgramOptions.ApiUser);
            }
            if (!string.IsNullOrEmpty(ProgramOptions.ApiToken))
            {
                audit_options.Add("ApiToken", ProgramOptions.ApiToken);
            }
            #endregion

            #region Reporters

            #region BitBucket
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

            #region IQ Server
            if (!string.IsNullOrEmpty(ProgramOptions.IQServerReporter))
            {
                Dictionary<string, object> parsed_options = Options.Parse(ProgramOptions.IQServerReporter);
                if (parsed_options.Count == 0)
                {
                    PrintErrorMessage("There was an error parsing the IQ Server reporter options {0}.", ProgramOptions.IQServerReporter);
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
                    PrintErrorMessage("You must specify the IQ Server Url  as Url=<url> in the IQ Server reporter options {0}.", ProgramOptions.IQServerReporter);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("User"))
                {
                    PrintErrorMessage("You must specify the IQ Server user name as User=<user> in the IQ Server reporter options {0}.", ProgramOptions.IQServerReporter);
                    return (int)Exit;
                }
                if (!parsed_options.ContainsKey("Pass"))
                {
                    PrintErrorMessage("You must specify the IQ Server user password as Pass=<pass> in the IQ Server reporter options {0}.", ProgramOptions.IQServerReporter);
                    return (int)Exit;
                }
                if (Uri.TryCreate((string) parsed_options["Url"], UriKind.Absolute, out Uri uri))
                {
                    audit_options.Add("IQServerUrl", uri);
                    audit_options.Add("IQServerUser", parsed_options["User"]);
                    audit_options.Add("IQServerPass", parsed_options["Pass"]);
                }
                else
                {
                    PrintErrorMessage("The IQ Server Url specified is not valid: {0}.", parsed_options["Url"]);
                    return (int)Exit;
                }
           


            }
            #endregion

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
                        Source = new NuGetv2PackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "netcore")
                    {
                        Source = new NetCorePackageSource(audit_options, EnvironmentMessageHandler);
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
                    else if (verb == "msi")
                    {
                        Source = new MSIPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "rpm")
                    {
                        Source = new RpmPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "yum")
                    {
                        Source = new YumPackageSource(audit_options, EnvironmentMessageHandler);
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

            if (Source == null )
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
            int vulnCount = 0;
            AuditTarget.AuditResult ar = Source.Audit(CTS.Token);
            if (Stopwatch.IsRunning) Stopwatch.Stop();

            if (ar != AuditTarget.AuditResult.SUCCESS)
            {
                Exit = ar;
            }
            else
            {
                vulnCount += PrintPackageSourceAuditResults(ar, out Exit);
            }
            Source.Dispose();
            #endregion

            if (Exit == AuditTarget.AuditResult.SUCCESS && ProgramOptions.CiMode)
            {
                // Is execution is succesfull and we are in CI mode then exit with the number of problems found
                return (int)ExitApplication(vulnCount);
            }
            else
            {
                return (int)ExitApplication(Exit);
            }
        }

        #region Private methods
        static int ExitApplication(AuditTarget.AuditResult exit)
        {
            // Make error codes negative, since positive numbers can be made to indicate the number of vulnerabilities identified
            return ExitApplication(-(int)exit);
        }

        static int ExitApplication(int exit)
        {
            if (Spinner != null) StopSpinner();
            if (!ProgramOptions.NonInteractive) Console.CursorVisible = true;
            return (int)exit;
        }

        static int PrintPackageSourceAuditResults(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
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
                    return 0;
                }
                else if (ar == AuditTarget.AuditResult.SUCCESS && Source.Packages.Count() == 0)
                {
                    PrintMessageLine("No packages found for {0}. ", Source.PackageManagerLabel);
                    return 0;
                }
                else
                {
                    return 0;
                }
            }

            if (!string.IsNullOrEmpty(ProgramOptions.OutputFile))
            {
                File.WriteAllText(ProgramOptions.OutputFile, JsonConvert.SerializeObject(Source));
            }
            if (!string.IsNullOrEmpty(ProgramOptions.XmlOutputFile))
            {
                JUnitXmlSerializer jxs = new JUnitXmlSerializer(Source, Stopwatch.Elapsed.TotalSeconds.ToString(), ProgramOptions.XmlOutputFile);
            }

            int total_vulnerabilities = Source.Vulnerabilities.Sum(v => v.Value != null ? v.Value.Count(pv => pv.PackageVersionIsInRange) : 0);
            PrintMessageLine(ConsoleColor.White, "\nPackage Source Audit Results\n============================");
            PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilit{3} found in {1} package source audit. Total time for audit: {2} ms.\n", total_vulnerabilities, Source.PackageManagerLabel, Stopwatch.ElapsedMilliseconds, total_vulnerabilities == 0 || total_vulnerabilities > 1 ? "ies" : "y");
            int packages_count = Source.Vulnerabilities.Count;
            int packages_processed = 0;
            List<DataSourceInfo> vuln_ds = new List<DataSourceInfo>();
            foreach (var pv in Source.Vulnerabilities.OrderByDescending(sv => sv.Value.Count(v => v.PackageVersionIsInRange)))
            {
                IPackage package = pv.Key;
                List<IVulnerability> package_vulnerabilities = pv.Value;
                PrintMessage(ConsoleColor.White, "[{0}/{1}] {2} {3}", ++packages_processed, packages_count, package.Name, package.Version);
                if (package_vulnerabilities.Count() == 0)
                {
                    PrintMessageLine(" no known vulnerabilities.");
                }
                else if (package_vulnerabilities.Count(v => v.PackageVersionIsInRange) == 0)
                {
                    PrintMessageLine(" {0} known vulnerabilit{1}, 0 affecting installed package version(s).", package_vulnerabilities.Count(), package_vulnerabilities.Count() > 1 ? "ies" : "y");
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
                        PrintAuditResultMultiLineField(ConsoleColor.White, 2, "Description", v.Description.Trim().Replace("\n", "").Replace(". ", "." + Environment.NewLine));
                        PrintMessageLine(ConsoleColor.Red, "{0}", string.Join(", ", v.Versions.ToArray()));
                        if (v.CVE != null && v.CVE.Count() > 0)
                        {
                            PrintMessageLine(ConsoleColor.White, "  --CVE(s): {0}", string.Join(", ", v.CVE.ToArray()));
                        }
                        if (!string.IsNullOrEmpty(v.Reporter))
                        {
                            PrintMessageLine(ConsoleColor.White, "  --Reporter: {0} ", v.Reporter.Trim());
                        }
                        if (!string.IsNullOrEmpty(v.CVSS.Score))
                        {
                            PrintMessageLine("  --CVSS Score: {0}. Vector: {1}", v.CVSS.Score, v.CVSS.Vector);
                        }
                        if (v.Published != DateTime.MinValue)
                        {
                            PrintMessageLine("  --Date published: {0}", v.Published.ToShortDateString());
                        }
                        if (!string.IsNullOrEmpty(v.Id))
                        {
                            PrintMessageLine("  --Id: {0}", v.Id);
                        }
                        if (v.References != null && v.References.Count() > 0)
                        {
                            if (v.References.Count() == 1)
                            {
                                PrintMessageLine("  --Reference: {0}", v.References[0]);
                            }
                            else
                            {
                                PrintMessageLine("  --References:");
                                for (int i = 0; i < v.References.Count(); i++)
                                {
                                    PrintMessageLine("    - {0}", v.References[i]);
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(v.DataSource.Name))
                        {
                            PrintMessageLine("  --Provided by: {0}", v.DataSource.Name);
                        }
                    });
                    PrintMessageLine("");
                    string[] dsn = matched_vulnerabilities.Select(v => v.DataSource.Name).Distinct().ToArray();
                    foreach (string d in dsn)
                    {
                        if (!vuln_ds.Any(ds => ds.Name == d))
                        {
                            vuln_ds.Add(Source.DataSources.Single(ds => ds.Info.Name == d).Info);
                        }
                    }
                }
            }
            PrintMessageLine("");
            if (vuln_ds.Count > 0)
            {
                PrintMessageLine("Vulnerabilities Data Providers\n==============================");
                foreach (DataSourceInfo dsi in vuln_ds)
                {
                    PrintMessageLine("");
                    PrintMessage(ConsoleColor.White, "{0} ", dsi.Name);
                    PrintMessage(ConsoleColor.Green, "{0} ", dsi.Url);
                    PrintMessageLine(dsi.Description);
                }
            }
            Source.Dispose();
            return total_vulnerabilities;
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
            if (args.Length == 0)
            {
                Console.Write(format);
            }
            else
            {
                Console.Write(format, args);
            }
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
                PrintMessage(format, args);
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
            if (e is HttpException)
            {
                HandleHttpException(e as HttpException);
            }
            else
            {
                PrintMessage(ConsoleColor.DarkRed, "{0}", e.Message);
                if (e.InnerException != null)
                {
                    PrintMessageLine(ConsoleColor.DarkRed, " Inner Exception: {0}", e.InnerException.Message);
                }
                else PrintMessageLine("");
                if (ProgramOptions.EnableDebug)
                {
                    PrintMessageLine(ConsoleColor.DarkRed, "Stack trace: {0}", e.StackTrace);
 
                }
            }
        }

        static void PrintAuditResultMultiLineField(int indent, string field, string value)
        {
            PrintAuditResultMultiLineField(ConsoleColor.White, indent, field, value);
        }

        static void PrintAuditResultMultiLineField(ConsoleColor color, int indent, string field, string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            sb.Append(' ', indent);
            sb.Append("--");
            sb.Append(field);
            sb.AppendLine(":");
            string[] lines = value.Split(Environment.NewLine.ToCharArray());
            string last = null;
            foreach (string l in lines)
            {
                string line = l.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                sb.Append(' ', indent * 2);
                if (string.IsNullOrEmpty(last))
                {
                    sb.Append("--");
                }
                else
                {
                    sb.Append("  ");
                }
                sb.AppendLine(line);
                last = line;
            }
            PrintMessage(color, sb.ToString());
        }

        static void PrintAuditResultMultiLineField(ConsoleColor color, int indent, string field, List<string> values)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indent);
            sb.Append("--");
            sb.Append(field);
            sb.AppendLine(":");
            string last = null;
            foreach (string l in values)
            {
                string line = l.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                sb.Append(' ', indent * 2);
                if (string.IsNullOrEmpty(last))
                {
                    sb.Append("--");
                }
                else
                {
                    sb.Append("  ");
                }
                sb.AppendLine(line);
                last = line;
            }
            PrintMessage(color, sb.ToString());
        }

        static void PrintAuditResultMultiLineField(int indent, string field, List<string> values)
        {
            PrintAuditResultMultiLineField(ConsoleColor.White, indent, field, values);
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

        static void HandleHttpException(Exception e)
        {
            /*
            if (e is HttpException)
            {
                HttpException oe = (HttpException) e;
                PrintErrorMessage("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}", (int) oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request);
            }*/
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
