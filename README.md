dotnet build .\MinttiCLI\MinttiCLI.csproj -c Release -p:Platform=x64


cd C:\Users\admin\Desktop\stethoscope
dotnet build .\MinttiCLI\MinttiCLI.csproj -c Release -p:Platform=x64
cd .\MinttiCLI\bin\x64\Release\net48
.\MinttiCLI.exe -connect -mac c6:38:8f:83:54:80


$u = "https://github.com/lucasg/Dependencies/releases/download/v1.11.1/Dependencies_x64_Release.zip"
Invoke-WebRequest $u -OutFile deps.zip; Expand-Archive deps.zip -DestinationPath deps -Force
.\deps\Dependencies.exe -imports .\MinttiAlgo.dll


.\MinttiCLI.exe -connect -mac c6:38:8f:83:54:80 -verbose > out.txt 2> err.txt

Get-Content .\err.txt
Get-Content $env:TEMP\mintti_native.log