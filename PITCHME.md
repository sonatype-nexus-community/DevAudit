#HSLIDE
### DevAudit
#### Cross-platform multi-purpose security auditing for developers
![Screenshot of DevAudit auditing over SSH](https://lh3.googleusercontent.com/giEat4IU1U6zrRUkT2CR0NLT2bfspdQqflfwV0_4q486EgdqvDNj_-mMblSh82kF_Ptj0I8V7ydN1RZbrkdjMSlc3SSYfZyifH_8S60ghmdno-GtXnBjxFAzNvJklwPdWndFg3ihi8P4Xb8XR07TGJttS0AhxS0ySSm-nLnomZj_6oCyqBcWG_1yvldpaVJkCqz5f9lGzueLVlW9n3KpiuvZFOHGhejz7D5OhnR9tH34-6tr88lEqik4NOE5ZeqycqmX5iB2UVpzDS6piqYJ4NPZNxPcxkA7HmFOWC6NTYh0BPAU5eKw416K9fNv4YRuUp2yH3kPhu9tKq6IBnmq1t20zIUPOtDmzz_Ts-PrdL_S4npk4qG0YD9G1ifD23Jtep1Kz7-mTIlGUo9jXlzjzpZSvVE7PXCFSxLu9hyftddLAloUYqz1IWznldSGjYBkSf8ap3o7vWgP27FGDDil67UF1eRwYbE1VTmV0vS3w5B0vpHhso3OrL_I4QT2DLtYYbET5vjFZMQCnrjq4beBXviKy-vvO8QM5x9kro0_JP31YpLKkX_yVaqt2va0X_lPdF2DdFhpIjlDmoCeH3dfxF1F7Zs4lbqHOB3K90VC_nhmrbtsyGUeYnE39dgNHS9HOejDKzSfTlwAmmoC6z-jAqbN3rTdoX6dbuqML8Qbsw=w1600-h828-no)
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
### Features: Overview I

- Audit operating system package and library dependency versions for vulnerabilities.

- Audit application and application server configurations for vulnerabilities. 

- Audit application code using static analysis.

- Modular, extendable architecture: use from command line interface, Docker, web application, Visual Studio extension etc.

+++

### Features: Overview II

- CLI interface that can be easily integrated into CI build pipelines. DevAudit [Docker image](https://hub.docker.com/r/ossindex/devaudit/tags/) available.

- Uses the [OSS Index API](https://ossindex.net/) which provides continuously updated vulnerabilities data.

- Agentless remote-auditing: audit remote hosts via SSH or WinRM without DevAudit installed on remote hosts. 

- Audit Docker containers without DevAudit installed on containers. 

+++

### Features: Overview III

- Audit GitHub projects directly from GitHub repository.

- Use GitHub,GitLab,BitBucket audit reporters for reporting audit results to issue queue.

![Screenshot](https://cdn-images-1.medium.com/max/1116/1*Uj0WBK9RlS8YvN0qW-IFZQ.png)

---

### Features: Package Manager Auditing

![Screenshot of DevAudit package source audit](https://lh3.googleusercontent.com/tR98RwJops5G97vjm6-lXWHAxAhLA_pvan7qKF9wrxJttPt6C8VW9kGnruvPnUJ7q1jV2exWGH9w=w1382-h957-no)
+++

### Package Manager Auditing Overview
- Audit OS package managers and development library package managers.
- Audits package versions recorded in package manager against reported vulnerabilities from OSS Index.
- Uses [Versatile library](https://github.com/allisterb/Versatile) for comparing package versions and reducing false positives.


+++

### Package Manager Auditing : OS package managers
- Debian Dpkg
- Redhat RPM/YUM
- Windows Chocolatey 
- Windows MSI and OneGet

+++

### Package Manager Auditing : Library package managers
- Bower
- Composer
- NuGet v2
- More planned e.g. Yarn, NuGet v3
---

### Features: Application Server Configuration Auditing
![Screenshot of DevAudit configuration audit](https://lh3.googleusercontent.com/tnqX8jGJ1WW2vimXHwlKxBTOuP2bvsPVgzq92isPg0ditWoQdsDzh3_PEbMiOldinezQeSqGsQmMSaXE_2tKJ2fzGSeAH7OnNZBofZXom1U6PS8RX4jd_h5DfF1How_xwsRDIw=w812-h608-no)
+++

### Application Server Configuration Auditing : Overview
- Audits the server version and the server configuration for servers.
- Supports OpenSSH sshd, Apache httpd, MySQL, and Nginx servers with many more coming. 
- Configuration auditing is based on the [Alpheus](https://github.com/allisterb/Alpheus) library using full syntactic analysis of the server configuration files. 
- Server configuration rules are stored in YAML text files and can be customized to the needs of developers.

---

### Features: Application Configuration Auditing
![Screenshot of DevAudit ASP.NET application audit](https://lh3.googleusercontent.com/WiMC-en25YIOG5lWzPjhF6D9l3WTw5GdY_ne-LjpbQcOcaWgzg2beS3fQc1YrCVblmPo59QIZMmWk98suJjEG_CGeC1gAEfPqZbOUbm59ibTwfuxvtHSr-dwNkp8NMzl7PYHHg=w812-h608-no)

+++

### Application Configuration Auditing : Overview
- Audit application configuration for vulnerabilities.
- Supports ASP.NET applications with several more planned.



### Features: Application Code Static Analysis
DevAudit currently supports static analysis of .NET CIL bytecode. Analyzers reside in external script files and can be fully customized based on the needs of the developer. Support for C# source code analysis via Roslyn, PHP7 source code and many more languages and external static code analysis tools is coming.