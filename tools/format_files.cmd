@echo off
rem This file formats all of the .csproj that can be found down the
rem root given.
rem   %1, the root path where to format all of the files.

setlocal

set workspace=%~p0..

set codeformatter_rules=^
  /rule:BraceNewLine ^
  /rule:ExplicitThis ^
  /rule:ExplicitVisibility ^
  /rule:FieldNames ^
  /rule:FormatDocument ^
  /rule:ReadonlyFields ^
  /rule:UsingLocation

rem Go through all of the .csproj files formatting them.
pushd "%~1"
for /R %%i in (*.csproj) do codeformatter "%%~i" %codeformatter_rules% /nocopyright
popd
echo Done.
