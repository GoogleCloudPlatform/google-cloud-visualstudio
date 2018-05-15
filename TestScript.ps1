Param([string]$Configuration, [switch]$FunctionalTests, $VerbosePreference)

if (-not $Configuration) {
	$Configuration = "Release"
}

$testDllNames = "GoogleAnalyticsUtilsTests.dll",
	"GoogleCloudExtensionUnitTests.dll",
	"GoogleCloudExtension.Utils.UnitTests.dll",
	"GoogleCloudExtension.DataSources.UnitTests.dll",
	"GoogleCloudExtension.Deployment.UnitTests.dll"

if ($env:APPVEYOR_SCHEDULED_BUILD -or $FunctionalTests) {
	# Don't run functional tests on triggered (PR) builds.
	$testDllNames += "ProjectTemplate.Tests.dll"
}

$testDlls = ls -r -include $testDllNames | ? FullName -Like *\bin\$Configuration\*

$testAdapterName = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll"
$testAdapterPath = ls .\GoogleCloudExtension\packages -Recurse -Include $testAdapterName |
	Sort-Object LastWriteTime -Descending |
	Select-Object -First 1 | Split-Path -Parent

$testContainerArgs = $testDlls.FullName -join " "
$testAdapterArg = "/TestAdapterPath:`"$testAdapterPath`""
$testArgs = $testContainerArgs, "/inisolation",  "/diag:logs\log.txt", "/TestCaseFilter:`"TestCategory!=OFF`"", $testAdapterArg -join " "

if ($env:APPVEYOR) {
	$testArgs = "$testArgs /logger:Appveyor"
}

$testFilters = ($testDlls.BaseName | % { "-[$_]*"}) -join " "

$filter = $testFilters,
	"+[GoogleCloudExtension*]*",
	"+[GoogleAnalyticsUtils*]*",
	"-[*]XamlGeneratedNamespace*",
	"-[*]GoogleCloudExtension*.Resources" -join " "

Write-Verbose "Running OpenCover.Console.exe -register:user -target:vstest.console.exe -targetargs:$testArgs -output:codecoverage.xml `
	-filter:$filter -returntargetcode"

OpenCover.Console.exe -register:user -target:vstest.console.exe -targetargs:$testArgs -output:codecoverage.xml `
	-filter:$filter -returntargetcode

if ($LASTEXITCODE -and $env:APPVEYOR) {
	Get-ChildItem logs -Include *.txt -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
	# Only if vstest crashed push the dumps and symbols files.
	# $LASTEXITCODE is 1 when tests fail, but that doesn't mean that vstest crashed.
	if (Test-Path -Path C:/Users/appveyor/AppData/Local/CrashDumps){
		Get-ChildItem -Path C:/Users/appveyor/AppData/Local/CrashDumps -Force | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
		# Add the .pdbs and .dlls when it crashes so as to be able to debug the .dmp properly.
		Get-ChildItem -Path C:/projects/google-cloud-visualstudio/TestResults/Deploy_appveyor*/Out *.dll -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
		Get-ChildItem -Path C:/projects/google-cloud-visualstudio/TestResults/Deploy_appveyor*/Out *.pdb -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
	}
	throw "Test failed with code $LASTEXITCODE"
}
Write-Host "Finished code coverage."
