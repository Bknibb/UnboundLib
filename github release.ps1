param (
    [Parameter(Mandatory = $true)]
    [string]$Version,          # e.g. "v4.0.1"

    [Parameter(Mandatory = $true)]
    [string]$Description,      # e.g. "Improved performance, bug fixes"

    [string]$ProjectPath = "./UnboundLib/UnboundLib.csproj",   # Change this to your actual .csproj
    [string]$DllName = "UnboundLib.dll",             # Name of the built DLL to attach
	[string]$Repo = "Bknibb/UnboundLib"
)

# Ensure we're using a clean version string
if ($Version -notmatch '^v\d+\.\d+\.\d+$') {
    Write-Error "Version must follow the format vX.Y.Z (e.g. v1.2.3)"
    exit 1
}

# Check version in Unbound.cs
$unboundCsPath = "./UnboundLib/Unbound.cs"
if (-not (Test-Path $unboundCsPath)) {
    Write-Error "❌ Unbound.cs file not found at path $unboundCsPath"
    exit 1
}

$csContent = Get-Content $unboundCsPath -Raw
if ($csContent -match 'public\s+const\s+string\s+Version\s*=\s*"(?<csVersion>\d+\.\d+\.\d+)"') {
    $csVersion = $matches['csVersion']
    $cleanInputVersion = $Version.TrimStart("v")

    if ($csVersion -ne $cleanInputVersion) {
        Write-Error "❌ Version mismatch: script version is '$cleanInputVersion' but Unbound.cs has '$csVersion'"
        exit 1
    }

    Write-Host "✅ Version match confirmed: $csVersion"
} else {
    Write-Error "❌ Could not find a version declaration in Unbound.cs"
    exit 1
}

Write-Host "🔧 Building project..."
dotnet build $ProjectPath -property:SolutionDir=$PWD\
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

$octokitPath = Get-ChildItem -Recurse -Path $outputDir -Filter "Octokit.dll" | Select-Object -First 1

if (-not $octokitPath) {
    Write-Error "❌ DLL 'Octokit.dll' not found in build output."
    exit 1
}

# Commit and tag
Write-Host "🏷️ Creating git tag '$Version'..."
git tag $Version
git push origin $Version

$Description = $Description + "\n\n(Put all dlls in the plugins folder)"
$Description = $Description -replace "\\n", "`n"



# Create GitHub release with asset
Write-Host "🚀 Creating GitHub release..."
gh release create $Version "$dllPath" "./Assemblies/MMHOOK_Assembly-CSharp.dll" "$octokitPath" `
    --title "Release $Version" `
    --notes "$Description" `
	--repo "$Repo"

Write-Host "✅ Release $Version created successfully with attached DLLs."
