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
