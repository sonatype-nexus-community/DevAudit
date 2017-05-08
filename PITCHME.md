#HSLIDE

### DevAudit
### Open-source cross-platform multi-purpose security auditing for developers
---

### Motivation
>Only a quarter to a half of organizations do what their own programmers say is needed for the security of their code: automated code scans, peer security code reviews, and further code reviews by security experts. 

...

>Most developers do not use tools for improving software quality. In
large part, this is because they lack the budget to acquire them. One
part of the problem here was addressed in the previous point: lack‐
ing adequate tools, programmers simply will not be able to maintain
code quality at the level they would like to. Beyond that, however,
lies a deeper issue for organizations, namely that they are not dedi‐
cating enough resources—or, in some cases, communicating the
availability of those resources—to enable their development teams
to deliver high-quality code using a consistent, empirical methodol‐
ogy.

...

>more than 70 percent
of survey respondents reported that they have no budget reserved
for code quality tools—not even a few dollars per month. SIG’s
hands-on experience with development teams in organizations
viii | Prefaceacross a range of sizes and sectors shows clearly that use of the right
tools and methodologies for code quality has a marked impact on
the performance, stability, security, and maintainability of enterprise
software. In general, paying attention to code quality is the best way
to make software “future-proof.”

"Improving Code Quality: A Survey of Tools, Trends, and Habits
Across Sofware Organizations" O'Reilly/SIG
---

### Features

- A lightweight, fluent Java library
- For calling APIs on the Amazon Web Service API Gateway
- Inside any application running on the JVM


+++

### AWSGateway

<span style="color:gray">A handle that represents an API on the AWS API Gateway.</span>

```Java
AWSGateway gateway = AWS.Gateway(echo-api-key)
                        .stage("beta")
                        .region(AWS.Region.OREGON)
                        .build();
```
