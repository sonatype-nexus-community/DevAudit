FROM debian:wheezy

#based on bockerfile by Jo Shields <jo.shields@xamarin.com>

MAINTAINER Allister Beharry <allister.beharry@gmail.com>

RUN apt-get update \
  && apt-get install -y curl \
  && rm -rf /var/lib/apt/lists/*

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb http://download.mono-project.com/repo/debian wheezy/snapshots/4.6.1.3 main" > /etc/apt/sources.list.d/mono-xamarin.list \
  && apt-get update \
  && apt-get install -y binutils mono-devel ca-certificates-mono nuget \
  && rm -rf /var/lib/apt/lists/* /tmp/*
