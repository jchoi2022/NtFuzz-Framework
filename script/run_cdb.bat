@echo off
powershell -Command "Start-Process -Wait cmd -Verb RunAs -ArgumentList '/k cd /d %CD% && cdb -y C:\symbols -z C:\Windows\MEMORY.DMP -logo cdb.txt -c \"!analyze -v; dd Hooker!heapPoisonValue L1; q\" && exit'"
@echo on
