# S13G Local Setup Script (Windows with Chocolatey)
# This script installs PostgreSQL, RabbitMQ, and starts the API

# Run this in PowerShell as Administrator

Write-Host "🚀 S13G Local Setup Script (Windows)" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')
if (-not $isAdmin) {
    Write-Host "❌ This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Check if Chocolatey is installed
Write-Host "Checking Chocolatey..." -ForegroundColor Blue
if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Chocolatey..." -ForegroundColor Yellow
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
}
Write-Host "✓ Chocolatey is installed" -ForegroundColor Green
Write-Host ""

# Install PostgreSQL if not already installed
Write-Host "Checking PostgreSQL..." -ForegroundColor Blue
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "Installing PostgreSQL 15..." -ForegroundColor Yellow
    choco install postgresql15 -y
    refreshenv
    Write-Host "✓ PostgreSQL installed" -ForegroundColor Green
} else {
    Write-Host "✓ PostgreSQL already installed" -ForegroundColor Green
}
Write-Host ""

# Install RabbitMQ if not already installed
Write-Host "Checking RabbitMQ..." -ForegroundColor Blue
if (-not (Get-Service RabbitMQ -ErrorAction SilentlyContinue)) {
    Write-Host "Installing RabbitMQ..." -ForegroundColor Yellow
    choco install rabbitmq -y
    refreshenv
    Write-Host "✓ RabbitMQ installed" -ForegroundColor Green
} else {
    Write-Host "✓ RabbitMQ already installed" -ForegroundColor Green
}
Write-Host ""

# Start PostgreSQL
Write-Host "Starting PostgreSQL..." -ForegroundColor Blue
$pgService = Get-Service -Name "PostgreSQL*" -ErrorAction SilentlyContinue
if ($null -ne $pgService) {
    Start-Service $pgService.Name -ErrorAction SilentlyContinue
    Write-Host "✓ PostgreSQL started" -ForegroundColor Green
} else {
    Write-Host "⚠ PostgreSQL service not found" -ForegroundColor Yellow
    Write-Host "Please start PostgreSQL manually from Services" -ForegroundColor Yellow
}
Write-Host ""

# Start RabbitMQ
Write-Host "Starting RabbitMQ..." -ForegroundColor Blue
$rmqService = Get-Service -Name "RabbitMQ" -ErrorAction SilentlyContinue
if ($null -ne $rmqService) {
    Start-Service "RabbitMQ" -ErrorAction SilentlyContinue
    Write-Host "✓ RabbitMQ started" -ForegroundColor Green
} else {
    Write-Host "⚠ RabbitMQ service not found" -ForegroundColor Yellow
    Write-Host "Please start RabbitMQ manually from Services" -ForegroundColor Yellow
}
Write-Host ""

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Blue
$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = "Host=localhost;Username=postgres;Database=postgres"
        $connection.Open()
        $connection.Close()
        Write-Host "✓ PostgreSQL is ready" -ForegroundColor Green
        break
    } catch {
        $attempt++
        Write-Host "  Waiting... ($attempt/$maxAttempts)" -ForegroundColor Yellow
        Start-Sleep -Seconds 1
    }
}
Write-Host ""

# Create database using psql
Write-Host "Creating database..." -ForegroundColor Blue
$env:PGPASSWORD = "postgres"
$dbExists = psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 's13g'" 2>$null
if (-not $dbExists) {
    psql -U postgres -c "CREATE DATABASE s13g;" 2>$null
}
Write-Host "✓ Database 's13g' is ready" -ForegroundColor Green
Write-Host ""

# Wait for RabbitMQ
Write-Host "Waiting for RabbitMQ to be ready..." -ForegroundColor Blue
Start-Sleep -Seconds 5
Write-Host "✓ RabbitMQ should be ready" -ForegroundColor Green
Write-Host ""

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Blue
dotnet restore
Write-Host "✓ Packages restored" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "Building solution..." -ForegroundColor Blue
dotnet build -c Release
Write-Host "✓ Solution built" -ForegroundColor Green
Write-Host ""

# Apply migrations
Write-Host "Applying database migrations..." -ForegroundColor Blue
Set-Location src/Api
$env:PGPASSWORD = "postgres"
dotnet ef database update
Set-Location ../..
Write-Host "✓ Migrations applied" -ForegroundColor Green
Write-Host ""

# Display information
Write-Host "════════════════════════════════════════" -ForegroundColor Green
Write-Host "✓ Setup Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Service Information:" -ForegroundColor Blue
Write-Host ""
Write-Host "PostgreSQL:" -ForegroundColor Green
Write-Host "  Host: localhost"
Write-Host "  Port: 5432"
Write-Host "  User: postgres"
Write-Host "  Password: postgres (default)"
Write-Host "  Database: s13g"
Write-Host ""
Write-Host "RabbitMQ:" -ForegroundColor Green
Write-Host "  AMQP: amqp://guest:guest@localhost:5672"
Write-Host "  Management UI: http://localhost:15672 (guest/guest)"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Blue
Write-Host "1. Start the API:"
Write-Host "   cd src/Api && dotnet run"
Write-Host ""
Write-Host "2. API will be available at: http://localhost:5000"
Write-Host "3. Swagger UI: http://localhost:5000/swagger"
Write-Host ""
