using System;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using CL = CommandLine; //Avoid type name conflict with external CommandLine library
using CC = Colorful; //Avoid type name conflict with System Console class

using DevAudit.AuditLibrary;

namespace DevAudit.CommandLine
{
    class Program
    {
        public enum ExitCodes
        {
            SUCCESS = 0,
            INVALID_ARGUMENTS,
            ERROR_CREATING_AUDIT_TARGET,
            NO_PACKAGE_MANAGER,
            ERROR_SCANNING_FOR_PACKAGES,
            ERROR_SEARCHING_OSS_INDEX,
            ERROR_SCANNING_SERVER_VERSION,
            ERROR_SCANNING_SERVER_CONFIGURATION,
            ERROR_EVALUATING_CONFIGURATION_RULES
        }

        static CC.Figlet FigletFont = new CC.Figlet(CC.FigletFont.Load("chunky.flf"));

        static Options ProgramOptions = new Options();

        static PackageSource Source { get; set; }

        static ApplicationServer Server { get; set; }

        static Exception AuditLibraryException { get; set; }

        static Spinner Spinner { get; set; }

        static CC.StyleSheet MessageStyleSheet { get; set; }

        static ConsoleColor OriginalConsoleForeColor;

        static Dictionary<EventMessageType, ConsoleColor> ConsoleMessageColors = new Dictionary<EventMessageType, ConsoleColor>()
        {
            {EventMessageType.ERROR, ConsoleColor.DarkRed },
            {EventMessageType.SUCCESS, ConsoleColor.Cyan },
            {EventMessageType.WARNING, ConsoleColor.DarkYellow },
            { EventMessageType.DEBUG, ConsoleColor.Blue }
        };

        static int Main(string[] args)
        {
            #region Setup console colors
            ConsoleMessageColors.Add(EventMessageType.INFO, Console.ForegroundColor);
            OriginalConsoleForeColor = Console.ForegroundColor;
            #endregion

            #region Handle command line options
            if (!CL.Parser.Default.ParseArguments(args, ProgramOptions))
            {
                return (int)ExitCodes.INVALID_ARGUMENTS;
            }
            Dictionary<string, object> audit_options = new Dictionary<string, object>();

            if (!ProgramOptions.EnableDebug)
            {
                AppDomain.CurrentDomain.UnhandledException += Program_UnhandledException;
            }
   
            if (!string.IsNullOrEmpty(ProgramOptions.RemoteHost))
            {
                if (Uri.CheckHostName(ProgramOptions.RemoteHost) == UriHostNameType.Unknown)
                {
                    PrintErrorMessage("Unknown host name type: {0}.", ProgramOptions.RemoteHost);
                    return (int)ExitCodes.INVALID_ARGUMENTS;
                }
                else
                {
                    audit_options.Add("RemoteHost", ProgramOptions.RemoteHost);
                }

                if (ProgramOptions.WindowsUseOpenSsh)
                {
                    audit_options.Add("WindowsUseOpenSsh", true);
                }
                else if (ProgramOptions.WindowsUsePlink)
                {
                    audit_options.Add("WindowsUsePlink", true);
                }

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
                    else if (!string.IsNullOrEmpty(ProgramOptions.RemoteKey))
                    {
                        if (!File.Exists(ProgramOptions.RemoteKey))
                        {
                            PrintErrorMessage("Error in parameter: Could not find file {0}.", ProgramOptions.RemoteKey);
                            return (int)ExitCodes.INVALID_ARGUMENTS;
                        }
                        else
                        {
                            SecureString p = ReadPassword('*');
                            audit_options.Add("RemoteKey", ProgramOptions.RemoteKey);
                            audit_options.Add("RemoteKeyPassPhrase", p);
                        }
                    }
                    else
                    {
                        audit_options.Add("RemoteUseAgent", true);
                    }
                }
            }

            if (!string.IsNullOrEmpty(ProgramOptions.File))
            {
                if (!File.Exists(ProgramOptions.File))
                {
                    PrintErrorMessage("Error in parameter: Could not find file {0}.", ProgramOptions.File);
                    return (int)ExitCodes.INVALID_ARGUMENTS;
                }
                else
                {
                    audit_options.Add("File", ProgramOptions.File);
                }
            }

