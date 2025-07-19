param (
    [Parameter(Mandatory = $true)]
    [string]$Version,          # e.g. "v4.0.1"

    [Parameter(Mandatory = $true)]
    [string]$Description,      # e.g. "Improved performance, bug fixes"

    [string]$ProjectPath = "./UnboundLib/UnboundLib.csproj",   # Change this to your actual .csproj
    [string]$DllName = "UnboundLib.dll"             # Name of the built DLL to attach
)

# Ensure we're using a clean version string
if ($Version -notmatch '^v\d+\.\d+\.\d+$') {
    Write-Error "Version must follow the format vX.Y.Z (e.g. v1.2.3)"
    exit 1
}

Write-Host "🔧 Building project..."
dotnet build $ProjectPath -property:SolutionDir=$PWD.Path
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Build failed."
    exit 1
}

# Determine output directory
$outputDir = Join-Path -Path (Split-Path $ProjectPath -Parent) -ChildPath "bin/Debug/net472"
$dllPath = Get-ChildItem -Recurse -Path $outputDir -Filter $DllName | Select-Object -First 1

if (-not $dllPath) {
    Write-Error "❌ DLL '$DllName' not found in build output."
    exit 1
}

$octokitPath = Join-Path -Path $dllPath.Parent - ChildPath "Octokit.dll"

if (-not $octokitPath) {
    Write-Error "❌ DLL 'Octokit.dll' not found in build output."
    exit 1
}

# Commit and tag
Write-Host "🏷️ Creating git tag '$Version'..."
git tag $Version
git push origin $Version

$Description = $Description + "\n\n(Put both dlls in the plugins folder)"



# Create GitHub release with asset
Write-Host "🚀 Creating GitHub release..."
gh release create $Version "$dllPath.FullName" "./Assemblies/MMHOOK_Assembly-CSharp.dll" "$octokitPath.FullName" `
    --title "Release $Version" `
    --notes "$Description"

Write-Host "✅ Release $Version created successfully with attached DLL."
