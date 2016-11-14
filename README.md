# DevAudit: Software Development Auditing
![Screenshot of DevAudit package source audit](https://lh3.googleusercontent.com/tR98RwJops5G97vjm6-lXWHAxAhLA_pvan7qKF9wrxJttPt6C8VW9kGnruvPnUJ7q1jV2exWGH9w=w1382-h957-no)

![Screenshot of DevAudit configuration audit](https://lh3.googleusercontent.com/ZLnNFiFH-4KSFhE9Tvwqwz1jYUKpaxwlUhL7Zg_4Xojhp465fo4_armzWZ6kCIqE7C31qmQpcfuG=w1440-h957-no)
##About
DevAudit is an open-source, cross-platform, multi-purpose security auditing tool targeted at developers and DevOps practitioners that detects security vulnerabilities at multiple levels of the solution stack. DevAudit provides a wide array of auditing capabilities that automate security practices and implementation of security auditing in the software development life-cycle. DevAudit can scan your operating system and application package dependencies, application server configurations, and application source code for potential vulnerabilities based on data aggregated by OSS Index from a wide array of sources and data feeds such as the NVD CVE data feed, Debian Security Tracker feed, Drupal Security Advisories, and several others. Support for other vulnerability databases like vulners.com is also planned.

DevAudit helps developers address at least 2 of the [OWASP Top 10](https://www.owasp.org/index.php/Top_10_2013) risks to web application development: [A9 Using Components with Known Vulnerabilities](https://www.owasp.org/index.php/Top_10_2013-A9-Using_Components_with_Known_Vulnerabilities) and [A5 Security Misconfiguration](https://www.owasp.org/index.php/Top_10_2013-A5-Security_Misconfiguration) as well as risks classified by MITRE in the CWE dictionary such as [CWE-2 Environment](http://cwe.mitre.org/data/definitions/2.html). As development progress and its capabilities mature, DevAudit will be able to address the other risks on the OWASP Top 10 and CWE lists like Injection, XSS, and Sensitive Data Disclosure. With the focus on web and cloud and distributed multi-user applications, software development today is increasingly a complex affair with security issues and potential vulnerabilities arising at all levels of the stack developers rely on to deliver applications. The goal of DevAudit is to provide a platform for automating implementation of development security reviews and best practices at all levels of the solution stack from library package dependencies to application and server configuration to source code.  

## Features
* **Cross-platform with a Docker image also available.** DevAudit runs on Windows and Linux with *BSD and Mac support coming and ARM Linux a possibility. Only an up-to-date version of .NET or Mono is required to run DevAudit. A [DevAudit Docker image](https://hub.docker.com/r/ossindex/devaudit/) can also be pulled from Docker Hub and run on Linux without the need to install Mono.

* **CLI interface** DevAudit has a CLI interface with an option for non-interactive output and can be easily integrated into CI build pipelines or as post-build command-line tasks in developer IDEs. Work on integration of the core audit library into IDEs has already begun with the [Audit.Net](https://visualstudiogallery.msdn.microsoft.com/73493090-b219-452a-989e-e3d228023927?SRC=Home) Visual Studio extension.

* **Audit operating system and development package dependencies.** DevAudit audits packages installed with the dpkg and yum/rpm Linux package mangers, Chocolatey, Windows MSI and OneGet installed applications and packages on Windows, and the NuGet v2, Bower, and Composer package managers for .NET and nodejs and PHP development. Support for many more is coming.

* **Audit application server configurations.** DevAudit audits the server configuration for OpenSSH sshd, Apache httpd, MySQL and Nginx with many more coming. Configuration auditing is based on the [Alpheus](https://github.com/allisterb/Alpheus) library and is done using full syntactic (and eventually semantic) analysis of the server configuration files. Server configuration rules are stored in YAML text files and can be customized to the needs of the developer. Support for many more servers and applications is coming.
 
* **Audit application code by static analysis.** DevAudit currently supports static analysis of PHP7 code, and C# code via Roslyn. Analyzers reside in external script files and can be fully customized based on the needs of the developer. Support for many more languages and external static code analysis tools is coming.

* **Remote auditing**. DevAudit can connect to remote hosts via SSH with identical auditing features available in remote environments as in local environments. Support for other remote protocols like WinRM is also planned.

* **PowerShell** support*. DevAudit can also be run inside the PowerShell system administration environment as cmdlets. Work on PowerShell support is paused at present but will resume in the near future with support for cross-platform Powershell both on Windows and Linux.

Installation
============
Currently there are pre-built binaries for Windows. Linux support is in development.

Pre-built DevAudit binaries may be installed in one of three ways:

1. MSI Installer
2. Zip file
3. Chocolatey
 
### MSI Installer

The MSI installer for a release can be found on the release page.

1. Click on the [releases](https://github.com/OSSIndex/DevAudit/releases) link near the top of the page
2. Identify the release you would like to install
3. A "DevAudit.exe" link should be visible for each release that has a pre-built installer
4. Download the file
5. Execute the installer. You will be guided through a simple installation.
6. Open a *new* command prompt or powershell window in order to have DevAudit in path
7. Run DevAudit

### Zip file

The Zip install requires that [.NET framework version 4.5](https://www.microsoft.com/en-ca/download/details.aspx?id=30653) or later is installed.

1. Install [.NET framework version 4.5](https://www.microsoft.com/en-ca/download/details.aspx?id=30653)
2. Click on the [releases](https://github.com/OSSIndex/DevAudit/releases) link near the top of the page
2. Identify the release you would like to install
3. A "DevAudit#.#.#.zip" link should be visible for each release that has release binaries
4. Download the file
5. Unzip into the desired location
6. Add the install directory into your PATH environment variable
7. Run DevAudit
 
### Chocolatey

DevAudit is available available on [Chocolatey](https://chocolatey.org/packages/devaudit/1.0.1)

1. Install [Chocolatey](https://chocolatey.org)
2. Open an admin console or powershell window
3. Type `choco install devaudit`
4. Run DevAudit
 
Usage
=====

`devaudit <package-source> [--file <file>] [-n]`

where **package-source** is one of
* msi
* chocolatey
* oneget
* nuget
* bower

**Options**:

--file <file>
    For use with **nuget** and **bower** package sources. The file argument specifies the dependency file to be processed. For NuGet this would be "packages.config" and for bower a "bower.json" file.

-n, --non-interact
    Run in non-interactive mode. Useful for when running with a script or redirecting output to a file

Examples
========

### devaudit --msi
```
Scanning MSI packages...
Found 519 distinct packages.
Searching OSS Index for 519 MSI packages...
Found 140 artifacts, 139 with an OSS Index project id.
Searching OSS Index for vulnerabilities for 139 projects...

Audit Results
=============
[1/139] Call of Duty: Advanced Warfare  No known vulnerabilities.
[2/139] Battlelog  No known vulnerabilities.
[3/139] Amazon Kindle  1 distinct (1 total) known vulnerabilities, 0 affecting installed version.

...

[85/139] Microsoft Silverlight  [VULNERABLE]
27 distinct (27 total) known vulnerabilities, 7 affecting installed version.
[CVE-2012-0159]Microsoft Windows XP SP2 and SP3, Windows Server 2003 SP2, Windows Vista SP2, Wi...
Microsoft Windows XP SP2 and SP3, Windows Server 2003 SP2, Windows Vista SP2, Windows Server 2008 SP2, R2, and R2 SP1, Windows 7 Gold and SP1, and Windows 8 Consumer Preview; Office 2003 SP3, 2007 SP2 and SP3, and 2010 Gold and SP1; Silverlight 4 before 4.1.10329; and Silverlight 5 before 5.1.10411 allow remote attackers to execute arbitrary code via a crafted TrueType font (TTF) file, aka "TrueType Font Parsing Vulnerability."

...

[137/139] Mozilla Thunderbird  688 distinct (688 total) known vulnerabilities, 0 affecting installed version.
[138/139] Mozilla Firefox  1317 distinct (1317 total) known vulnerabilities, 0 affecting installed version.
[139/139] Google Chrome  1210 distinct (1210 total) known vulnerabilities, 0 affecting installed version.
```

### devaudit nuget -n --file DevAudit.CommandLine\Examples\packages.config.example
```
Scanning NuGet packages...
Found 28 distinct packages.
Searching OSS Index for 28 NuGet packages...
Found 34 artifacts, 16 with an OSS Index project id.
Searching OSS Index for vulnerabilities for 16 projects...

Audit Results
=============
[1/16] EntityFramework 6.1.2 No known vulnerabilities.
[2/16] EntityFramework 6.1.2 No known vulnerabilities.
[3/16] EntityFramework 6.1.2 No known vulnerabilities.
[4/16] WebGrease 1.6.0 No known vulnerabilities.
[5/16] Respond 1.2.0 No known vulnerabilities.
[6/16] Microsoft.AspNet.Razor 3.2.2 No known vulnerabilities.
[7/16] Antlr 3.5.0.2 No known vulnerabilities.
[8/16] Microsoft.AspNet.Razor 3.2.2 No known vulnerabilities.
[9/16] Twitter.Typeahead 0.10.5 No known vulnerabilities.
[10/16] Modernizr 2.8.3 No known vulnerabilities.
[11/16] Newtonsoft.Json 6.0.7 No known vulnerabilities.
[12/16] Microsoft.Data.Edm 5.6.3 1 distinct (1 total) known vulnerabilities, 0 affecting installed version.
[13/16] bootstrap 3.0.0 1 distinct (1 total) known vulnerabilities, 0 affecting installed version.
[14/16] jQuery 1.6.1 [VULNERABLE]
3 distinct (3 total) known vulnerabilities, 2 affecting installed version.
[CVE-2011-4969]Cross-site scripting (XSS) vulnerability in jQuery before 1.6.3, when using loca...
Cross-site scripting (XSS) vulnerability in jQuery before 1.6.3, when using location.hash to select elements, allows remote attackers to inject arbitrary web script or HTML via a crafted tag.

Affected versions: 1.6, 1.6.1, 1.6.2
Selector interpreted as HTML
&num;9521 and &num;6429 and probably others identify specific instances of a general problem: jQuery( strInput ) cannot reliably differentiate selectors from HTML.
?http://jsfiddle.net/C8dgG/
Looking for "<" past the first character creates vulnerabilities and confusing behavior on complex input.
quickExpr should be abandoned in favor of a simpler "parse as HTML if and only if there is a leading less-than" rule, with intentional parsing handled by the jQuery( "<div/>" ).html( strHtml ).contents() pattern.

Affected versions: <1.9.0
[15/16] Microsoft.Data.OData 5.6.3 1 distinct (1 total) known vulnerabilities, 0 affecting installed version.
[16/16] Microsoft.Data.Services.Client 5.6.3 1 distinct (1 total) known vulnerabilities, 0 affecting installed version.
```
