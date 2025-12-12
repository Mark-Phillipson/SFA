$Env:ASPNETCORE_Kestrel__Certificates__Default__Path = 'C:\Temp\devcert.pfx'
$Env:ASPNETCORE_Kestrel__Certificates__Default__Password = 'P@ssw0rd'
Write-Host "Using PFX: $Env:ASPNETCORE_Kestrel__Certificates__Default__Path"
Write-Host "Starting project..."
dotnet run --project 'C:\Users\MPhil\source\repos\SFA\SFA_PWA\SFA_PWA.csproj' --urls 'https://localhost:5158;http://localhost:5157'