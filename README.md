
**Note**: DevAudit uses the OSS Index database, which has some rate limiting. If you notice you are hitting the limit please raise an issue. Authenticated users get a higher limit, and we am implementing authentication into DevAudit soon. Most non-authenticated users probably won't notice the limit for many use cases. It usually kicks in only in much larger projects or higher project volumes.


# DevAudit: Development Auditing
Get the latest release from the [releases](https://github.com/OSSIndex/DevAudit/releases) page.

![Screenshot of DevAudit package source audit](https://lh3.googleusercontent.com/tR98RwJops5G97vjm6-lXWHAxAhLA_pvan7qKF9wrxJttPt6C8VW9kGnruvPnUJ7q1jV2exWGH9w=w1382-h957-no)

![Screenshot of DevAudit Wheezy dpkg package source audit](https://lh3.googleusercontent.com/ehCLOrKGdSIMZZN-gWCzJS_VdzxRF5UCQ9UgSkasH6WUuue-RRXVSOgF4xXFz2F4be9VCSYVBV_WhRs4Gjlcp-Wuc_a-QaRBOKaR083M-h8f29dwyY7iFqIJkgWcNPAAI1dUKx1rbb6uUnDAqwv2WDzehzbFgGose8Vc8H2ND-eY3NHYTR7T06hA9_b9s4QL7EZetogvCG1W8Ofa9vjDJufQ1POwVhnLriTLI7tS25lWGF0OYE8zVfWE3132nj5IZNaKtn0D4rumHCvsE-r2g2ev-uylKsTTACuiUOzRfMSFbIlw54IQP7GBhsdJKFPm0-zhSsiJK6WNBIcNcKUEVp1VR7s0I0D0vihwpAJsPhrpKJJE3asUpzJXJbBIqycKLYP-6u6uONAyPHJJT4MTHsJlIh6D4iuPgNFZWP6rPCdUs2hv-_4U9JPCKPBYRD8Z-EbjJzZgsOzGkyJTmGseCezI93Hi17vsnGUcuPeo0cyMmKW0piML8B1FGZV11_aqqdftcMsHz09Xn2HioV6SRRZC_igIE9cg6GAMvj-BjeaJUeibsRaG58eyy6H6QQsdO4tbFRTcbBeDAKwb9FDOmVa8dDWjN24j53aRbhnJoTx-nYdFKr2QPuQWAIWK2dAebwMDpF9Aix7Aad_VoXeyqIn8QNFlWAJ6T78fRtbXjw=w1082-h811-no)

## Table of Contents
* [About](https://github.com/OSSIndex/DevAudit/wiki#about)
* [Features](https://github.com/OSSIndex/DevAudit/wiki#features)
* [Requirements](https://github.com/OSSIndex/DevAudit/wiki#requirements)
* [Installation](https://github.com/OSSIndex/DevAudit/wiki#installation)
* [Concepts](https://github.com/OSSIndex/DevAudit/wiki#concepts)
* [Basic Usage](https://github.com/OSSIndex/DevAudit/wiki#basic-usage)
* [Audit Targets](https://github.com/OSSIndex/DevAudit/wiki#audit-targets)
* [Environments](https://github.com/OSSIndex/DevAudit/wiki#environments)
* [Program Options](https://github.com/OSSIndex/DevAudit/wiki#program-options)
* [Docker Usage](https://github.com/OSSIndex/DevAudit/wiki#docker-usage)
* [Troubleshooting](https://github.com/OSSIndex/DevAudit/wiki#troubleshooting)
* [Known Issues](https://github.com/OSSIndex/DevAudit/wiki#known-issues)

## About
DevAudit is an open-source, cross-platform, multi-purpose security auditing tool targeted at developers and teams adopting DevOps and DevSecOps that detects security vulnerabilities at multiple levels of the solution stack. DevAudit provides a wide array of auditing capabilities that automate security practices and implementation of security auditing in the software development life-cycle. DevAudit can scan your operating system and application package dependencies, application and application server configurations, and application code, for potential vulnerabilities based on data aggregated by providers like [OSS Index](https://ossindex.net/) and [Vulners](https://vulners.com) from a wide array of sources and data feeds such as the National Vulnerability Database (NVD) CVE data feed, the Debian Security Advisories data feed, Drupal Security Advisories, and many others.

DevAudit helps developers address at least 4 of the [OWASP Top 10](https://www.owasp.org/index.php/Top_10_2013) risks to web application development: 
* [A9 Using Components with Known Vulnerabilities](https://www.owasp.org/index.php/Top_10_2013-A9-Using_Components_with_Known_Vulnerabilities)
* [A5 Security Misconfiguration](https://www.owasp.org/index.php/Top_10_2013-A5-Security_Misconfiguration)
* [A6 Sensitive Data Exposure](https://www.owasp.org/index.php/Top_10_2013-A6-Sensitive_Data_Exposure)
* [A2 Broken Authentication and Session Management](https://www.owasp.org/index.php/Top_10_2013-A2-Broken_Authentication_and_Session_Management)

as well as risks classified by MITRE in the CWE dictionary such as [CWE-2 Environment](http://cwe.mitre.org/data/definitions/2.html) and [CWE-200 Information Disclosure](http://cwe.mitre.org/data/definitions/200.html)

![Screenshot of DevAudit ASP.NET application audit](https://lh3.googleusercontent.com/WiMC-en25YIOG5lWzPjhF6D9l3WTw5GdY_ne-LjpbQcOcaWgzg2beS3fQc1YrCVblmPo59QIZMmWk98suJjEG_CGeC1gAEfPqZbOUbm59ibTwfuxvtHSr-dwNkp8NMzl7PYHHg=w1402-h815-no)
As development progresses and its capabilities mature, DevAudit will be able to address the other risks on the OWASP Top 10 and CWE lists like Injection and XSS. With the focus on web and cloud and distributed multi-user applications, software development today is increasingly a complex affair with security issues and potential vulnerabilities arising at all levels of the stack developers rely on to deliver applications. The goal of DevAudit is to provide a platform for automating implementation of development security reviews and best practices at all levels of the solution stack from library package dependencies to application and server configuration to source code.

## Features
* **Cross-platform with a Docker image also available.** DevAudit runs on Windows and Linux with *BSD and Mac and ARM Linux support planned. Only an up-to-date version of .NET or Mono is required to run DevAudit. A [DevAudit Docker image](https://hub.docker.com/r/ossindex/devaudit/) can also be pulled from Docker Hub and run without the need to install Mono.

* **CLI interface.** DevAudit has a CLI interface with an option for non-interactive output and can be easily integrated into CI build pipelines or as post-build command-line tasks in developer IDEs. Work on integration of the core audit library into IDE GUIs has already begun with the [Audit.Net](https://visualstudiogallery.msdn.microsoft.com/73493090-b219-452a-989e-e3d228023927?SRC=Home) Visual Studio extension.

*  **Continuously updated vulnerabilties data**. DevAudit uses backend data providers like [OSS Index](https://ossindex.net/) and [Vulners](https://vulners.com/#stats) which provide continuously updated vulnerabilities data compiled from a [wide range](https://vulners.com/stats) of security data feeds and sources such as the NVD CVE feeds, Drupal Security Advisories, and so on. Support for additional vulnerability and package data providers like [vFeed](https://vfeed.io) and [Libraries.io](https://libraries.io) will be added.

* **Audit operating system and development package dependencies.** DevAudit audits Windows applications and packages installed via Windows MSI, Chocolatey, and OneGet, as well as Debian, Ubuntu, and CentOS Linux packages installed via Dpkg, RPM and YUM, for vulnerabilities reported for specific versions of the applications and packages. For development package dependencies and libraries DevAudit audits NuGet v2 dependencies for .NET, Yarn/NPM and Bower dependencies for nodejs, and Composer package dependencies for PHP. Support for other package managers for different languages is added regularly.

* **Audit application server configurations**. DevAudit audits the server version and the server configuration for the OpenSSH sshd, Apache httpd, MySQL/MariaDB, PostgreSQL, and Nginx servers with many more coming. Configuration auditing is based on the [Alpheus](https://github.com/allisterb/Alpheus) library and is done using full syntactic analysis of the server configuration files. Server configuration rules are stored in YAML text files and can be customized to the needs of developers. Support for many more servers and applications and types of analysis like database auditing is added regularly.

* **Audit application configurations**. DevAudit audits Microsoft ASP.NET applications and detects vulnerabilities present in the application configuration. Application configuration rules are stored in YAML text files and can be customized to the needs of developers. Application configuration auditing for applications like Drupal and WordPress and DNN CMS is coming.
 
* **Audit application code by static analysis**. DevAudit currently supports static analysis of .NET CIL bytecode. Analyzers reside in external script files and can be fully customized based on the needs of the developer. Support for C# source code analysis via Roslyn, PHP7 source code and many more languages and external static code analysis tools is coming.

* **Remote agentless auditing**. DevAudit can connect to remote hosts via SSH with identical auditing features available in remote environments as in local environments. Only a valid SSH login is required to audit remote hosts and DevAudit running on Windows can connect to and audit Linux hosts over SSH. On Windows DevAudit can also remotely connect to and audit other Windows machines using WinRM.

* **Agentless Docker container auditing**. DevAudit can audit running Docker containers from the Docker host with identical features available in container environments as in local environments.

* **GitHub repository auditing**. DevAudit can connect directly to a project repository hosted on GitHub and perform package source and application configuration auditing.

* **PowerShell support**. DevAudit can also be run inside the PowerShell system administration environment as cmdlets. Work on PowerShell support is paused at present but will resume in the near future with support for cross-platform Powershell both on Windows and Linux.

## Requirements
DevAudit is a .NET 4.6 application. To install locally on your machine you will need either the Microsoft .NET Framework 4.6 runtime on Windows, or Mono 4.4+ on Linux. .NET 4.6 should be already installed on most recent versions of Windows, if not then it is available as a Windows feature that can be turned on or installed from the Programs and Features control panel applet on consumer Windows, or from the Add Roles and Features option in Server Manager on server versions of Windows. For older versions of Windows, the .NET 4.6 installer from Microsoft can be found [here](https://www.microsoft.com/en-us/download/details.aspx?id=48130).

On Linux the minimum version of Mono supported is 4.4. Although DevAudit runs on Mono 4 ([with one known issue](https://github.com/OSSIndex/DevAudit/issues/78)) it's recommended that Mono 5 be installed. Mono 5 brings many [improvements](http://www.mono-project.com/news/2017/05/15/mono-5-0-is-out/) to the build and runtime components of Mono that benefit DevAudit. 

The existing Mono packages provided by your distro are probably not Mono 5 as yet, so you will have to install Mono packages manually to be able to use Mono 5. Installation instructions for the most recent packages provided by the Mono project for several major Linux distros are [here](http://www.mono-project.com/docs/getting-started/install/linux/). It is recommended you have the mono-devel package installed as this will reduce the chances of missing assemblies.

Alternatively on Linux you can use the DevAudit Docker image if you do not wish to install Mono and already have Docker installed on your machine.

## Installation
DevAudit can be installed by the following methods:
- Building from source.
- Using a binary release archive file downloaded from Github for Windows or Linux. 
- Using the release MSI installer downloaded from Github for Windows.
- Using the Chocolatey package manager on Windows.
- Pulling the ossindex/devaudit image from Docker Hub on Linux.

### Building from source on Linux
1. Pre-requisites: Mono 4.4+ (Mono 5 recommended) and the mono-devel package which provides the compiler and other tools needed for building Mono apps. Your distro should have packages for at least Mono version 4.4 and above, otherwise manual installation instructions for the most recent packages provided by the Mono project for several major Linux distros are [here](http://www.mono-project.com/docs/getting-started/install/linux/)

2. Clone the DevAudit repository from https://github.com/OSSIndex/DevAudit.git

3. Run the `build.sh` script in the root DevAudit directory. DevAudit should compile without any errors.

4. Run `./devaudit --help` and you should see the DevAudit version and help screen printed.

Note that NuGet on Linux may occasionally exit with `Error: NameResolutionFailure` which seems to be a transient problem contacting the servers that contain the NuGet packages. You should just run ./build.sh again until the build completes normally.

### Building from source on Windows
1. Pre-requisites: You must have one of:
    * A [.NET Framework 4.6](http://getdotnet.azurewebsites.net/target-dotnet-platforms.html) SDK or developer pack.
    * Visual Studio 2015.
2. Clone the DevAudit repository from https://github.com/OSSIndex/DevAudit.git

3. From a visual Studio 2015 or ,NETRun the `build.cmd` script in the root DevAudit directory. DevAudit should compile without any errors.

4. Run `./devaudit --help` and you should see the DevAudit version and help screen printed.

### Installing from the release archive files on Windows on Linux
1. Pre-requisites: You must have Mono 4.4+ on Linux or .NET 4.6 on Windows.
2. Download the latest release archive file for Windows or Linux from the project [releases](https://github.com/OSSIndex/DevAudit/releases) page. Unpack this file to a directory.

2. From the directory where you unpacked the release archive run `devaudit --help` on Windows or `./devaudit --help` on Linux. You should see the version and help screen printed.

3. (Optional) Add the DevAudit installation directory to your PATH environment variable

### Installing using the MSI Installer on Windows
The MSI installer for a release can be found on the Github releases page.

1. Click on the [releases](https://github.com/OSSIndex/DevAudit/releases) link near the top of the page.
2. Identify the release you would like to install.
3. A "DevAudit.exe" link should be visible for each release that has a pre-built installer.
4. Download the file and execute the installer. You will be guided through a simple installation.
6. Open a *new* command prompt or PowerShell window in order to have DevAudit in path.
7. Run DevAudit.

### Installing using Chocolatey on Windows 
DevAudit is also available on [Chocolatey](https://chocolatey.org/packages/devaudit/2.0.0.40-beta).

1. Install [Chocolatey](https://chocolatey.org).
2. Open an admin console or PowerShell window.
3. Type `choco install devaudit`
4. Run DevAudit.

### Installing using Docker on Linux
Pull the Devaudit image from Docker Hub: `docker pull ossindex/devaudit`. The image tagged `ossindex/devaudit:latest` (which is the default image that is downloaded) is built from the most recent release while `ossindex/devaudit:unstable` is built on the master branch of the source code and contains the newest additions albeit with less testing.

Concepts
---
#### Audit Target
Represents a logical group of auditing functions. DevAudit currently supports the following audit targets:

- **Package Source**. A package source manages application and library dependencies using a package manager. Package managers install, remove or update applications and library dependencies for an operating system like Debian Linux, or for a development language or framework like .NET or nodejs. Examples of package sources are dpkg, yum, Chocolatey, Composer, and Bower. DevAudit audits the names and versions of installed packages against vulnerabilities reported for specific versions of those packages.
- **Application**. An application like Drupal or a custom application built using a framework like ASP.NET. DevAudit audits applications and application modules and plugins against vulnerabilities reported for specific versions of application binaries and modules and plugins. DevAudit can also audit application configurations for known vulnerabilities, and perform static analysis on application code looking for known weaknesses. 
- **Application Server**. Application servers provide continuously running services or daemons like a web or database server for other applications to use, or for users to access services like authentication. Examples of application servers are the OpenSSH sshd and Apache httpd servers. DevAudit can audit application server binaries, modules and plugins against vulnerabilities reported for specific versions as well as audit server configurations for known server configuration vulnerabilities and weaknesses.

#### Audit Environment
Represents a logical environment where audits against audit targets are executed. Audit environments abstract the I/O and command executions required for an audit and allow identical functions to be performed against audit targets on whatever physical or network location the target's files and executables are located. The follwing environments are currently supported :

- **Local**. This is the default audit environment where audits are executed on the local machine.
- **SSH**. Audits are executed on a remote host connected over SSH. It is not necessary to have DevAudit installed on the remote host.
- **WinRM**. Audits are executed on a remote Windows host connected over WinRM. It is not necessary to have DevAudit installed on the remote host.
- **Docker**. Audits are executed on a running Docker container. It is not necessary to have DevAudit installed on the container image.
- **GitHub**. Audits are executed on a GitHub project repository's file-system directly. It is not necessary to checkout or download the project locally to perform the audit.

#### Audit Options
These are different options that can be enabled for the audit. You can specify options that apply to the DevAudit program for example, to run in non-interactive mode, as well as options that apply to the target e.g if you set the AppDevMode option for auditing ASP.NET applications to true then certain audit rules will not be enabled. 

Basic Usage
---
The CLI is the primary interface to the DevAudit program and is suitable both for interactive use and for non-interactive use in scheduled tasks, shell scripts, CI build pipelines and post-build tasks in developer IDEs. The basic DevAudit CLI syntax is:

	devaudit TARGET [ENVIRONMENT] | [OPTIONS]
where `TARGET` specifies the audit target `ENVIRONMENT` specifies the audit environment and `OPTIONS` specifies the options for the audit target and environment. There are 2 ways to specify options: program options and general audit options that apply to more than one target can be specified directly on the command-line as parameters . Target-specific options can be specified with the `-o` options using the format: `-o OPTION1=VALUE1,OPTION2=VALUE2,....` with commas delimiting each option key-value pair.

If you are piping or redirecting the program output to a file then you should always use the `-n --non-interactive` option to disable any interactive user interface features and animations. 

When specifying file paths, an @ prefix before a path indicates to DevAudit that this path is relative to the root directory of the audit target e.g if you specify: 
`-r c:\myproject -b @bin\Debug\app2.exe`
DevAudit considers the path to the binary file as c:\myproject\bin\Debug\app2.exe.

Audit Targets
---
#### Package Sources
- `msi`    Do a package audit of the Windows Installer MSI package source on Windows machines.

- `choco`  Do a package audit of packages installed by the Choco package manager.

- `oneget` Do a package audit of the system OneGet package source on Windows.

- `nuget` Do a package audit of a NuGet v2 package source. You must specify the location of the NuGet `packages.config` file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched for this file.
- `bower` Do a package audit of a Bower package source. You must specify the location of the Bower `packages.json` file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched for this file.
- `composer` Do a package audit of a Composer package source. You must specify the location of the Composer `composer.json` file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched for this file.

- `dpkg` Do a package audit of the system dpkg package source on Debian Linux and derivatives.

- `rpm` Do a package audit of the system RPM package source on RedHat Linux and derivatives.

- `yum` Do a package audit of the system Yum package source on RedHat Linux and derivatives.

For every package source the following general audit options can be used:
- `-f --file` Specify the location of the package manager configuration file if needed. The NuGet, Bower and Composer package sources require this option.
- `--list-packages` Only list the packages in the package source scanned by DevAudit.

- `--list-artifacts` Only list the artifacts found on OSS Index for packages scanned by DevAudit.

Package sources tagged [Experimental] are only available in the master branch of the source code and may have limited back-end OSS Index support. However you can always list the packages scanned and artifacts available on OSS Index using the `list-packages` and `list-artifacts` options.

#### Applications
- `aspnet` Do an application audit on a ASP.NET application. The relevant options are:
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level application directory that contains files like Global.asax and Web.config.
	- `-b --application-binary` Specify the application binary. The is the .NET assembly that contains the application's .NET bytecode. This file is usually a .DLL and located in the bin sub-folder of the ASP.NET application root directory.
	- `--config-file` or `-o AppConfig=configuration-file` Specifies the ASP.NET application configuration file. This file is usually named Web.config and located in the application root directory. You can override the default @Web.config value with this option.
	- `-o AppDevMode=enabled` Specifies that application development mode should be enabled for the audit. This mode can be used when auditing an application that is under development. Certain configuration rules that are tagged as disabled for AppDevMode (e.g running the application in ASP.NET debug mode) will not be enabled during the audit.

- `netfx` Do an application audit on a .NET application. The relevant options are:
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level application directory that contains files like App.config.
	- `-b --application-binary` Specify the application binary. The is the .NET assembly that contains the application's .NET bytecode. This file is usually a .DLL and located in the bin sub-folder of the ASP.NET application root directory.
	- `--config-file` or `-o AppConfig=configuration-file` Specifies the .NET application configuration file. This file is usually named App.config and located in the application root directory. You can override the default @App.config value with this option.
	- `-o GendarmeRules=RuleLibrary` Specifies that the [Gendarme](http://www.mono-project.com/docs/tools+libraries/tools/gendarme/) static analyzer should enabled for the audit with rules from the specified rules library used. For example: 
	`devaudit netfx -r /home/allisterb/vbot-debian/vbot.core -b @bin/Debug/vbot.core.dll --skip-packages-audit -o GendarmeRules=Gendarme.Rules.Naming`
	will run the Gendarme static analyzer on the vbot.core.dll assembly using rules from Gendarme.Rules.Naming library. 	The complete list of rules libraries is (taken from the Gendarme wiki):
    * [Gendarme.Rules.BadPractice](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.BadPractice%28git%29)
	* [Gendarme.Rules.Concurrency](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Concurrency%28git%29)
	* [Gendarme.Rules.Correctness](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Correctness%28git%29)
	* [Gendarme.Rules.Design](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Design%28git%29)
	* [Gendarme.Rules.Design.Generic](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Design.Generic%28git%29)
	* [Gendarme.Rules.Design.Linq](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Design.Linq%28git%29)
	* [Gendarme.Rules.Exceptions](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Exceptions%28git%29)
	* [Gendarme.Rules.Gendarme](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Gendarme%28git%29)
	* [Gendarme.Rules.Globalization](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Globalization%28git%29)
	* [Gendarme.Rules.Interoperability](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Interoperability%28git%29)
	* [Gendarme.Rules.Interoperability.Com](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Interoperability.Com%28git%29)
	* [Gendarme.Rules.Maintainability](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Maintainability%28git%29)
	* [Gendarme.Rules.NUnit](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.NUnit%28git%29)
	* [Gendarme.Rules.Naming](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Naming%28git%29)
	* [Gendarme.Rules.Performance](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Performance%28git%29)
	* [Gendarme.Rules.Portability](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Portability%28git%29)
	* [Gendarme.Rules.Security](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Security%28git%29)
	* [Gendarme.Rules.Security.Cas](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Security.Cas%28git%29)
	* [Gendarme.Rules.Serialization](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Serialization%28git%29)
	* [Gendarme.Rules.Smells](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Smells%28git%29)
	* [Gendarme.Rules.Ui](https://github.com/spouliot/gendarme/wiki/Gendarme.Rules.Ui%28git%29)

- `drupal7` Do an application audit on a Drupal 7 application.
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level directory of your Drupal 7 install.

- `drupal8` Do an application audit on a Drupal 8 application.
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level directory of your Drupal 8 install.

All applications also support the following common options for auditing the application modules or plugins:
- `--list-packages` Only list the application plugins or modules scanned by DevAudit.

- `--list-artifacts` Only list the artifacts found on OSS Index for application plugins and modules scanned by DevAudit.

- `--skip-packages-audit` Only do an appplication configuration or code analysis audit and skip the packages audit.

#### Application Servers
- `sshd` Do an application server audit on an OpenSSH sshd-compatible server.

- `httpd` Do an application server audit on an Apache httpd-compatible server.

- `mysql` Do an application server audit on a MySQL-compatible server (like MariaDB or Oracle MySQL.)

- `nginx` Do an application server audit on a Nginx server.

- `pgsql` Do an application server audit on a PostgreSQL server.

This is an example command line for an application server audit:
`./devaudit httpd -i httpd-2.2 -r /usr/local/apache2/ --config-file @conf/httpd.conf -b @bin/httpd`
which audits an Apache Httpd server running on a Docker container named httpd-2.2.

The following are audit options common to all application servers:
- `-r --root-directory` Specifies the root directory of the server. This is just the top-level of your server filesystem and defaults to `/` unless you want a different server root. 
- `--config-file` Specifies the server configuration file. e.g in the above audit the Apache configuration file is located at `/usr/local/apache2/conf/httpd.conf`. If you don't specify the configuration file DevAudit will attempt to auto-detect the configuration file for the server selected.
- `-b --application-binary` Specifies the server binary. e.g in the above audit the Apache binary is located at `/usr/local/apache2/bin/httpd`. If you don't specify the binary path DevAudit will attempt to auto-detect the server binary for the server selected.

Application servers also support the following common options for auditing the server modules or plugins:
- `--list-packages` Only list the application plugins or modules scanned by DevAudit.

- `--list-artifacts` Only list the artifacts found on OSS Index for application plugins and modules scanned by DevAudit.

- `--skip-packages-audit` Only do a server configuration audit and skip the packages audit.

Environments
---

There are currently 5 audit environment supported: local, remote hosts over SSH, remote hosts over WinRM, Docker containers, and GitHub. Local environments are used by default when no other environment options are specified.

#### SSH
The SSH environment allows audits to be performed on any remote hosts accessible over SSH without requiring DevAudit to be installed on the remote host. SSH environments are cross-platform: you can connect to a Linux remote host from a Windows machine running DevAudit. An SSH environment is created by the following options:`-s SERVER [--ssh-port PORT] -u USER [-k KEYFILE] [-p | --password-text PASSWORD]`

`-s SERVER` Specifies the remote host or IP to connect to via SSH.

`-u USER` Specifies the user to login to the server with.

`--ssh-port PORT` Specifies the port on the remote host to connect to. The default is 22.

`-k KEYFILE` Specifies the OpenSSH compatible private key file to use to connect to the remote server. Currently only RSA or DSA keys in files in the PEM format are supported.

`-p` Provide a prompt with local echo disabled for interactive entry of the server password or key file passphrase.

`--password-text PASSWORD` Specify the user password or key file passphrase as plaintext on the command-line. Note that on Linux when your password contains special characters you should use enclose the text on the command-line using single-quotes like `'MyPa<ss'` to avoid the shell interpreting the special characters.

#### WinRM
The WinRM environment allows audits to be performed on any remote Windows hosts accessible over WinRM without requiring DevAudit to be installed on the remote host. WinRM environments are currently only available on Windows machines running DevAudit. A WinRM environment is created by the following options:`-w IP -u USER [-p | --password-text PASSWORD]`

`-w IP` Specifies the remote IP to connect to via WinRM.

`-u USER` Specifies the user to login to the server with.

`-p` Provide a prompt with local echo disabled for interactive entry of the server password or key file passphrase.

`--password-text PASSWORD` Specify the server password or key file passphrase as plaintext on the command-line.

#### Docker
This section discusses how to **audit** Docker images using DevAudit installed on the local machine. For **running** DevAudit as a containerized Docker app see the section below on Docker Usage.

A Docker audit environment is specified by the following option: `-i CONTAINER_NAME | -i CONTAINER_ID`

![Screenshot of DevAudit auditing a Docker container](https://lh3.googleusercontent.com/id4IBEf25ugvSjZOxF7KVY8CDoWLNlbb5dyHQ8R6wabtFpMjwrVl_EIJNnoPobzo2UCJj5vghOac8phJ2UKLciqCljB1ioUQ682dRTgOYOgkfLBZLrxVM2lcHGJX7wnQpyqyWw=w900-h450-no)
`CONTAINER_(NAME|ID)` Specifes the name or id of a running Docker container to connect to. The container must be already running as DevAudit does not know how to start the container with the name or the state you require.  

#### GitHub
The GitHub audit environment allows audits to be performed directly on a GitHub project repository. A GitHub environment is created by the `-g` option: `-g "Owner=OWNER,Name=NAME,Branch=BRANCH"`

`OWNER` Specifies the owner of the project

`NAME` Specifies the name of the project

`PATH` Specifies the branch of the project to connect to

You can use the `-r`, `--config-file`, and `-f` options as usual to specify the path to file-system files and directories required for the audit. e.g the following commad:
`devaudit aspnet -g "Owner=Dnnsoftware,Name=Dnn.Platforn,Branch=Release/9.0.2" -r /Website --config-file @web.config`
will do an ASP.NET audit on this repository https://github.com/dnnsoftware/Dnn.Platform/ using the `/Website` source folder as the root directory and the `web.config` file as the ASP.NET configuration file. Note that filenames are case-sensitive in most environments.  

![Screenshot of a GitHub project audit](https://cdn-images-1.medium.com/max/800/1*Uj0WBK9RlS8YvN0qW-IFZQ.png)

Program Options
---
`-n --non-interactive` Run DevAudit in non-interactive mode with all interactive features and animations of the CLI disabled. This mode is necessary for running DevAudit in shell scripts for instance otherwise errors will occure when DevAudit attempts to use interactive console features. 

`-d --debug` Run DevAudit in debug mode. This will print a variety of informational and diagnostic messages. This mode is used for troubleshooting DevAudit errors and bugs.

Docker Usage
---
DevAudit also ships as a Docker containerized app which allows users on Linux to run DevAudit without the need to install Mono and build from source. To pull the DevAudit Docker image from Docker Hub:

`docker pull ossindex/devaudit[:label]`

The current images are about 131 MB compressed. By default the image labelled `latest` is pulled which is the most recent release of the program. An `unstable` image is also available which tracks the master branch of the source code. To run DevAudit as a containerized app:

	docker run -i -t ossindex/devaudit TARGET [ENVIRONMENT] | [OPTIONS]

The -i and -t Docker options are necessary for running DevAudit interactively. If you don't specify these options then you must run DevAudit in non-interactive mode by using the DevAudit option -n. 

You must mount any directories on the Docker host machine that DevAudit needs to access on the DevAudit Docker container using the Docker -v option. If you mount your local root directory at a mount point named /hostroot on the Docker image then DevAudit can access files and directories on your local machine using the same local paths. For example:

`docker run -i -t -v /:/hostroot:ro ossindex/devaudit netfx -r /home/allisterb/vbot-debian/vbot.core`

will allow the DevAudit Docker container to audit the local directory /home/allisterb/vbot-debian/vbot.core. You _must_ mount your local root in this way to audit _other_ Docker containers from the DevAudit container e.g. 

`docker run -i -t -v /:/hostroot:ro ossindex/devaudit mysql -i myapp1 -r / --config-file /etc/my.cnf --skip-packages-audit`

will run a MySQL audit on a Docker container named `myapp1` from the `ossindex/devaudit` container.

If you do not need to mount your entire root directory then you can mount just the directory needed for the audit. For example:

`docker run -i -t -v /home/allisterb/vbot-debian/vbot.core:/vbot:ro ossindex/devaudit netfx -r /vbot -b @bin/Debug/vbot.core.dll`

will mount read-only the `/home/allisterb/vbot-debian/vbot.core` directory as `/vbot` on the DevAudit container which allows DevAudit to access it as the audit root directory for a netfx application audit at `/vbot`.

If you wish to use private key files on the local Docker host for an audit over SSH, you can mount your directory that contains the needed key file and then tell DevAudit to use that file path e.g.

`docker -i -t -v /home/allisterb/.ssh:/ssh:ro run ossindex/devaudit dpkg -s localhost -u allisterb -p -k /ssh/mykey.key`

will mount the directory containing key files at /ssh and allow the DevAudit container to use them.

Note that it's currently not possible for the Docker container to audit operating system package sources like dpkg or rpm or application servers like OpenSSH sshd on the _local_ Docker host without mounting your local root directory at `/hostroot` as described above. DevAudit must chroot into your local root directory from the Docker container when running executables like dpkg or server binaries like sshd and httpd. You must also mount your local root as described above to audit other Docker containers from the DevAudit container as DevAudit also needs to chroot into your local root to execute local Docker commands to communicate with your other containers.


For running audits over SSH from the DevAudit container it is not necessary to mount the local root at `/hostroot`.

Troubleshooting
---
If you encounter a bug or other issue with DevAudit there are a couple of things you can enable to help us resolve it:
- Use the -d option to enable debugging output. Diagnostic information will be emitted during the audit run.
- On Linux use the DEVAUDIT_TRACE variable to enable tracing program execution. The value of this variable must be in the format for [Mono tracing](http://www.mono-project.com/docs/debug+profile/debug/#tracing-program-execution) e.g you can set DEVAUDIT_TRACE=N:DevAudit.AuditLibrary to trace all the calls made to the audit library duing an audit.

Known Issues
---
- On Windows you _must_ use the `-n --non-interactive` program option when piping or redirecting program output to a file otherwise a crash will result. This behaviour may be changed in the future to make non-interactive mode the default.
- There appears to be an issue using the Windows console app [ConEmu](https://conemu.github.io/) and the Cygwin builds of the OpenSSH client when SSHing into remote Linux hosts to run Mono apps. If you run DevAudit this way you may notice strange sequences appearing sometimes at the end of console output. You may also have problems during keyboard interactive entry like entering passwords for SSH audits where the wrong password appears to be sent. If you are having problems entering passwords for SSH audits using ConEmu when working remotely, try holding the backspace key for a second or two to clear the input buffer before entering your password.
