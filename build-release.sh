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
TAG=v$MAJOR.$MINOR.x
BUILD_TAG=$MAJOR.$MINOR.$PATCH.$BUILD
RELEASE_TAG=$MAJOR.$MINOR.$PATCH.$BUILD$SPECIAL
BUILD_DIR=DevAudit.$TAG.Build_$BUILD
if [ -d "$BUILD_DIR" ]; then
	echo Deleting $BUILD_DIR.
	rm -rf "$BUILD_DIR"
fi
git clone --branch "$TAG" https://github.com/OSSIndex/DevAudit $BUILD_DIR
if [[ $? -ne 0 ]]; then
	echo An error occurred during checkout of branch $TAG.
	exit 1
fi
if [ ! -f $BUILD_DIR/DevAudit.Mono.sln ]; then
	echo Could not find the solution file $BUILD_DIR/DevAudit.Mono.sln. An error may have occurred during git checkout.	
	exit 2
fi

if [ -d "$RELEASE_TAG" ]; then
	echo Deleting $RELEASE_TAG.
	rm -rf "$RELEASE_TAG"
fi
mkdir $RELEASE_TAG
mkdir $RELEASE_TAG/DevAudit
RELEASE_DIR=$RELEASE_TAG/DevAudit
nuget restore $BUILD_DIR/DevAudit.Mono.sln && xbuild $BUILD_DIR/DevAudit.Mono.sln /verbosity:diagnostic /p:Configuration=RuntimeDebug /p:VersionAssembly=$BUILD_TAG
if [[ $? -ne 0 ]]; then
	echo An error occurred during build in $BUILD_DIR.
    rm -rf "$BUILD_DIR"
    rm -rf "$RELEASE_DIR"
	exit 3
fi
cp $BUILD_DIR/DevAudit.AuditLibrary/bin/Debug/Gendarme.Rules.* $BUILD_DIR/DevAudit.CommandLine/bin/Debug/
cp -R $BUILD_DIR/DevAudit.CommandLine/bin/Debug/* $RELEASE_DIR
mkdir $RELEASE_DIR/Examples && cp -R $BUILD_DIR/Examples/* $RELEASE_DIR/Examples
copy ./README.md %RELEASE_DIR%
copy ./LICENSE %RELEASE_DIR%
cp $BUILD_DIR/BuildCommon/devaudit-run-linux.sh $RELEASE_DIR
mv $RELEASE_DIR/devaudit-run-linux.sh $RELEASE_DIR/devaudit 
chmod +x $RELEASE_DIR/devaudit
cd $RELEASE_TAG
tar -cvzf ../DevAudit-$RELEASE_TAG.tgz DevAudit
cd ..
rm -rf "$BUILD_DIR"
echo Release $RELEASE_TAG created in directory $RELEASE_DIR and archive DevAudit-$RELEASE_TAG.tgz.