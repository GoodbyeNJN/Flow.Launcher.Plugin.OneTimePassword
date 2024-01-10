dotnet publish

Get-Process -Name "Flow.Launcher" | Stop-Process -Force
Start-Sleep -Seconds 3

$source = "$PWD\artifacts\publish\Flow.Launcher.Plugin.OneTimePassword\release"
$target = "$env:APPDATA\FlowLauncher\Plugins\One Time Password-1.0.0"

Remove-Item -Path $target -Recurse -Force
Copy-Item -Path $source -Destination $target -Recurse

& "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"
