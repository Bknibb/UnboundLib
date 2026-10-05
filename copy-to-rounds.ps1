param(
	[Parameter(Mandatory)]
    [System.String]$TargetPath,
    
	[Parameter(Mandatory)]
    [System.String]$TargetAssembly,
    
    [Parameter(Mandatory)]
    [System.String]$RoundsPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$RoundsPath"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

Write-Host "Updating local installation in $RoundsPath"
    
$plug = New-Item -Type Directory -Path "$RoundsPath\BepInEx\plugins\$name" -Force
Write-Host "Copy $TargetAssembly to $plug"
Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force

Pop-Location