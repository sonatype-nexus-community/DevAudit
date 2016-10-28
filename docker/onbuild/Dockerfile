FROM ossindex/devaudit-mono:latest

MAINTAINER Allister Beharry <allister.beharry@gmail.com>

ONBUILD RUN mkdir -p /usr/src/app/source /usr/src/app/build
ONBUILD WORKDIR /usr/src/app/source
ONBUILD COPY . /usr/src/app/source
ONBUILD RUN nuget sources add -Name OSSIndex -Source https://www.myget.org/F/ossindex/api/v2 \
            && nuget restore DevAudit.Mono.sln \
            && xbuild DevAudit.Mono.sln /property:Configuration=RuntimeDebug \
            && rm -rf packages \
            && rm -rf DevAudit.AuditLibrary \
            && rm -rf DevAudit.AuditLibrary.Analyzers.CSharp \
			&& rm -rf DevAudit.AuditLibrary.Tests \
			&& rm -rf Renci.SshNet \
			&& rm -rf Expect.NET 