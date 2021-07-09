@echo off
@setlocal
set ERROR_CODE=0

cd src\Micah.CLI\bin\Debug\netcoreapp3.1\
"Micah.CLI.exe" %*
goto end

:end
cd ..\..\..\..\..\
exit /B %ERROR_CODE%