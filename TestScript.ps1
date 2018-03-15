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
    $testArgs = $testArgs = "/logger:Appveyor $testContainerArgs /diag:diagnostics.txt"
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
    Get-Content diagnostics.txt | Write-Host
    throw "Test failed with code $LASTEXITCODE"
}
Write-Host "Finished code coverage."
