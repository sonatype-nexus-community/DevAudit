# DevAudit
Identify known vulnerabilities in MSWin packages (MSI, Chocolatey, OneGet, Bower)

#### Usage

win-audit package-source --file [file] [--packages] [--artifacts]

package-source is one of nuget, msi, chocolatey, bower, oneget.
--file is a optional file parameter that is used by package sources like NuGet and Bower.
