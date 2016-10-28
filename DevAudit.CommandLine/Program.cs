using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        static CC.Figlet FigletFont = new CC.Figlet(CC.FigletFont.Load("chunky.flf"));

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
            #region Setup console colors
            ConsoleMessageColors.Add(EventMessageType.INFO, Console.ForegroundColor);
            ConsoleMessageColors.Add(EventMessageType.PROGRESS, Console.ForegroundColor);
            ConsoleMessageColors.Add(EventMessageType.STATUS, Console.ForegroundColor);
            OriginalConsoleForeColor = Console.ForegroundColor;
            #endregion

            #region Handle command line options
            Exit = AuditTarget.AuditResult.INVALID_AUDIT_TARGET_OPTIONS;
            if (!CL.Parser.Default.ParseArguments(args, ProgramOptions))
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

            if (!string.IsNullOrEmpty(ProgramOptions.RemoteHost))
            {
                if (Uri.CheckHostName(ProgramOptions.RemoteHost) == UriHostNameType.Unknown)
                {
                    PrintErrorMessage("Unknown host name type: {0}.", ProgramOptions.RemoteHost);
                    return (int)Exit; 
                }
                else
                {
                    audit_options.Add("RemoteHost", ProgramOptions.RemoteHost);
                }
                #region Ssh client
                if (ProgramOptions.WindowsUseOpenSsh && Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    audit_options.Add("WindowsUseOpenSsh", true);
                }
                else if (ProgramOptions.WindowsUsePlink && Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    audit_options.Add("WindowsUsePlink", true);
                }
                #endregion

                #region User and password or key file
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
                            return (int) Exit;
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
            #endregion

            if (ProgramOptions.UseApiv2)
            {
                audit_options.Add("UseApiv2", ProgramOptions.UseApiv2);
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
            if (ProgramOptions.ListConfigurationRules)
            {
                audit_options.Add("ListConfigurationRules", ProgramOptions.ListConfigurationRules);
            }
            if (ProgramOptions.OnlyLocalRules)
            {
                audit_options.Add("OnlyLocalRules", ProgramOptions.OnlyLocalRules);
            }
            if (ProgramOptions.ListCodeProjectAnalyzers)
            {
                audit_options.Add("ListCodeProjectAnalyzers", ProgramOptions.ListCodeProjectAnalyzers);
            }
            if (!string.IsNullOrEmpty(ProgramOptions.File))
            {
                audit_options.Add("File", ProgramOptions.File);
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

            if (!string.IsNullOrEmpty(ProgramOptions.CodeProjectName))
            {
                audit_options.Add("CodeProjectName", ProgramOptions.CodeProjectName);
            }
            #endregion

            PrintBanner();

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
                    else if (verb == "drupal8")
                    {
                        Application = new Drupal8Application(audit_options, EnvironmentMessageHandler);
                        Source = Application as PackageSource;
                    }
                    else if (verb == "drupal7")
                    {
                        Application = new Drupal7Application(audit_options, EnvironmentMessageHandler);
                    }
                    else if (verb == "mysql")
                    {
                        Server = new MySQLServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                    }
                    else if (verb == "sshd")
                    {
                        Server = new SSHDServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                        Source = Server as PackageSource;
                    }
                    else if (verb == "httpd")
                    {
                        Server = new HttpdServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                    }
                    else if (verb == "nginx")
                    {
                        Server = new NginxServer(audit_options, EnvironmentMessageHandler);
                        Application = Server as Application;
                    }
                    else if (verb == "netfx")
                    {
                        CodeProject = new NetFxCodeProject(audit_options, EnvironmentMessageHandler);
                        Application = CodeProject as Application;
                        Source = CodeProject as PackageSource;
                    }
                    else if (verb == "php")
                    {
                        CodeProject = new  PHPCodeProject(audit_options, EnvironmentMessageHandler);
                        Application = CodeProject as Application;
                        Source = CodeProject as PackageSource;
                    }
                    else if (verb == "drupal8-module")
                    {
                        CodeProject = new Drupal8ModuleCodeProject(audit_options, EnvironmentMessageHandler);
                        Application = CodeProject as Application;
                        Source = CodeProject as PackageSource;
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
                    return (int) Exit;
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
                    return (int) Exit;
                }
                else
                {
                    PrintErrorMessage(AuditLibraryException);
                    return (int)Exit;
                }
            }
            #endregion

            if (!ProgramOptions.NonInteractive) Console.CursorVisible = false;
            if (Application == null && Source != null) //Auditing a package source
            {
                AuditTarget.AuditResult ar = Source.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                AuditPackageSource(ar, out Exit);
                if (Source != null)
                {
                    Source.Dispose();
                }
            }

            else if (CodeProject == null && Server == null && Application != null) //Auditing an application
            {
                AuditTarget.AuditResult aar = Application.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                AuditPackageSourcev1(aar, out Exit);
                AuditApplication(aar, out Exit);
                if (Application != null)
                {
                    //Server.Dispose();
                }
            }
            else if (CodeProject == null && Server != null) //Auditing server
            {
                AuditTarget.AuditResult aar = Application.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                AuditPackageSourcev1(aar, out Exit);
                AuditApplication(aar, out Exit);
                if (Application != null)
                {
                    //Server.Dispose();
                }
            }
            else if (CodeProject != null)
            {
                AuditTarget.AuditResult cpar = CodeProject.Audit(CTS.Token);
                if (Stopwatch.IsRunning) Stopwatch.Stop();
                AuditCodeProject(cpar, out Exit);
                if (CodeProject != null)
                {
                    CodeProject.Dispose();
                }
            }
            if (!ProgramOptions.NonInteractive) Console.CursorVisible = true;
            return (int) Exit;

        }

        #region Private methods
        static void AuditPackageSource(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            exit = ar;
            if (Spinner != null) StopSpinner();
            if (ProgramOptions.ListPackages)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Source.Packages.Count() > 0)
                {
                    int i = 1;
                    foreach (OSSIndexQueryObject package in Source.Packages)
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
            if (ProgramOptions.SkipPackagesAudit || ProgramOptions.ListConfigurationRules)
            {
                return;
            }

            #region Cache stuff
            /*
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
                        PrintMessageLine("No known vulnerabilities.");
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
            }*/
            #endregion

            PrintMessageLine(ConsoleColor.White, "\nPackage Source Audit Results\n============================");
            PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilities found in {1} package source audit. Total time for audit: {2} ms.\n", Source.Vulnerabilities.Sum(v => v.Value != null ? v.Value.Count(pv => pv.CurrentPackageVersionIsInRange) : 0), Source.PackageManagerLabel, Stopwatch.ElapsedMilliseconds);
            int packages_count = Source.Vulnerabilities.Count;
            int packages_processed = 0;
            foreach (var pv in Source.Vulnerabilities)
            {
                OSSIndexQueryObject package = pv.Key;
                List<OSSIndexApiv2Vulnerability> package_vulnerabilities = pv.Value;
                PrintMessage(ConsoleColor.White, "[{0}/{1}] {2} {3}", ++packages_processed, packages_count, package.Name, package.Version);
                if (package_vulnerabilities.Count() == 0)
                {
                    PrintMessage(" no known vulnerabilities.");
                }
                else if (package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange) == 0)
                {
                    PrintMessage(" {0} known vulnerabilities, 0 affecting current package version.", package_vulnerabilities.Count());
                }
                else
                {
                    PrintMessage(ConsoleColor.Red, " [VULNERABLE] ");
                    PrintMessage(" {0} known vulnerabilities, ", package_vulnerabilities.Count());
                    PrintMessageLine(ConsoleColor.Magenta, " {0} affecting installed version. ", package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange));
                    var matched_vulnerabilities = package_vulnerabilities.Where(v => v.CurrentPackageVersionIsInRange).ToList();
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

        static void AuditPackageSourcev1(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            if (Stopwatch.IsRunning) Stopwatch.Stop();
            exit = ar;
            if (Spinner != null) StopSpinner();
            if (ProgramOptions.ListPackages)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Source.Packages.Count() > 0)
                {
                    int i = 1;
                    foreach (OSSIndexQueryObject package in Source.Packages)
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
            if (ProgramOptions.SkipPackagesAudit || ProgramOptions.ListConfigurationRules)
            {
                return;
            }

            #region Cache stuff
            /*
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
                        PrintMessageLine("No known vulnerabilities.");
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
            }*/
            #endregion

            PrintMessageLine(ConsoleColor.White, "\nPackage Source Audit Results\n============================");
            int projects_count = Source.ProjectVulnerabilities.Count;
            int projects_processed = 0;
            foreach (var pv in Source.ProjectVulnerabilities)
            {
                OSSIndexProject p = pv.Key;
                IEnumerable<OSSIndexProjectVulnerability> project_vulnerabilities = pv.Value;
                IEnumerable<OSSIndexPackageVulnerability> package_vulnerabilities = Source.PackageVulnerabilities.Where(package_vuln => package_vuln.Key.Name == p.Package.Name).First().Value;
                PrintMessage(ConsoleColor.White, "[{0}/{1}] {2} {3}", ++projects_processed, projects_count, p.Package.Name, string.IsNullOrEmpty(p.Artifact.Version) ? "" :
                        string.Format("({0}) ", p.Artifact.Version));
                if (package_vulnerabilities.Count() == 0 && project_vulnerabilities.Count() == 0)
                {
                    PrintMessage("no known vulnerabilities. ");
                    PrintMessage("[{0} {1}]\n", p.Package.Name, p.Package.Version);
                }
                else if (package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange) == 0 && project_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange) == 0)
                {
                    PrintMessage("{0} known vulnerabilities, 0 affecting current package version. ", package_vulnerabilities.Count() + project_vulnerabilities.Count());
                    PrintMessage("[{0} {1}]\n", p.Package.Name, p.Package.Version);
                }
                else
                {
                    PrintMessageLine(ConsoleColor.Red, "[VULNERABLE]");
                    PrintMessage(ConsoleColor.Magenta, "{0} known vulnerabilities, ", project_vulnerabilities.Count() + package_vulnerabilities.Count()); //vulnerabilities.Value.GroupBy(v => new { v.CVEId, v.Uri, v.Title, v.Summary }).SelectMany(v => v).Count(),
                    PrintMessage("{0} affecting installed version. ", project_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange) + package_vulnerabilities.Count(v => v.CurrentPackageVersionIsInRange));
                    PrintMessageLine("[{0} {1}]", p.Package.Name, p.Package.Version);
                    package_vulnerabilities.Where(v => v.CurrentPackageVersionIsInRange).ToList().ForEach(v =>
                    {
                        PrintMessageLine(ConsoleColor.Red, "{0} {1}", v.Id.Trim(), v.Title.Trim());
                        PrintMessageLine(v.Summary);
                        PrintMessage(ConsoleColor.Red, "Affected versions: ");
                        PrintMessageLine(ConsoleColor.White, string.Join(", ", v.Versions.ToArray()));
                        PrintMessageLine("");
                    });
                    project_vulnerabilities.Where(v => v.CurrentPackageVersionIsInRange).ToList().ForEach(v =>
                    {
                        PrintMessageLine(ConsoleColor.Red, "{0} {1}", v.CVEId, v.Title);
                        PrintMessageLine(v.Summary);
                        PrintMessage(ConsoleColor.Red, "Affected versions: ");
                        PrintMessageLine(ConsoleColor.White, string.Join(", ", v.Versions.ToArray()));
                        PrintMessageLine("");
                    });
                }
            }
            Source.Dispose();
            return;
        }

        static void AuditApplication(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            if (Stopwatch.IsRunning) Stopwatch.Stop();
            exit = ar;
            if (Spinner != null) StopSpinner();
            if (ProgramOptions.ListConfigurationRules)
            {
                if (ar == AuditTarget.AuditResult.SUCCESS && Application.ProjectConfigurationRules.Count() > 0)
                {
                    int i = 1;
                    foreach (var project_rule in Application.ProjectConfigurationRules)
                    {
                        if (project_rule.Key.Name == Application.ApplicationId + "_" + "default") continue;
                        PrintMessage("[{0}/{1}]", i, Application.ProjectConfigurationRules.Count);
                        PrintMessageLine(ConsoleColor.Blue, " {0} ", project_rule.Key.Name);
                        int j = 1;
                        foreach (OSSIndexProjectConfigurationRule rule in project_rule.Value)
                        {
                            PrintMessageLine("  [{0}/{1}] {2}", j++, project_rule.Value.Count(), rule.Title);
                        }
                    }
                    return;
                }
                else if (ar == AuditTarget.AuditResult.SUCCESS && Application.ProjectConfigurationRules.Count() == 0)
                {
                    PrintMessageLine("No configuration rules found for {0}. ", Application.ApplicationLabel);
                    return;
                }
            }
            else if (Application.ProjectConfigurationRules.Count() > 0)
            {
                PrintMessageLine(ConsoleColor.White, "\nApplication Configuration Audit Results\n=======================================");
                PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilities found in {1} application configuration audit. Total time for audit: {2} ms.\n", Application.ProjectConfigurationRulesEvaluations.Values.Where(v => v.Item1).Count(), Application.ApplicationLabel, Stopwatch.ElapsedMilliseconds);
                int projects_count = Application.ProjectConfigurationRules.Count, projects_processed = 0;
                foreach (KeyValuePair<OSSIndexProject, IEnumerable<OSSIndexProjectConfigurationRule>> rule in Application.ProjectConfigurationRules)
                {
                    IEnumerable<KeyValuePair<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>>> evals = Application.ProjectConfigurationRulesEvaluations.Where(pcre => pcre.Key.Project.Name == rule.Key.Name);
                    PrintMessage("[{0}/{1}] Project: ", ++projects_processed, projects_count);
                    PrintMessage(ConsoleColor.Blue, "{0}. ", rule.Key.Name);
                    int total_project_rules = rule.Value.Count();
                    int succeded_project_rules = evals.Count(ev => ev.Value.Item1);
                    int processed_project_rules = 0;
                    PrintMessage("{0} rule(s). ", total_project_rules);
                    if (succeded_project_rules > 0)
                    {
                        PrintMessage(ConsoleColor.Magenta, " {0} rule(s) succeded. ", succeded_project_rules);
                        PrintMessageLine(ConsoleColor.Red, "[VULNERABLE]");
                    }
                    else
                    {
                        PrintMessage(ConsoleColor.DarkGreen, " {0} rule(s) succeded. \n", succeded_project_rules);
                    }
                    foreach (KeyValuePair<OSSIndexProjectConfigurationRule, Tuple<bool, List<string>, string>> e in evals)
                    {
                        ++processed_project_rules;
                        if (!e.Value.Item1)
                        {
                            PrintMessage("--[{0}/{1}] Rule: {2}. Result: ", processed_project_rules, total_project_rules, e.Key.Title);
                        }
                        else
                        {
                            PrintMessage(ConsoleColor.White, "--[{0}/{1}] Rule: {2}. Result: ", processed_project_rules, total_project_rules, e.Key.Title);
                        }
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
            }
            else
            {
                PrintMessageLine(ConsoleColor.White, "\nApplication Configuration Audit Results\n=======================================");
                PrintMessageLine(ConsoleColor.White, "{0} total vulnerabilities found in {1} application configuration audit. Total time for audit: {2} ms.\n", Application.ProjectConfigurationRulesEvaluations.Values.Where(v => v.Item1).Count(), Application.ApplicationLabel, Stopwatch.ElapsedMilliseconds);
            }
        }

        static void AuditCodeProject(AuditTarget.AuditResult ar, out AuditTarget.AuditResult exit)
        {
            exit = ar;
            if (ar == AuditTarget.AuditResult.ERROR_SCANNING_WORKSPACE)
            {
                PrintErrorMessage("There was an error scanning the code project workspace.");
                return;
            }
            else if (ar == AuditTarget.AuditResult.ERROR_SCANNING_ANALYZERS)
            {
                PrintErrorMessage("There was an error scanning the code project analyzer scripts.");
                return;
            }
            else if (ar == AuditTarget.AuditResult.ERROR_ANALYZING)
            {
                PrintErrorMessage("There was an error analyzing the code project;");
                return;
            }
            else if (ar != AuditTarget.AuditResult.SUCCESS)
            {
                throw new Exception("Unknown audit target state.");
            }
            foreach (AnalyzerResult analyzer_result in CodeProject.AnalyzerResults)
            {

            }
        }

        static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e)
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
                PrintErrorMessage(e.InnerException);
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
            if (e.GetType() == typeof(OSSIndexHttpException))
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
