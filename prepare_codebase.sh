#!/bin/bash
CURFILE=$(readlink -f "$0")
CURDIR=$(dirname "$CURFILE")
CODEBASEDIR=$CURDIR/codebase
SRCDIRNAME=src
SCRIPTDIRNAME=script

rm -rf $CODEBASEDIR
mkdir -p $CODEBASEDIR
# Copy the source and script directories.
cp -r $CURDIR/$SRCDIRNAME $CODEBASEDIR/
rm -rf $CODEBASEDIR/$SRCDIRNAME/bin
rm -rf $CODEBASEDIR/$SRCDIRNAME/obj
cp -r $CURDIR/$SCRIPTDIRNAME $CODEBASEDIR/
