#!/bin/bash
vboxmanage showvminfo $1 | grep State | grep abort
