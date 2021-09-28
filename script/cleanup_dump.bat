@echo off
powershell -Command "Start-Process cmd -Verb RunAs -ArgumentList '/k cd /d %CD% && del /q C:\Windows\MEMORY.DMP && exit'"
@echo on
