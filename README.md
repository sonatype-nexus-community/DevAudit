DevAudit
==========
Identify known vulnerabilities in development packages and applications (NuGet, MSI, Chocolatey, OneGet, Bower, Drupal)

```
#### Usage

devaudit package-source --file [file] [--packages] [--artifacts]

package-source is one of nuget, msi, chocolatey, bower, oneget.
--file is a optional file parameter that is used by package sources like NuGet and Bower.
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
 
