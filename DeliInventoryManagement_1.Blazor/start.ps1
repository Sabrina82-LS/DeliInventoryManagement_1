Write-Host "Stopping and cleaning containers..." -ForegroundColor Yellow
docker-compose down -v

Write-Host "Building and starting..." -ForegroundColor Yellow
docker-compose up --build