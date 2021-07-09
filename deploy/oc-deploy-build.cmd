@echo off
@setlocal
set ERROR_CODE=0

pushd C:\Projects\Micah\src\Micah.Web
if not %ERRORLEVEL%==0  (
    echo Could not cd to C:\Projects\Micah\src\Micah.Web.
    set ERROR_CODE=1
    goto End
)
dotnet publish -c Debug /p:MicrosoftNETPlatformLibrary=Microsoft.NETCore.App
if not %ERRORLEVEL%==0  (
    echo Could not build project at C:\Projects\Micah\src\Micah.Web.
    set ERROR_CODE=1
    popd
    goto End
)
oc start-build Micah --from-dir=bin\Debug\netcoreapp3.1\publish
if not %ERRORLEVEL%==0  (
    echo Could not start build on OpenShift.
    set ERROR_CODE=1
    popd
    goto End
)
echo Deploy succeded.
popd

:End
@endlocal
exit /B %ERROR_CODE%