            if (ProgramOptions.Cache)
            {
                audit_options.Add("Cache", true);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.CacheTTL))
            {
                audit_options.Add("CacheTTL", ProgramOptions.CacheTTL);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.RootDirectory))
            {
                audit_options.Add("RootDirectory", ProgramOptions.RootDirectory);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.DockerContainerId))
            {
                audit_options.Add("DockerContainerId", ProgramOptions.DockerContainerId);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.ConfigurationFile))
            {
                audit_options.Add("ConfigurationFile", ProgramOptions.ConfigurationFile);
            }

            if (!string.IsNullOrEmpty(ProgramOptions.ApplicationBinary))
            {
                audit_options.Add("ApplicationBinary", ProgramOptions.ApplicationBinary);
            }
            #endregion

            #region Handle command line verbs
            CL.Parser.Default.ParseArguments(args, ProgramOptions, (verb, options) =>
            {
                try
                {
                    if (verb == "nuget")
                    {
                        Source = new NuGetPackageSource(audit_options);
                    }
                    else if (verb == "msi")
                    {
                        Source = new MSIPackageSource(audit_options);
                    }
                    else if (verb == "choco")
                    {
                        Source = new ChocolateyPackageSource(audit_options);
                    }
                    else if (verb == "bower")
                    {
                        Source = new BowerPackageSource(audit_options);
                    }
                    else if (verb == "oneget")
                    {
                        Source = new OneGetPackageSource(audit_options);
                    }
                    else if (verb == "composer")
                    {
                        Source = new ComposerPackageSource(audit_options);
                    }
                    else if (verb == "dpkg")
                    {
                        Source = new DpkgPackageSource(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "drupal8")
                    {
                        Source = new Drupal8Application(audit_options);
                    }
                    else if (verb == "drupal7")
                    {
                        Source = new Drupal7Application(audit_options);
                    }
                    else if (verb == "mysql")
                    {
                        Server = new MySQLServer(audit_options, EnvironmentMessageHandler);
                        Source = Server as PackageSource;
                    }
                    else if (verb == "sshd")
                    {
                        Server = new SSHDServer(audit_options, EnvironmentMessageHandler);
                        Source = Server as PackageSource;
                    }
                    else if (verb == "httpd")
                    {
                        Server = new HttpdServer(audit_options, EnvironmentMessageHandler);
                        Source = Server as PackageSource;
                    }
                    else if (verb == "nginx")
                    {
                        Server = new NginxServer(audit_options, EnvironmentMessageHandler);
                        Source = Server as PackageSource;
                    }
                }
                catch (ArgumentException ae)
                {
                    PrintErrorMessage(ae);
                    return;
                }
                catch (Exception e)
                {
                    AuditLibraryException = e;
                    return;
                }
            });

            if (Source == null && Server == null)
            {
                if (AuditLibraryException == null)
                {
                    Console.WriteLine("No package source or application server specified.");
                    return (int)ExitCodes.INVALID_ARGUMENTS;
                }
                else
                {
                    PrintErrorMessage("Error initialzing audit library for audit target.");
                    PrintErrorMessage(AuditLibraryException);
                    return (int)ExitCodes.ERROR_CREATING_AUDIT_TARGET;
                }
            }
            #endregion

            PrintBanner();
            if (Server != null) //Auditing an application server
            {
                ExitCodes exit;
                AuditServer(out exit);
                if (Server != null)
                {
                    Server.Dispose();
                }
                return (int) exit;
            }
            else
            {
                ExitCodes exit;
                AuditPackageSource(out exit);
                if (Source != null)
                {
                    Source.Dispose();
                }
                return (int)exit;
            }
        }

        #region Static methods
        static void AuditPackageSource(out ExitCodes exit)
        {
            if (ReferenceEquals(Source, null)) throw new ArgumentNullException("Source");
            PrintMessage("Scanning {0} packages...", Source.PackageManagerLabel);
            exit = ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
            StartSpinner();
            try
            {
                Source.PackagesTask.Wait();
            }
            catch (AggregateException ae)
            {
                if (!ReferenceEquals(Spinner, null)) StopSpinner();
                PrintErrorMessage("Error(s) encountered scanning for {0} packages: {1}", Source.PackageManagerLabel, ae.InnerException.Message);
                return;
            }
            finally
            {
                if (!ReferenceEquals(Spinner, null)) StopSpinner();
            }
            PrintMessageLine("Found {0} distinct packages.", Source.Packages.Count());
            if (ProgramOptions.ListPackages)
            {
                int i = 1;
                foreach (OSSIndexQueryObject package in Source.Packages)
                {
                    PrintMessageLine("[{0}/{1}] {2} {3} {4}", i++, Source.Packages.Count(), package.Name,
                        package.Version, package.Vendor);
                }
                exit = ExitCodes.SUCCESS;
                return;
            }
            if (Source.Packages.Count() == 0)
            {
                PrintMessageLine("Nothing to do, exiting package audit.");
                exit = ExitCodes.SUCCESS;
                return;
            }
            else
            {
                PrintMessage("Searching OSS Index for {0} {1} packages...", Source.Packages.Count(), Source.PackageManagerLabel);
            }
            exit = ExitCodes.ERROR_SEARCHING_OSS_INDEX;
            StartSpinner();
            try
            {
                Task.WaitAll(Source.ArtifactsTask.ToArray());
            }
            catch (AggregateException ae)
            {

                if (!ReferenceEquals(Spinner, null)) StopSpinner();
                PrintErrorMessage("Error encountered searching OSS Index for {0} packages: {1}...", Source.PackageManagerLabel, ae.InnerException.Message);
                ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                return;
            }
            finally
            {
                if (!ReferenceEquals(Spinner, null)) StopSpinner();
            }
            PrintMessageLine("Found {0} artifacts, {1} with an OSS Index project id.", Source.Artifacts.Count(), Source.ArtifactsWithProjects.Count);
            if (Source.Artifacts.Count() == 0)
            {
                PrintMessageLine("Nothing to do, exiting package audit.");
                exit = ExitCodes.SUCCESS;
                return;
            }
            if (ProgramOptions.ListArtifacts)
            {
                int i = 1;
                foreach (OSSIndexArtifact artifact in Source.Artifacts)
                {
                    PrintMessage("[{0}/{1}] {2} ({3}) ", i++, Source.Artifacts.Count(), artifact.PackageName,
                        !string.IsNullOrEmpty(artifact.Version) ? artifact.Version : string.Format("No version reported for package version {0}", artifact.Package.Version));
                    if (!string.IsNullOrEmpty(artifact.ProjectId))
                    {
                        PrintMessage(ConsoleColor.Blue, artifact.ProjectId + "\n");
                    }
                    else
                    {
                        PrintMessage(ConsoleColor.DarkRed, "No project id found.\n");
                    }
                }
                exit = ExitCodes.SUCCESS;
                return;
            }
            if (ProgramOptions.CacheDump)
            {
                Console.WriteLine("Dumping cache...");
                Console.WriteLine("\nCurrently Cached Items\n===========");
                int i = 0;
                foreach (var v in Source.ProjectVulnerabilitiesCacheItems)
                {
                    Console.WriteLine("{0} {1} {2}", i++, v.Item1.Id, v.Item1.Name);

                }
                int k = 0;
                Console.WriteLine("\nCache Keys\n==========");
                foreach (var v in Source.ProjectVulnerabilitiesCacheKeys)
                {
                    Console.WriteLine("{0} {1}", k++, v);

                }
                Source.Dispose();
                exit = ExitCodes.SUCCESS;
                return;
            }
            if (Source.ArtifactsWithProjects.Count == 0)
            {
                PrintMessageLine("No found artifacts have associated projects.");
                PrintMessageLine("No vulnerability data for your packages currently exists in OSS Index, exiting.");
                exit = ExitCodes.SUCCESS;
                return;
            }
            if (ProgramOptions.Cache)
            {
                PrintMessageLine("{0} projects have cached values.", Source.CachedArtifacts.Count());
                PrintMessageLine("{0} cached project entries are stale and will be removed from cache.", Source.ProjectVulnerabilitiesExpiredCacheKeys.Count());
            }
            PrintMessageLine("Searching OSS Index for vulnerabilities for {0} projects...", Source.VulnerabilitiesTask.Count());
            int projects_count = Source.ArtifactsWithProjects.Count;
            int projects_processed = 0;
            int projects_successful = 0;
            if (Source.ProjectVulnerabilitiesCacheEnabled)
            {
                foreach (Tuple<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> c in Source.ProjectVulnerabilitiesCacheItems)
                {
                    if (projects_processed++ == 0) Console.WriteLine("\nAudit Results\n=============");
                    OSSIndexProject p = c.Item1;
                    IEnumerable<OSSIndexProjectVulnerability> vulnerabilities = c.Item2;
                    OSSIndexArtifact a = p.Artifact;
                    PrintMessage(ConsoleColor.White, "[{0}/{1}] {2}", projects_processed, projects_count, a.PackageName);
                    PrintMessage(ConsoleColor.DarkCyan, "[CACHED]");
                    PrintMessage(ConsoleColor.White, " {0} ", a.Version);
                    if (vulnerabilities.Count() == 0)
                    {
                        PrintMessageLine(ConsoleColor.DarkGray, "No known vulnerabilities.");
                    }
                    else
                    {
                        List<OSSIndexProjectVulnerability> found_vulnerabilities = new List<OSSIndexProjectVulnerability>(vulnerabilities.Count());
                        foreach (OSSIndexProjectVulnerability vulnerability in vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList())
                        {
                            try
                            {
                                if (vulnerability.Versions.Any(v => !string.IsNullOrEmpty(v) && Source.IsVulnerabilityVersionInPackageVersionRange(v, a.Package.Version)))
                                {
                                    found_vulnerabilities.Add(vulnerability);
                                }
                            }
                            catch (Exception e)
                            {
                                PrintErrorMessage("Error determining vulnerability version range {0} in package version range {1}: {2}.",
                                    vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), a.Package.Version, e.Message);
                            }
                        }
                        //found_vulnerabilities = found_vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList();
                        if (found_vulnerabilities.Count() > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[VULNERABLE]");
                        }
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("{0} distinct ({1} total) known vulnerabilities, ", vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).Count(),
                            vulnerabilities.Count());
                        Console.WriteLine("{0} affecting installed version.", found_vulnerabilities.Count());
                        found_vulnerabilities.ForEach(v =>
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            if (!string.IsNullOrEmpty(v.CVEId)) Console.Write("{0} ", v.CVEId);
                            Console.WriteLine(v.Title);
                            Console.ResetColor();
                            Console.WriteLine(v.Summary);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write("\nAffected versions: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(string.Join(", ", v.Versions.ToArray()));
                        });
                    }
                    Console.ResetColor();
                    projects_successful++;
                }
            }
            while (Source.VulnerabilitiesTask.Count() > 0)
            {
                Task<KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>>>[] tasks = Source.VulnerabilitiesTask.ToArray();
                try
                {
                    int x = Task.WaitAny(tasks);
                    var task = Source.VulnerabilitiesTask.Find(t => t.Id == tasks[x].Id);
                    KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectVulnerability>> vulnerabilities = task.Result;
                    OSSIndexProject p = vulnerabilities.Key;
                    OSSIndexArtifact a = p.Artifact;
                    KeyValuePair<OSSIndexQueryObject, IEnumerable<OSSIndexPackageVulnerability>> package_vulnerabilities
                        = Source.PackageVulnerabilities.Where(pv => pv.Key == p.Package).First();
                    if (projects_processed++ == 0)
                    {
                        PrintMessageLine("\nAudit Results\n=============");
                    }
                    projects_successful++;
                    PrintMessage(ConsoleColor.White, "[{0}/{1}] {2} {3}", projects_processed, projects_count, a.PackageName, string.IsNullOrEmpty(a.Version) ? "" :
                        string.Format("({0}) ", a.Version));
                    if (package_vulnerabilities.Value.Count() == 0 && vulnerabilities.Value.Count() == 0)
                    {
                        PrintMessage(ConsoleColor.DarkGray, "no known vulnerabilities. ");
                        PrintMessage("[{0} {1}]\n", p.Package.Name, p.Package.Version);
                    }
                    else
                    {

                        List<OSSIndexPackageVulnerability> found_package_vulnerabilities = new List<OSSIndexPackageVulnerability>();
                        foreach (OSSIndexPackageVulnerability package_vulnerability in package_vulnerabilities.Value)
                        {

                            try
                            {
                                if (package_vulnerability.Versions.Any(v => !string.IsNullOrEmpty(v) && Source.IsVulnerabilityVersionInPackageVersionRange(v, p.Package.Version)))
                                {
                                    found_package_vulnerabilities.Add(package_vulnerability);
                                }
                            }
                            catch (Exception e)
                            {
                                PrintErrorMessage("Error determining vulnerability version range {0} in package version range {1}: {2}",
                                    package_vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), a.Package.Version, e.Message);
                            }
                        }
                        List<OSSIndexProjectVulnerability> found_vulnerabilities = new List<OSSIndexProjectVulnerability>(vulnerabilities.Value.Count());
                        foreach (OSSIndexProjectVulnerability vulnerability in vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList())
                        {
                            try
                            {
                                if (vulnerability.Versions.Any(v => !string.IsNullOrEmpty(v) && Source.IsVulnerabilityVersionInPackageVersionRange(v, p.Package.Version)))
                                {
                                    found_vulnerabilities.Add(vulnerability);
                                }
                            }
                            catch (Exception e)
                            {
                                PrintErrorMessage("Error determining vulnerability version range ({0}) in project version range ({1}). Message: {2}",
                                    vulnerability.Versions.Aggregate((f, s) => { return f + "," + s; }), a.Package.Version, e.Message);
                            }
                        }
                        //found_vulnerabilities = found_vulnerabilities.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).ToList();
                        if (found_vulnerabilities.Count() > 0 || found_package_vulnerabilities.Count() > 0)
                        {
                            PrintMessageLine(ConsoleColor.Red, "[VULNERABLE]");
                        }
                        PrintMessage(ConsoleColor.Magenta, "{0} known vulnerabilities, ", vulnerabilities.Value.Count() + package_vulnerabilities.Value.Count()); //vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).Count(),
                        PrintMessage("{0} affecting installed version. ", found_vulnerabilities.Count() + found_package_vulnerabilities.Count());
                        PrintMessageLine("[{0} {1}]", p.Package.Name, p.Package.Version);
                        found_package_vulnerabilities.ForEach(v =>
                        {
                            PrintMessageLine(ConsoleColor.Red, "{0} {1}", v.Id.Trim(), v.Title.Trim());
                            PrintMessageLine(v.Summary);
                            PrintMessage(ConsoleColor.Red, "Affected versions: ");

                            PrintMessageLine(ConsoleColor.White, string.Join(", ", v.Versions.ToArray()));
                            PrintMessageLine("");
                        });
                        found_vulnerabilities.ForEach(v =>
                        {
                            PrintMessageLine(ConsoleColor.Red, "{0} {1}", v.CVEId, v.Title);
                            PrintMessageLine(v.Summary);
                            PrintMessage(ConsoleColor.Red, "Affected versions: ");
                            PrintMessageLine(ConsoleColor.White, string.Join(", ", v.Versions.ToArray()));
                            PrintMessageLine("");
                        });
                    }
                    Source.VulnerabilitiesTask.Remove(task);
                    projects_successful++;
                }
                catch (AggregateException ae)
                {
                    if (projects_processed++ == 0)
                    {
                        PrintMessageLine("\nAudit Results\n=============");
                    }
                    var failed_tasks = Source.VulnerabilitiesTask.Where(t => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.Canceled).ToList();
                    foreach (var t in failed_tasks)
                    {
                        if (t.Exception != null && t.Exception.InnerException is OSSIndexHttpException)
                        {
                            OSSIndexHttpException oe = t.Exception.InnerException as OSSIndexHttpException;
                            OSSIndexArtifact artifact = Source.Artifacts.FirstOrDefault(a => a.ProjectId == oe.RequestParameter || a.PackageId == oe.RequestParameter);
                            Console.Write("[{0}/{1}] {2} ", ++projects_processed, projects_count, artifact.PackageName, artifact.Version);
                            PrintMessageLine(ConsoleColor.DarkRed, "{0} HTTP Error searching OSS Index...", artifact.Version);
                            ++projects_processed;
                            HandleOSSIndexHttpException(oe);
                            //ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
                        }
                        else
                        {
                            PrintErrorMessage("Unknown error encountered searching OSS Index for vulnerabilities : {0}",
                                ae.InnerException.Message);
                            ++projects_processed;
                        }
                        Source.VulnerabilitiesTask.Remove(t);
                    }
                }
            }
            Source.Dispose();
            exit = ExitCodes.SUCCESS;
            return;
        }

        static void AuditServer(out ExitCodes exit)
        {
            if (ReferenceEquals(Server, null)) throw new ArgumentNullException("Server");
            PrintMessage("Scanning {0} version, modules, extensions, or plugins...", Server.ServerLabel);
            StartSpinner();
            string version;
            exit = ExitCodes.ERROR_SCANNING_SERVER_VERSION;
            try
            {
                version = Server.GetVersion();
                exit = ExitCodes.ERROR_SCANNING_FOR_PACKAGES;
                Server.ModulesTask.Wait();
                Server.PackagesTask.Wait();
            }
            catch (Exception e)
            {
                PrintErrorMessage(e);
                return;
            }
            finally
            {
                StopSpinner();
            }
            PrintMessageLine("Detected {0} version: {1}.", Server.ServerLabel, Server.Version);
            if (!ProgramOptions.ListRules)
            {
                AuditPackageSource(out exit);
                if (exit != ExitCodes.SUCCESS)
                {
                    PrintMessageLine("Error encountered auditing packages.");
                }
            }
            if (ProgramOptions.ListPackages || ProgramOptions.ListArtifacts)
            {
                return;
            }
            Task t = Server.ConfigurationTask;
            PrintMessage("Scanning {0} configuration...", Server.ServerLabel);
            StartSpinner();
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                PrintErrorMessage(e);
            }
            finally
            {
                StopSpinner();
            }
            if (t.IsFaulted || t.IsCanceled  || Server.XmlConfiguration == null)
            {
                PrintMessageLine("Error encountered scanning server configuration. Exiting");
                return;
            }
            else if (Server.XmlConfiguration.Root.Elements().Count() == 0)
            {
                PrintMessageLine("Got zero top-level nodes from scanning server configuration. Exiting.");
                return;
            }
            PrintMessageLine("Got {0} top-level server configuration node(s).",
                Server.XmlConfiguration.Root.Elements().Count());
            PrintMessage("Scanning {0} configuration rules...", Server.ServerLabel);
            StartSpinner();
            try
            {
                Task.WaitAll(Server.ConfigurationRulesTask.ToArray());
            }
            catch (AggregateException ae)
            {
                StopSpinner();
                PrintErrorMessage("Error encountered searching OSS Index for {0} configuration rules: {1}...", Server.ApplicationLabel, ae.InnerException.Message);
                ae.InnerExceptions.ToList().ForEach(i => HandleOSSIndexHttpException(i));
            }
            finally
            {
                StopSpinner();
            }
            if (Server.ProjectConfigurationRules.Count() == 0)
            {
                exit = ExitCodes.SUCCESS;
                PrintErrorMessage("No configuration rules found for {0} server. Exiting.", Server.ServerLabel);
                return;
            }
            PrintMessageLine("Got {0} configuration rule(s) for {1} server project(s).", Server.ProjectConfigurationRules.Sum(cr => cr.Value.Count()), Server.ProjectConfigurationRules.Keys.Count);
            exit = ExitCodes.ERROR_EVALUATING_CONFIGURATION_RULES;
            int projects_count = Server.ProjectConfigurationRules.Keys.Count;
            int projects_processed = 0;
            foreach (KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> rule in Server.ProjectConfigurationRules)
            {
                Dictionary<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> evals = Server.EvaluateProjectConfigurationRules(rule.Value);
                PrintMessage("[{0}/{1}] Project: ", ++projects_processed, projects_count);
                PrintMessage(ConsoleColor.Blue, "{0}. ", rule.Key.Name);
                int total_project_rules = rule.Value.Count();
                int succeded_project_rules = evals.Count(ev => ev.Value.Item1);
                int processed_project_rules = 0;
                PrintMessage("{0} rule(s). ", total_project_rules);
                if (succeded_project_rules > 0 )
                {
                    PrintMessage(ConsoleColor.Magenta, " {0} rule(s) succeded. ", succeded_project_rules);
                    PrintMessageLine(ConsoleColor.Red, "[VULNERABLE]");
                }
                else
                {
                    PrintMessage(ConsoleColor.DarkGreen, " {0} rule(s) succeded. \n", succeded_project_rules);
                }
                foreach (KeyValuePair<OSSIndexProjectConfigurationRule, Tuple <bool, List<string>, string>> e in evals)
                {
                    ++processed_project_rules;
                    PrintMessage("--[{0}/{1}] Rule Name: {2}. Result: ", processed_project_rules, total_project_rules, e.Key.Title);
                    PrintMessageLine(e.Value.Item1 ? ConsoleColor.Red : ConsoleColor.DarkGreen, "{0}.", e.Value.Item1);
                    if (e.Value.Item1)
                    {
                        PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Summary", e.Key.Summary);
                        if (e.Value.Item2 != null && e.Value.Item2.Count > 0)
                        {
                            PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Results", e.Value.Item2);
                        }
                        PrintProjectConfigurationRuleMultiLineField(ConsoleColor.Magenta, 2, "Resolution", e.Key.Resolution);
                        PrintProjectConfigurationRuleMultiLineField(ConsoleColor.White, 2, "Urls", e.Key.Urls);
                    }
                }
            }
            return;
        }

        static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
        {
            if (Spinner != null)
            {
                Spinner.Stop();
            }
            if (e.MessageType == EventMessageType.DEBUG && !ProgramOptions.EnableDebug)
            {
                return;
            }
            else
            {
                if (ProgramOptions.NonInteractive)
                {
                    PrintMessageLine("{0:HH:mm:ss} [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message);
                }
                else
                {
                    PrintMessageLine(ConsoleMessageColors[e.MessageType], "{0:HH:mm:ss} [{1}] [{2}] {3}", e.DateTime, e.EnvironmentLocation, e.MessageType.ToString(), e.Message);
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
            PrintMessageLine(ConsoleColor.DarkRed, "Exception: {0}", e.Message);
            PrintMessageLine(ConsoleColor.DarkRed, "Stack trace: {0}", e.StackTrace);

            if (e.InnerException != null)
            {
                PrintMessageLine(ConsoleColor.DarkRed, "Inner exception: {0}", e.InnerException.Message);
                PrintMessageLine(ConsoleColor.DarkRed, "Inner stack trace: {0}", e.InnerException.StackTrace);
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
            Color banner_color;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                banner_color = Color.White;
            }
            else
            {
                banner_color = Color.White;
            }

            CC.Console.WriteLine(FigletFont.ToAscii("DevAudit"), banner_color);
            CC.Console.WriteLine("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(), banner_color);
        }

        static void HandleOSSIndexHttpException(Exception e)
        {
            if (e.GetType() == typeof(OSSIndexHttpException))
            {
                OSSIndexHttpException oe = (OSSIndexHttpException) e;
                PrintErrorMessage("HTTP status: {0} {1} \nReason: {2}\nRequest:\n{3}", (int) oe.StatusCode, oe.StatusCode, oe.ReasonPhrase, oe.Request);
            }

        }

        static void StartSpinner()
        {
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
                PrintMessageLine(string.Empty);
            }

        }

        static void UnPauseSpinner()
        {
            if (!ProgramOptions.NonInteractive)
            {
                Spinner.Start();
            }
        }

        static void PauseSpinner()
        {
            if (!ProgramOptions.NonInteractive)
            {
                if (ReferenceEquals(null, Spinner)) throw new ArgumentNullException();
                Spinner.Stop();
            }

        }

        static SecureString ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            string debug = "";
            SecureString pass = new SecureString();

            char c = (char) 0;
            Console.Write("Password: ");
            while ((c = Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (((c == BACKSP) || (c == CTRLBACKSP)) && (pass.Length > 0))
                {
                    //Console.Write("\b \b");
                    pass.RemoveAt(pass.Length - 1);
                    debug.Remove(debug.Length - 1);

                }
                // Don't append * when length is 0 and backspace is selected
                else if (((c == BACKSP) || (c == CTRLBACKSP)) && (pass.Length == 0))
                {
                }

                // Don't append when a filtered char is detected
                else if (FILTERED.Any(f => f == c))
                {
                }

                // Append and write * mask
                else
                {
                    pass.AppendChar(c);
                    debug += c;
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
        }

        #endregion

    }
}
