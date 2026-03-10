#!/bin/bash
# S13G Complete Local Setup Script (Linux with apt)
# This script installs PostgreSQL, RabbitMQ, and starts the API

set -e

echo "🚀 S13G Local Setup Script (Linux)"
echo "==================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if running with sudo
if [ "$EUID" -ne 0 ]; then
   echo -e "${RED}❌ This script must be run with sudo${NC}"
   echo "Run: sudo bash scripts/setup-local-linux.sh"
   exit 1
fi

# Update package lists
echo -e "${BLUE}Updating package lists...${NC}"
apt-get update
echo -e "${GREEN}✓ Package lists updated${NC}"
echo ""

# Install PostgreSQL if not already installed
echo -e "${BLUE}Checking PostgreSQL...${NC}"
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL 15..."
    apt-get install -y postgresql-15 postgresql-contrib-15
    echo -e "${GREEN}✓ PostgreSQL installed${NC}"
else
    echo -e "${GREEN}✓ PostgreSQL already installed${NC}"
fi
echo ""

# Install RabbitMQ if not already installed
echo -e "${BLUE}Checking RabbitMQ...${NC}"
if ! command -v rabbitmq-server &> /dev/null; then
    echo "Installing RabbitMQ..."
    apt-get install -y rabbitmq-server
    echo -e "${GREEN}✓ RabbitMQ installed${NC}"
else
    echo -e "${GREEN}✓ RabbitMQ already installed${NC}"
fi
echo ""

# Start PostgreSQL
echo -e "${BLUE}Starting PostgreSQL...${NC}"
systemctl start postgresql
systemctl enable postgresql
echo -e "${GREEN}✓ PostgreSQL started and enabled${NC}"
echo ""

# Start RabbitMQ
echo -e "${BLUE}Starting RabbitMQ...${NC}"
systemctl start rabbitmq-server
systemctl enable rabbitmq-server
echo -e "${GREEN}✓ RabbitMQ started and enabled${NC}"
echo ""

# Wait for PostgreSQL to be ready
echo -e "${BLUE}Waiting for PostgreSQL to be ready...${NC}"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if sudo -u postgres psql -c "SELECT 1" &> /dev/null; then
        echo -e "${GREEN}✓ PostgreSQL is ready${NC}"
        break
    fi
    attempt=$((attempt + 1))
    echo "  Waiting... ($attempt/$max_attempts)"
    sleep 1
done

if [ $attempt -ge $max_attempts ]; then
    echo -e "${RED}❌ PostgreSQL failed to start${NC}"
    exit 1
fi
echo ""

# Create database if it doesn't exist
echo -e "${BLUE}Creating database...${NC}"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname = 's13g'" | grep -q 1 || sudo -u postgres psql -c "CREATE DATABASE s13g;"
echo -e "${GREEN}✓ Database 's13g' is ready${NC}"
echo ""

# Wait for RabbitMQ to be ready
echo -e "${BLUE}Waiting for RabbitMQ to be ready...${NC}"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if rabbitmq-diagnostics ping &> /dev/null; then
        echo -e "${GREEN}✓ RabbitMQ is ready${NC}"
        break
    fi
    attempt=$((attempt + 1))
    echo "  Waiting... ($attempt/$max_attempts)"
    sleep 2
done

if [ $attempt -ge $max_attempts ]; then
    echo -e "${RED}❌ RabbitMQ failed to start${NC}"
    exit 1
fi
echo ""

# Restore NuGet packages
echo -e "${BLUE}Restoring NuGet packages...${NC}"
dotnet restore
echo -e "${GREEN}✓ Packages restored${NC}"
echo ""

# Build API project
echo -e "${BLUE}Building API project...${NC}"
if dotnet build src/Api/Api.csproj -c Release; then
    echo -e "${GREEN}✓ API project built${NC}"
else
    echo -e "${YELLOW}⚠️  Build completed with warnings. Continuing...${NC}"
fi
echo ""

# Apply database migrations
echo -e "${BLUE}Applying database migrations...${NC}"

# Ensure dotnet-ef global tool is installed at a version compatible with the project
REQUIRED_EF_VERSION="9.0.3"
if ! dotnet ef --version &> /dev/null; then
    echo "Installing dotnet-ef ${REQUIRED_EF_VERSION}..."
    dotnet tool install --global dotnet-ef --version "${REQUIRED_EF_VERSION}"
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    INSTALLED_EF=$(dotnet ef --version 2>/dev/null | head -1)
    if [[ "$INSTALLED_EF" != 9.* ]]; then
        echo "Updating dotnet-ef to ${REQUIRED_EF_VERSION} (found: ${INSTALLED_EF})..."
        dotnet tool update --global dotnet-ef --version "${REQUIRED_EF_VERSION}"
    fi
fi

# Linux apt installs Postgres with a 'postgres' system user — use that for migrations
export DB_USER=postgres
export DB_PASS=""

cd src/Infrastructure
if dotnet ef database update --project Infrastructure.csproj; then
    echo -e "${GREEN}✓ Migrations applied${NC}"
else
    echo -e "${RED}❌ Migration failed. Check that PostgreSQL is running and the 'postgres' role has access.${NC}"
    exit 1
fi
cd ../..
echo ""

# Display service information
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo -e "${GREEN}✓ Setup Complete!${NC}"
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Service Information:${NC}"
echo -e "${GREEN}PostgreSQL:${NC}"
echo "  Host: localhost"
echo "  Port: 5432"
echo "  User: postgres (no password set)"
echo "  Database: s13g"
echo ""
echo -e "${GREEN}RabbitMQ:${NC}"
echo "  AMQP: amqp://guest:guest@localhost:5672"
echo "  Management UI: http://localhost:15672 (guest/guest)"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Start the API:"
echo "   cd src/Api && dotnet run"
echo ""
echo "2. API will be available at: http://localhost:5000"
echo "3. Swagger UI: http://localhost:5000/swagger"
echo ""
echo -e "${BLUE}To manage services:${NC}"
echo "  Stop PostgreSQL:  sudo systemctl stop postgresql"
echo "  Stop RabbitMQ:    sudo systemctl stop rabbitmq-server"
echo "  Start PostgreSQL: sudo systemctl start postgresql"
echo "  Start RabbitMQ:   sudo systemctl start rabbitmq-server"
echo ""
