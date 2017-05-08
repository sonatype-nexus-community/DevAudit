#HSLIDE
### DevAudit
#### Cross-platform multi-purpose security auditing for developers
![Screenshot of DevAudit auditing a Docker container](https://lh3.googleusercontent.com/id4IBEf25ugvSjZOxF7KVY8CDoWLNlbb5dyHQ8R6wabtFpMjwrVl_EIJNnoPobzo2UCJj5vghOac8phJ2UKLciqCljB1ioUQ682dRTgOYOgkfLBZLrxVM2lcHGJX7wnQpyqyWw=w900-h450-no)
---

### About
DevAudit is an open-source auditing tool targeted at developers and DevOps practitioners that detects security vulnerabilities at multiple levels of the solution stack. DevAudit provides a wide array of auditing capabilities that automate security practices and implementation of security auditing in the software development life-cycle.
---

### Motivation
From ["Improving Code Quality: A Survey of Tools, Trends, and Habits Across Sofware Organizations."](https://www.sig.eu/wp-content/uploads/2017/04/Improving-Code-Quality-.pdf) SIG/O'Reilly. 2017.

>Only a quarter to a half of organizations do what their own programmers say is needed for the security of their code: automated code scans, peer security code reviews, and further code reviews by security experts. 
+++

### Motivation (SIG/O'REILLY)
>Most developers do not use tools for improving software quality. In
large part, this is because they lack the budget to acquire them. One
part of the problem here was addressed in the previous point: lack‐
ing adequate tools, programmers simply will not be able to maintain
code quality at the level they would like to.
+++

### Motivation (SIG/O'REILLY)
>more than 70 percent of survey respondents reported that they have no budget reserved
for code quality tools—not even a few dollars per month...use of the right
tools and methodologies for code quality has a marked impact on
the performance, stability, security, and maintainability of enterprise
software. 
---

### Features
![Screenshot](https://lh3.googleusercontent.com/tnqX8jGJ1WW2vimXHwlKxBTOuP2bvsPVgzq92isPg0ditWoQdsDzh3_PEbMiOldinezQeSqGsQmMSaXE_2tKJ2fzGSeAH7OnNZBofZXom1U6PS8RX4jd_h5DfF1How_xwsRDIw=w812-h608-no)
+++

### Features: Overview

- CLI interface that can be easily integrated into CI build pipelines. 

- Uses the [OSS Index API](https://ossindex.net/) which provides continuously updated vulnerabilities data.

- Agentless remote-auditing: audit remote hosts via SSH or WinRM.

- Audit Docker containers. 
+++

### Features: Package Dependencies Auditing

![Screenshot of DevAudit package source audit](https://lh3.googleusercontent.com/tR98RwJops5G97vjm6-lXWHAxAhLA_pvan7qKF9wrxJttPt6C8VW9kGnruvPnUJ7q1jV2exWGH9w=w1382-h957-no)
+++

### Features: Package Dependencies Auditing
DevAudit audits Windows applications and pacakges installed via Chocolatey, Windows MSI and OneGet for vulnerabilities reported for specific versions. For development package dependencies and libraries, DevAudit audits NuGet v2 dependencies for .NET, Bower dependencies for nodejs, and Composer package dependencies for PHP. Support for many more is coming.
+++

### Features: Application Server Configuration Auditing
![Screenshot of DevAudit configuration audit]
+++

### Features: Application Server Configuration Auditing
(https://lh3.googleusercontent.com/tnqX8jGJ1WW2vimXHwlKxBTOuP2bvsPVgzq92isPg0ditWoQdsDzh3_PEbMiOldinezQeSqGsQmMSaXE_2tKJ2fzGSeAH7OnNZBofZXom1U6PS8RX4jd_h5DfF1How_xwsRDIw=w812-h608-no)
DevAudit audits the server version and the server configuration for the OpenSSH sshd, Apache httpd, MySQL, and Nginx servers with many more coming. Configuration auditing is based on the [Alpheus](https://github.com/allisterb/Alpheus) library and is done using full syntactic analysis of the server configuration files. Server configuration rules are stored in YAML text files and can be customized to the needs of developers. Support for many more servers and applications and types of analysis is coming.

+++

### Features: Application Configuration Auditing
![Screenshot of DevAudit ASP.NET application audit](https://lh3.googleusercontent.com/WiMC-en25YIOG5lWzPjhF6D9l3WTw5GdY_ne-LjpbQcOcaWgzg2beS3fQc1YrCVblmPo59QIZMmWk98suJjEG_CGeC1gAEfPqZbOUbm59ibTwfuxvtHSr-dwNkp8NMzl7PYHHg=w812-h608-no)
DevAudit audits Microsoft ASP.NET applications and detects vulnerabilities present in the application configuration.

+++

### Features: Application Code Static Analysis
DevAudit currently supports static analysis of .NET CIL bytecode. Analyzers reside in external script files and can be fully customized based on the needs of the developer. Support for C# source code analysis via Roslyn, PHP7 source code and many more languages and external static code analysis tools is coming.