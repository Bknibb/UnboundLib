param(
    [Parameter(Mandatory)]
    [System.String]$Version,
    
    [System.String]$TargetPath = "./UnboundLib/bin/Release/net472",
    
    [System.String]$TargetAssembly = "UnboundLib.dll",
    
    [System.String]$ProjectPath = "./"
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ProjectPath"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Go
Write-Host "Publishing for $Target from $TargetPath"

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

$package = "$ProjectPath\release"

# Release package for ThunderStore
if($name.Equals("UnboundLib")) {
    Write-Host "Packaging for ThunderStore"
    New-Item -Type Directory -Path "$package\Thunderstore" -Force
    $thunder = New-Item -Type Directory -Path "$package\Thunderstore\package"
    $thunder.CreateSubdirectory('plugins')
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$thunder\plugins\"
	Copy-Item -Path "$TargetPath\Octokit.dll" -Destination "$thunder\plugins\"
	Copy-Item -Path "$(Get-Location)\Assemblies\MMHOOK_Assembly-CSharp.dll" -Destination "$thunder\plugins\"
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$thunder\README.md"
    Copy-Item -Path "$ProjectPath\manifest.json" -Destination "$thunder\manifest.json"

    ((Get-Content -path "$thunder\manifest.json" -Raw) -replace "#VERSION#", "$Version") | Set-Content -Path "$thunder\manifest.json"

    Remove-Item -Path "$package\Thunderstore\$name.*.zip" -Force
    Copy-Item -Path "$(Get-Location)\icon.png" -Destination "$thunder\icon.png"
    Compress-Archive -Path "$thunder\*" -DestinationPath "$package\Thunderstore\$name.$Version.zip" -Force
    $thunder.Delete($true)
	
	#Write-Host "Uploading to Thunderstore"
	#$token = Get-Content "$(Get-Location)\thunderstore.token"
	#tcli publish --file "$package\Thunderstore\$name.$Version.zip" --token $token
}

Copy-Item -Path "$TargetPath\$name.dll" -Destination "$package\$name.$Version.dll"

Pop-Location
