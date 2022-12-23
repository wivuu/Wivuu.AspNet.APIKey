Param (
    [Parameter(Mandatory=$true)]
    [string] $NugetKey,
    [switch] $Publish
)

dotnet publish src/Wivuu.AspNetCore.APIKey -c Release

if ($Publish -and $NugetKey) {
    # Find first nupkg file in directory
    $nupkg = Get-ChildItem -Path "src/Wivuu.AspNetCore.APIKey/bin/Release" -Filter *.nupkg | Select-Object -First 1

    dotnet nuget push `
        $nupkg `
        --api-key $NugetKey `
        --source https://api.nuget.org/v3/index.json
}