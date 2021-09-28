#!/bin/bash
VBoxManage startvm $1 && echo "" > $PWD/tasks/$1/start.txt
