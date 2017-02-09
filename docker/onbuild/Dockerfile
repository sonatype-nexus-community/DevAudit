FROM ossindex/devaudit-mono:latest

MAINTAINER Allister Beharry <allister.beharry@gmail.com>

ONBUILD RUN mkdir -p /usr/src/app/source /usr/src/app/build/DevAudit.CommandLine /usr/src/app/build/Analyzers /usr/src/app/build/Rules /usr/src/app/build/Examples
ONBUILD WORKDIR /usr/src/app/build
ONBUILD COPY ./DevAudit.CommandLine /usr/src/app/build/DevAudit.CommandLine
ONBUILD COPY ./Analyzers /usr/src/app/build/Analyzers
ONBUILD COPY ./Rules /usr/src/app/build/Rules
ONBUILD COPY ./Examples /usr/src/app/build/Examples
ONBUILD COPY ./chunky.flf /usr/src/app/build
ONBUILD COPY ./LICENSE /usr/src/app/build
ONBUILD COPY ./devaudit /usr/src/app/build
 
