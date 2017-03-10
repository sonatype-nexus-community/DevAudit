#!/bin/bash
if [[ $# -lt 4 ]]; then
	echo Usage is ./build-release.sh major minor patch build [special].	
	exit 1
fi
MAJOR=$1
MINOR=$2
PATCH=$3
BUILD=$4
if [[ $# -eq 5 ]]; 
then
	SPECIAL="-$5"
else
	SPECIAL=""
fi
TAG=v$MAJOR.$MINOR.$PATCH
BUILD_DIR=DevAudit.$TAG.Build
if [ -d "$BUILD_DIR" ]; then
	echo Deleting $BUILD_DIR.
	rm -rf "$BUILD_DIR"
fi
git clone --branch "$TAG" https://github.com/OSSIndex/DevAudit $BUILD_DIR
if [[ $? -ne 0 ]]; then
	echo An error occurred during checkout.
	exit 1
fi
if [ ! -f $BUILD_DIR/DevAudit.Mono.sln ]; then
	echo Could not find the solution file $BUILD_DIR/DevAudit.Mono.sln. An error may have occurred during git checkout.	
	exit 1
fi
RELEASE_DIR=$MAJOR.$MINOR.$PATCH.$BUILD$SPECIAL
if [ -d "$RELEASE_DIR" ]; then
	echo Deleting $RELEASE_DIR.
	rm -rf "$RELEASE_DIR"
fi
mkdir $RELEASE_DIR
mkdir $RELEASE_DIR/DevAudit
nuget restore $BUILD_DIR/DevAudit.Mono.sln && xbuild $BUILD_DIR/DevAudit.Mono.sln /verbosity:diagnostic /p:Configuration=RuntimeDebug /p:VersionAssembly=$MAJOR.$MINOR.$PATCH.$BUILD
if [[ $? -ne 0 ]]; then
	echo An error occurred during build.
    rm -rf "$BUILD_DIR"
    rm -rf "$RELEASE_DIR"
	exit 1
fi
cp $BUILD_DIR/DevAudit.AuditLibrary/bin/Debug/Gendarme.Rules.* ./DevAudit.CommandLine/bin/Debug/
cp $BUILD_DIR/BuildCommon/devaudit-run-linux.sh $RELEASE_DIR/DevAudit
mv $RELEASE_DIR/DevAudit/devaudit-run-linux.sh $RELEASE_DIR/DevAudit/devaudit 
chmod +x $RELEASE_DIR/DevAudit/devaudit
cp -R $BUILD_DIR/DevAudit.CommandLine/bin/Debug/* $RELEASE_DIR/DevAudit 
tar -cvzf DevAudit-$RELEASE_DIR.tgz $RELEASE_DIR/DevAudit
rm -rf "$BUILD_DIR"
rm -rf "$RELEASE_DIR"