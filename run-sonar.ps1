param(
    [string]$Token = ""
)

$ProjectKey = "DeliInventoryManagement_1"
$SonarUrl   = "http://localhost:9000"

if ([string]::IsNullOrWhiteSpace($Token))
{
    $Token = Read-Host "Enter your SonarQube token"
}

Write-Host "Starting SonarQube Analysis..." -ForegroundColor Cyan

dotnet sonarscanner begin `
    /k:"$ProjectKey" `
    /d:sonar.host.url="$SonarUrl" `
    /d:sonar.token="$Token" `
    /d:sonar.cs.opencover.reportsPaths="DeliInventoryManagement_1.Api.Tests/coverage/coverage.opencover.xml" `
    /d:sonar.exclusions="**/obj/**,**/bin/**,**/wwwroot/**,**/*.Designer.cs"

dotnet build --no-incremental

dotnet test DeliInventoryManagement_1.Api.Tests/DeliInventoryManagement_1.Api.Tests.csproj `
    --no-build `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:CoverletOutput=./coverage/

dotnet sonarscanner end /d:sonar.token="$Token"

Write-Host "Done! Open: $SonarUrl/dashboard?id=$ProjectKey" -ForegroundColor Green