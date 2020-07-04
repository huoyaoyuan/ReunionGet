Write-Host "Installing tools from Release output. Please make sure you've built them."
dotnet tool uninstall -g ReunionGet.TorrentInfo
dotnet tool uninstall -g ReunionGet.BTInteractive
dotnet tool install -g ReunionGet.TorrentInfo --add-source "$PSScriptRoot\..\PackageOutput\Release\"
dotnet tool install -g ReunionGet.BTInteractive --add-source "$PSScriptRoot\..\PackageOutput\Release\"
