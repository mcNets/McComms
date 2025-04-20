# Neteja la pantalla
Clear-Host

Write-Host "Compilant..."
Write-Host ""

dotnet build -c Release -v q

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed\n"
    Write-Host ""
    exit $LASTEXITCODE
}

# Busca la ultima versio del paquet a ./Packages
$latestVersion = Get-ChildItem -Path ./Packages -Filter "mcNuget.Comms.gRPC.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($null -eq $latestVersion) {
    Write-Host "No package found in ./Packages"
    Write-Host ""
    exit 1
}

Write-Host "Latest version: $latestVersion"
Write-Host ""

# Copia el paquet a la carpeta .nuget
Write-Host "Publicant..."

if (Test-Path -Path C:\Work\.nuget -PathType Container) {
    Write-Host "Copia el paquet a C:\Work\.nuget" -ForegroundColor Green
    Move-Item -Path .\Packages\*.nupkg -Destination C:\Work\.nuget\ -Force
}
elseif (Test-Path -Path C:\Codi\.nuget -PathType Container) {
    Write-Host "Copia el paquet a C:\Codi\.nuget" -ForegroundColor Green
    Move-Item -Path .\Packages\*.nupkg -Destination C:\Codi\.nuget\ -Force
}
elseif (Test-Path -Path F:\Codi\.nuget -PathType Container) {
    Write-Host "Copia el paquet a F:\Codi\.nuget" -ForegroundColor Green
    Move-Item -Path .\Packages\*.nupkg -Destination F:\Codi\.nuget\ -Force
}
Write-Host ""
