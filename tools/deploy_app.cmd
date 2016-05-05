@echo off
REM Deploy the given app using the given deployment profile.
REM   %1, the path to the project.
REM   %2, where to publish the app.
REM   %3, the path to the publish settings.

setlocal

echo "Ensuring output directory exists %~2"
mkdir "%~2"

echo "Building and deploying project %1."
msbuild "%~1" ^
  /p:Configuration=Release ^
  /p:Platform=AnyCPU ^
  /t:WebPublish ^
  /p:WebPublishMethod=FileSystem ^
  /p:DeleteExistingFiles=True ^
  /p:publishUrl="%~2"

REM Should the website be specified?
set default_site="Default Web Site"
set msdeploy_path="%ProgramFiles%\IIS\Microsoft Web Deploy V3\msdeploy.exe"

echo "Deploying using the profile %3."
%msdeploy_path% ^
  -verb:sync ^
  -source:contentPath="%~2" ^
  -dest:contentPath=%default_site%,publishSettings="%~3" ^
  -allowUntrusted

echo "Done."
