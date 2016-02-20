DevAudit
==========
Identify known vulnerabilities in development packages and applications (NuGet, MSI, Chocolatey, OneGet, Bower, Drupal)

```
Usage

devaudit <package-source> [--file <file>]

package-source is one of nuget, msi, chocolatey, bower, oneget.
--file <file>
    For use with **nuget** and **bower** package sources. The file argument
    specifies the dependency file to be processed. For NuGet this would be
    "packages.config" and for bower a "bower.json" file.

-n, --non-interact
    Run in non-interactive mode. Useful for when running with a script or
    redirecting output to a file
```

Installation
============
Currently there are pre-built binaries for Windows. Linux support is in development.

Pre-built DevAudit binaries may be installed in one of three ways:

1. MSI Installer
2. Zip file
3. [Soon] Chocolatey
 
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

DevAudit will soon be available on [Chocolatey](https://chocolatey.org)

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

```
