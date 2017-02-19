# DevAudit: Development Auditing
![Screenshot of DevAudit package source audit](https://lh3.googleusercontent.com/tR98RwJops5G97vjm6-lXWHAxAhLA_pvan7qKF9wrxJttPt6C8VW9kGnruvPnUJ7q1jV2exWGH9w=w1382-h957-no)

![Screenshot of DevAudit configuration audit](https://lh3.googleusercontent.com/ZLnNFiFH-4KSFhE9Tvwqwz1jYUKpaxwlUhL7Zg_4Xojhp465fo4_armzWZ6kCIqE7C31qmQpcfuG=w1440-h957-no)
##About
DevAudit is an open-source, cross-platform, multi-purpose security auditing tool targeted at developers and DevOps practitioners that detects security vulnerabilities at multiple levels of the solution stack. DevAudit provides a wide array of auditing capabilities that automate security practices and implementation of security auditing in the software development life-cycle. DevAudit can scan your operating system and application package dependencies, application server configurations, and application source code for potential vulnerabilities based on data aggregated by OSS Index from a wide array of sources and data feeds such as the NVD CVE data feed, Debian Security Tracker feed, Drupal Security Advisories, and several others. Support for other vulnerability databases like vulners.com is also planned.

DevAudit helps developers address at least 3 of the [OWASP Top 10](https://www.owasp.org/index.php/Top_10_2013) risks to web application development: 
* [A9 Using Components with Known Vulnerabilities](https://www.owasp.org/index.php/Top_10_2013-A9-Using_Components_with_Known_Vulnerabilities)
* [A5 Security Misconfiguration](https://www.owasp.org/index.php/Top_10_2013-A5-Security_Misconfiguration)
* [A6 Sensitive Data Disclosure](https://www.owasp.org/index.php/Top_10_2013-A6-Sensitive_Data_Exposure)


as well as risks classified by MITRE in the CWE dictionary such as [CWE-2 Environment](http://cwe.mitre.org/data/definitions/2.html). As development progress and its capabilities mature, DevAudit will be able to address the other risks on the OWASP Top 10 and CWE lists like Injection, XSS, and Sensitive Data Disclosure. With the focus on web and cloud and distributed multi-user applications, software development today is increasingly a complex affair with security issues and potential vulnerabilities arising at all levels of the stack developers rely on to deliver applications. The goal of DevAudit is to provide a platform for automating implementation of development security reviews and best practices at all levels of the solution stack from library package dependencies to application and server configuration to source code.

## Features
* **Cross-platform with a Docker image also available.** DevAudit runs on Windows and Linux with *BSD and Mac support coming and ARM Linux a possibility. Only an up-to-date version of .NET or Mono is required to run DevAudit. A [DevAudit Docker image](https://hub.docker.com/r/ossindex/devaudit/) can also be pulled from Docker Hub and run without the need to install Mono.

* **CLI interface.** DevAudit has a CLI interface with an option for non-interactive output and can be easily integrated into CI build pipelines or as post-build command-line tasks in developer IDEs. Work on integration of the core audit library into IDE GUIs has already begun with the [Audit.Net](https://visualstudiogallery.msdn.microsoft.com/73493090-b219-452a-989e-e3d228023927?SRC=Home) Visual Studio extension.

*  **Continuously updated vulnerabilties data**. DevAudit uses the [OSS Index API](https://ossindex.net/) which provides continuously updated vulnerabilities data compiled from a wide range of secuirty data feeds and sources such as the NVD CVE feeds, Drupal Security Advisories, and so on. Support for more backend data providers such as [vulners.com](https://vulners.com/#stats) is coming.

* **Audit operating system and development package dependencies.** DevAudit audits Windows applications and pacakges installed via Chocolatey, Windows MSI and OneGet for vulnerabilities reported for specific versions. For development package dependencies and libraries, DevAudit audits NuGet v2 dependencies for .NET, Bower dependencies for nodejs, and Composer package dependencies for PHP. Support for many more is coming.

* **Audit application server configurations.** DevAudit audits the server version and the server configuration for the OpenSSH sshd, Apache httpd, MySQL, and Nginx servers with many more coming. Configuration auditing is based on the [Alpheus](https://github.com/allisterb/Alpheus) library and is done using full syntactic analysis of the server configuration files with easy-. Server configuration rules are stored in YAML text files and can be customized to the needs of developers. Support for many more servers and applications and types of analysis is coming.

* **Audit application configuration.** DevAudit audits Microsoft ASP.NET applications and detects vulnerabilities present in the application configuration.
 
* **Audit application code by static analysis.** DevAudit currently supports static analysis of .NET CIL bytecode. Analyzers reside in external script files and can be fully customized based on the needs of the developer. Support for C# source code analysis via Roslyn, PHP7 source code and many more languages and external static code analysis tools is coming.

* **Remote agentless auditing**. DevAudit can connect to remote hosts via SSH with identical auditing features available in remote environments as in local environments. Only a valid SSH login is required to audit remote hosts and DevAudit running on Windows can connect to and audit Linux hosts over SSH. Support for other remote protocols like WinRM is planned.

* **Docker container auditing.** DevAudit can audit Docker containers with identical features available in container environments as in local environments.

* **PowerShell** support. DevAudit can also be run inside the PowerShell system administration environment as cmdlets. Work on PowerShell support is paused at present but will resume in the near future with support for cross-platform Powershell both on Windows and Linux.

##Requirements
DevAudit is a .NET 4.6 application. You will need either the Microsoft .NET Framework 4.6 runtime on Windows, or Mono 4.4+ on Linux. .NET 4.6 should be already installed on most recent versions of Windows, if not then it is available as a Windows feature that can be turned on or installed from the Programs and Features control panel applet on consumer Windows, or from Add Roles and Features option in Server Manager on server versions of Windows. For older versions of Windows, the .NET 4.6 installer from Microsoft can be found [here](https://www.microsoft.com/en-us/download/details.aspx?id=48130).

On Linux You must have a recent version (4.4.* or higher) of Mono. Check that the existing Mono packages provided by your distro are at least for Mono version 4.4 and above, otherwise you may have to install Mono packages manually.  Installation instructions for the most recent packages provided by the Mono project for several major Linux distros is [here](http://www.mono-project.com/docs/getting-started/install/linux/) It is recommended you have the mono-devel package installed as this will reduce the chances of missing assemblies.
 

## Installation
DevAudit can be installed by the following methods:
1. Building from source.
2. On Windows or Linux using A release archive file downloaded from Github.
3. On Windows using the release MSI installer downloaded from Github.
4. On Windows using the Chocolatey package manager.
5. On Linux by pulling the DevAudit image from Docker Hub.

### Building from source on Linux
1. Pre-requisites: Mono 4.4+ and the mono-devel package which provides the compiler and other tools needed for building Mono apps. Check that the existing Mono packages provided by your distro are at least Mono version 4.4 and above, otherwise you may have to install Mono packages manually.  Installation instructions for the most recent packages provided by the Mono project for several major Linux distros are [here](http://www.mono-project.com/docs/getting-started/install/linux/)

2. Clone the DevAudit repository from https://github.com/OSSIndex/DevAudit.git

3. Run the `build.sh` script in the root DevAudit directory. DevAudit should compile without any errors.

4. Run `./devaudit --help` and you should see the DevAudit version and help screen printed.

### Building from source on Windows
1. Pre-requisites: You must have one of:
    * A [.NET Framework 4.6](http://getdotnet.azurewebsites.net/target-dotnet-platforms.html) SDK or developer pack.
    * Visual Studio 2015.
2. Clone the DevAudit repository from https://github.com/OSSIndex/DevAudit.git

3. Run the `build.cmd` script in the root DevAudit directory. DevAudit should compile without any errors.

4. Run `./devaudit --help` and you should see the DevAudit version and help screen printed.

### Installing from the release archive files on Windows on Linux
2. Download the latest release archive file for Windows e.g DevAudit-0.2.0-Windows.zip or for Linux e.g DevAudit-0.2.0-Linux.tgz from the project [releases](https://github.com/OSSIndex/DevAudit/releases) page. Unpack this file to a directory.

2. From the directory where you unpacked the release archive run devaudit --help on Windows or ./devaudit --help on Linux. You should see the version and help screen printed.

3. Add the DevAudit installation directory to your PATH environment variable

### Installing using the MSI Installer on Windows
The MSI installer for a release can be found on the releases page.

1. Click on the [releases](https://github.com/OSSIndex/DevAudit/releases) link near the top of the page.
2. Identify the release you would like to install.
3. A "DevAudit.exe" link should be visible for each release that has a pre-built installer.
4. Download the file and execute the installer. You will be guided through a simple installation.
6. Open a *new* command prompt or PowerShell window in order to have DevAudit in path.
7. Run DevAudit.

### Installing on Windows using Chocolatey
DevAudit is also available on [Chocolatey](https://chocolatey.org/packages/devaudit/1.0.1)

1. Install [Chocolatey](https://chocolatey.org).
2. Open an admin console or PowerShell window.
3. Type `choco install devaudit`
4. Run DevAudit.

Concepts
---
####Audit Target
Represents a logical group of auditing functions. DevAudit currently supports the following audit targets:

- **Package Source**. A package source manages application and library dependencies using a package manager. Package managers install, remove or update applications and library dependencies for an operating system like Debian Linux, or for a development langauge or framework like .NET or nodejs. Examples of package sources are dpkg, yum, Chocolatey, Composer, and Bower. DevAudit audits the names and versions of installed packages against vulnerabilities reported for specific versions of those packages.
- **Application**. An application like Drupal or a custom application built using a framework like ASP.NET. DevAudit audits applications and application modules and plugins against vulnerabilities reported for specific versions of application binaries and moddules and plugins. DevAudit can also audit application configurations for known vulnerabilities, and perform static analysis on application code looking for known weaknesses. 
- **Application Server**. Application servers provide continuously running services or daemons like a web or database server for other applications to use, or for users to access services like authentication. Examples of application servers are the OpenSSH sshd and Apache httpd servers. DevAudit can audit application server binaries, modules and plugins against vulnerabilities reported for specific versions as well as audit server configurations for known server configuration vulnerabilities and weaknesses.

####Audit Environment
Represents a logical environment where audits against audit targets are executed. Audit environments abstract the I/O and command executions required for an audit and allow identical functions to be performed against audit targets on whatever physical or network location the target's files and executables are located. There are 3 audit environments currently supported by DevAudit:

- **Local**. This is the default audit environment where audits are executed on the local machine.
- **SSH**. Audits are executed on a remote host connected over SSH. It is not necessary to have DevAudit installed on the remote host.
- **Docker**. Audits are executed on a Docker container image. It is not necessary to have DevAudit installed on the container image.

####Audit Options
These are different options that can be enabled for the audit. You can specify options that apply to the DevAudit program for example, to run in non-interactive mode, as well as options that apply to the target e.g if you set the AppDevMode option for auditing ASP.NET applications to true then certain audit rules will not be enabled. 

Basic Usage
---
The CLI is the primary interface to the DevAudit program and is suitable both for interactive use and for non-interactive use in scheduled tasks, shell scripts, CI build pipelines and post-build tasks in developer IDEs. The basic DevAudit CLI syntax is:

	devaudit TARGET [ENVIRONMENT] | [OPTIONS]
where `TARGET` specifies the audit target `ENVIRONMENT` specifies the audit environment and `OPTIONS` specifies the options for the audit target and environment. There are 2 ways to specify options: program options and general audit options that apply to more than one target can be specified directly on the command-line as parameters . Target-specific options can be specified with the `-o` options using the format:
`-o OPTION1=VALUE1,OPTION2=VALUE2,....`

When specifying file paths, an @ prefix before a path indicates to DevAudit that this path is relative to the root directory of the audit target e.g if you specify: 
`-r c:\myproject -b @bin\Debug\app2.exe`
DevAudit considers the path to the binary file as c:\myproject\bin\Debug\app2.exe.
Audit Targets
---
####Package Sources
- `msi`    Do a package audit of the Windows Installer MSI package source on Windows machines.

- `choco`  Do a package audit of packages installed by the Choco package manager.

- `oneget` Do a package audit of the OneGet package source on Windows.

- `nuget` Do a package audit of a NuGet v2 package source. You must specify the location of the NuGet packages.config file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched.
- `bower` Do a package audit of a Bower package source. You must specify the location of the Bower packages.json file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched.

- `composer` Do a package audit of a Composer package source. You must specify the location of the Composer composer.json file you wish to audit using the `-f` or `--file` option otherwise the current directory will be searched.

For every package source the following general audit options can be used:
- `-f --file` Specify the location of the package manager configuration file if needed. The NuGet, Bower and Composer package sources require this option.
- `--list-packages` Only list the packages in the package source scanned by DevAudit.

- `--list-artifacts` Only list the artifacts found on OSS Index for packages scanned by DevAudit.

####Applications
- `aspnet` Do an application audit on a ASP.NET application. The relevant options are:
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level application directory that contains files like Global.asax and Web.config.
	- `-b --application-binary` Specify the application binary. The is the .NET assembly that contains the application's .NET bytecode. This file is usually a .DLL and located in the bin sub-folder of the ASP.NET application root directory.
	- `-c --configuration-file` or `-o AppConfig=configuration-file` Specifies the ASP.NET application configuration file. This file is usually named Web.config and located in the application root directory. You can override the default @Web.config value with this option.

- `drupal7` Do an application audit on a Drupal 7 application.
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level directory of your Drupal 7 install.

- `drupal8` Do an application audit on a Drupal 8 application.
	- `-r --root-directory` Specify the root directory of the application. This is just the top-level directory of your Drupal 8 install.

####Application Servers
- `sshd` Do an application server audit on an OpenSSH sshd-compatible server.

- `httpd` Do an application server audit on an Apache httpd-compatible server.

- `mysql` Do an application server audit on a MySQL-compatible server (like MariaDB or Oracle MySQL.)

- `nginx` Do an application server audit on a Nginx server.

This is an example command line for an application server audit:
`./devaudit httpd -i httpd-2.2 -r / -c /usr/local/apache2/conf/httpd.conf -b /usr/local/apache2/bin/httpd`
which audits an Apache Httpd server running on a Docker image named httpd-2.2.

The following are audit options common to all application servers:
- `-r --root-directory` Specifies the root directory of the server. This is just the top-level of your server filesystem where directories like /etc and /sbin are located. Usually is specified as `/` unless you require a different server root.
- `-c --configuration-file` Specifies the server configuration file e.g in the above audit the Apache configuration file is located at `/usr/local/apache2/conf/httpd.conf`. If you don't specify the configuration file DevAudit will try a default path which usually is whatever the default server configuration file path is on Ubuntu.
- `-b --application-binary` Specifies the server binary e.g in the above audit the Apache binary is located at `/usr/local/apache2/bin/httpd`. If you don't specify the binary path DevAudit will try a default path which usually is whatever the default server binary path is on Ubuntu.

Applications and application servers also support the following common options for listing the application modules or plugins scanned:
- `--list-packages` Only list the application plugins or modules scanned by DevAudit.

- `--list-artifacts` Only list the artifacts found on OSS Index for application plugins and modules scanned by DevAudit.

For example: 
`devaudit drupal7 -r D:\Apps\drupal-7.53 --list-packages`
will list the Drupal 7 modules found in the Drupal 7 application located at the path specified by -r.

###Environments

There are 3 kinds of audit environment supported: local, remote hosts over SSH, and Docker containers. Local environments are used by default when no other environment options are specified.

####SSH
The SSH environment allows audits to be performed on any remote hosts accessible over SSH without requiring DevAudit to be installed on the remote host. SSH environments are cross-platform: you can connect to a Linux remote host from a Windows machine running DevAudit. An SSH environment is created by the following options:`-s SERVER -u USER [-k KEYFILE] [-p | --password-text PASSWORD]`

`-s SERVER` Specifies the remote host or IP to connect to via SSH.
`-k KEYFILE` Specifies the OpenSSH compatible private key file to use to cinnect to the remote server.. Currently only RSA or DSA keys in files in the PEM format are supported.
`-p` Provide a prompt with local echo disabled for interactive entry of the server password or key file passphrase.
`--password-text PASSWORD` Specify the server password or key file passphrase as plaintext on the command-line.

####Docker
This section discusses how to **audit** Docker images using DevAudit installed on the local machine. For **running** DevAudit as a containerized Docker app see the section below on Docker Usage.

A Docker audit environment is specified by the following option: `-i CONTAINER_NAME | -i CONTAINER_ID`

`CONTAINER_(NAME|ID)` Specifes the name or id of a running Docker container to connect to. The container must be already running as DevAudit does not know how to start the container with the name or the state you require.  


### Program Options
`-n --non-interactive` Run DevAudit in non-interactive mode with all interactive features and animations of the CLI disabled. This mode is necessary for running DevAudit in shell scripts for instance otherwise errors will occure when DevAudit attempts to use interactive console features. 

`-d --debug` Run DevAudit in debug mode. This will print a variety of informational and diagnostic messages. This mode is used for troubleshooting DevAudit errors and bugs.

Docker Usage
---
DevAudit also ships as a Docker containerized app which allows users on Linux to run DevAudit without the need to install Mono and build from source. To pull the DevAudit Docker image from Docker Hub:

`docker pull ossindex/devaudit`

The current image is around 370MB uncompressed. To run DevAudit as a containerized app:

	docker run -i -t ossindex/devaudit TARGET [ENVIRONMENT] | [OPTIONS]

The -i and -t Docker options are necessary for running DevAudit interactively. If you don't specify these options then you must run DevAudit in non-interactive mode by using the DevAudit option -n. 

You must mount any directories on the Docker host machine that DevAudit needs to access on the DevAudit Docker container using the Docker -v option. If you mount your local root directory at a mount point named /hostroot on the Docker image then DevAudit can access files and directories on your local machine using the same local paths. For example:

`docker run -i -t -v /:/hostroot:ro ossindex/devaudit netfx -r /home/allisterb/vbot-debian/vbot.core`

will allow the DevAudit Docker container to audit the local directory /home/allisterb/vbot-debian/vbot.core. You _must_ mount your local root in this way to audit _other_ Docker containers from the DevAudit container e.g. 

`docker run -i -t -v /:/hostroot:ro ossindex/devaudit mysql -i myapp1 -r / -c /etc/my.cnf --skip-packages-audit`

will run a MySQL audit on a Docker container named `myapp1` from the `ossindex/devaudit` container.

If you do not need to mount your entire root directory then you can mount just the directory needed for the audit. For example:

`docker run -i -t -v /home/allisterb/vbot-debian/vbot.core:/vbot:ro ossindex/devaudit netfx -r /vbot -b @bin/Debug/vbot.core.dll`

will mount read-only the `/home/allisterb/vbot-debian/vbot.core` directory as `/vbot` on the DevAudit container which allows DevAudit to access it as the audit root directory for a netfx application audit at `/vbot`.

If you wish to use private key files on the local Docker host for an audit over SSH, you can mount your directory that contains the needed key file and then tell DevAudit to use that file path e.g.

`docker -i -t -v /home/allisterb/.ssh:/ssh:ro run ossindex/devaudit dpkg -s localhost -u allisterb -p -k /ssh/mykey.key`

will mount the directory containing key files at /ssh and allow the DevAudit container to use them.

Note that it's currently not possible for the Docker container to audit operating system package sources like dpkg or rpm or application servers like OpenSSH sshd on the _local_ Docker host without mounting your local root directory at `/hostroot` as described above. DevAudit must chroot into your local root directory from the Docker container when running executables like dpkg or server binaries like sshd and httpd. You must also mount your local root as described above to audit other Docker containers from the DevAudit container as DevAudit also needs to chroot into your local root to execute local Docker commands to communicate with your other containers.


For running audits over SSH from the DevAudit container it is not necessary to mount the local root at `/hostroot`.

