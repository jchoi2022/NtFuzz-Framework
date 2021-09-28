@echo off
powershell -Command "Start-Process cmd -Verb RunAs -ArgumentList '/k cd /d %CD% && move C:\Windows\MEMORY.DMP \\vboxsrv\task\crash\%1.dmp && del /q C:\Windows\MEMORY.DMP && exit'"
@echo on
