@echo off
REM Notification Service Build and Deploy Script for Windows

echo 🚀 Starting Notification Service build and deployment...

REM Check if Docker is running
docker info >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Docker is not running. Please start Docker and try again.
    exit /b 1
)

REM Build the .NET solution
echo [INFO] Building .NET solution...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to build .NET solution
    exit /b 1
)

REM Stop existing containers
echo [INFO] Stopping existing containers...
docker-compose down

REM Build Docker images
echo [INFO] Building Docker images...
docker-compose build --no-cache
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to build Docker images
    exit /b 1
)

REM Start services
echo [INFO] Starting services...
docker-compose up -d
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to start services
    exit /b 1
)

REM Wait for services to be ready
echo [INFO] Waiting for services to be ready...
timeout /t 30 /nobreak >nul

echo [INFO] 🎉 Deployment completed!
echo.
echo [INFO] 📋 Service URLs:
echo [INFO]   • API Swagger UI: http://localhost:8080/swagger
echo [INFO]   • API Health: http://localhost:8080/health
echo [INFO]   • API Metrics: http://localhost:8080/metrics
echo [INFO]   • RabbitMQ Management: http://localhost:15672 (guest/guest)
echo [INFO]   • MongoDB Express: http://localhost:8082 (admin/admin123)
echo [INFO]   • Redis Commander: http://localhost:8083
echo [INFO]   • Kibana: http://localhost:5601
echo [INFO]   • Grafana: http://localhost:3000 (admin/admin123)
echo [INFO]   • Prometheus: http://localhost:9090
echo.
echo [INFO] 📊 To view logs: docker-compose logs -f [service-name]
echo [INFO] 🛑 To stop services: docker-compose down

pause