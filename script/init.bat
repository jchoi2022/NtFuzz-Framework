:: Register this batch script to Startup folder of Windows.
:: (For Win 10, C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp)
rmdir /s /q C:\FuzzingFramework
xcopy /e /i \\vboxsrv\task\codebase C:\FuzzingFramework
cd C:\FuzzingFramework\src && dotnet run -- guest
pause
