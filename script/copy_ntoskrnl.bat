@echo off
powershell -Command "Start-Process cmd -Verb RunAs -ArgumentList '/k cd /d %CD% && copy C:\Windows\System32\ntoskrnl.exe .\ntoskrnl.exe && exit'"
@echo on
