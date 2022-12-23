Param (
    [string] $NugetKey,
    [switch] $Publish
)

dotnet publish src/Wivuu.AspNetCore.APIKey -c Release

# Find first nupkg file in directory
$nupkg = Get-ChildItem -Path "src/Wivuu.AspNetCore.APIKey/bin/Release" -Filter *.nupkg | Select-Object -First 1

if ($Publish -and $NugetKey) {
    dotnet nuget push `
        $nupkg `
        --api-key $NugetKey `
        --source https://api.nuget.org/v3/index.json
}

# Copy to artifacts
New-Item -ItemType Directory -Path ./artifacts -Force
Copy-Item $nupkg -Destination ./artifacts/

return $nupkg