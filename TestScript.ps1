Param([string]$Configuration, [switch]$FunctionalTests)

if (-not $Configuration) {
    $Configuration = "Release"
}

$testDllNames = "GoogleAnalyticsUtilsTests.dll",
    "GoogleCloudExtensionUnitTests.dll",
    "GoogleCloudExtension.Utils.UnitTests.dll",
    "GoogleCloudExtension.DataSources.UnitTests.dll"

if ($env:APPVEYOR_SCHEDULED_BUILD -or $FunctionalTests) {
    # Don't run functional tests on triggered (PR) builds.
    $testDllNames += "ProjectTemplate.Tests.dll"
}

$testDlls = ls -r -include $testDllNames | ? FullName -Like *\bin\$Configuration\*

$testContainerArgs = $testDlls.FullName -join " "

if ($env:APPVEYOR) {
    $testArgs = $testArgs = "$testContainerArgs /logger:Appveyor /inisolation /diag:logs\log.txt"
} else {
    $testArgs = $testContainerArgs
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

if ($LASTEXITCODE) {
    Get-ChildItem logs -Include *.txt -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
	Get-ChildItem -Path C:/Users/appveyor/AppData/Local/CrashDumps -Force | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
	# Add the .pdbs and .dlls when it crashes so as to be able to debug the .dmp properly.
	Get-ChildItem -Path C:/projects/google-cloud-visualstudio/TestResults/Deploy_appveyor*/Out *.dll -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
	Get-ChildItem -Path C:/projects/google-cloud-visualstudio/TestResults/Deploy_appveyor*/Out *.pdb -Force -Recurse | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }
    throw "Test failed with code $LASTEXITCODE"
}
Write-Host "Finished code coverage."
