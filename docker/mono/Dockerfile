FROM debian:sid-slim

#based on Dockerfile by Jo Shields <jo.shields@xamarin.com>

MAINTAINER Allister Beharry <allister.beharry@gmail.com>

RUN apt-get update \
  && apt-get install -y curl apt-transport-https dirmngr gnupg gnupg2 gnupg1 \
  && rm -rf /var/lib/apt/lists/*

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb https://download.mono-project.com/repo/debian preview-stretch main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
  && apt-get update \
  && apt-get install -y binutils mono-devel ca-certificates-mono \
  && rm -rf /var/lib/apt/lists/* /tmp/*
  
