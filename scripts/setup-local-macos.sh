#!/bin/bash
# S13G Complete Local Setup Script (macOS with Homebrew)
# This script installs PostgreSQL, RabbitMQ, and starts the API

set -e

echo "🚀 S13G Local Setup Script (macOS)"
echo "==================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if Homebrew is installed
echo -e "${BLUE}Checking Homebrew...${NC}"
if ! command -v brew &> /dev/null; then
    echo -e "${RED}❌ Homebrew is not installed.${NC}"
    echo "Install Homebrew from: https://brew.sh"
    exit 1
fi
echo -e "${GREEN}✓ Homebrew is installed${NC}"
echo ""

# Install PostgreSQL if not already installed
echo -e "${BLUE}Checking PostgreSQL...${NC}"
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL 15..."
    brew install postgresql@15
    brew link postgresql@15 --force
    echo -e "${GREEN}✓ PostgreSQL installed${NC}"
else
    echo -e "${GREEN}✓ PostgreSQL already installed${NC}"
fi
echo ""

# Install RabbitMQ if not already installed
echo -e "${BLUE}Checking RabbitMQ...${NC}"
if ! command -v rabbitmq-server &> /dev/null; then
    echo "Installing RabbitMQ..."
    brew install rabbitmq
    echo -e "${GREEN}✓ RabbitMQ installed${NC}"
else
    echo -e "${GREEN}✓ RabbitMQ already installed${NC}"
fi
echo ""

# Start PostgreSQL
echo -e "${BLUE}Starting PostgreSQL...${NC}"
if brew services list | grep -q "postgresql@15.*started"; then
    echo -e "${GREEN}✓ PostgreSQL is already running${NC}"
else
    brew services start postgresql@15
    echo -e "${GREEN}✓ PostgreSQL started${NC}"
fi

echo ""

# Before launching RabbitMQ make sure management port isn't occupied
if lsof -i :15672 &> /dev/null; then
    echo -e "${YELLOW}⚠️  Port 15672 (RabbitMQ management) is already in use.
    RabbitMQ will disable the management plugin to avoid conflict.${NC}"
    PATH="/opt/homebrew/opt/erlang/bin:$PATH" /opt/homebrew/sbin/rabbitmq-plugins --offline disable rabbitmq_management || true
fi

# Start RabbitMQ
echo -e "${BLUE}Starting RabbitMQ...${NC}"
service_status=$(brew services list | grep rabbitmq | awk '{print $2}')
if [ "$service_status" = "started" ]; then
    echo -e "${GREEN}✓ RabbitMQ is already running${NC}"
elif [ "$service_status" = "error" ]; then
    echo -e "${YELLOW}RabbitMQ service in error state, attempting restart...${NC}"
    brew services restart rabbitmq || true
    sleep 2
    service_status=$(brew services list | grep rabbitmq | awk '{print $2}')
    if [ "$service_status" = "started" ]; then
        echo -e "${GREEN}✓ RabbitMQ restarted successfully${NC}"
    else
        echo -e "${RED}❌ RabbitMQ failed to start (status: $service_status)${NC}"
        echo "You may need to run 'brew services restart rabbitmq' or check logs."
        exit 1
    fi
else
    # attempt to start normally
    if brew services start rabbitmq; then
        echo -e "${GREEN}✓ RabbitMQ started${NC}"
    else
        echo -e "${RED}❌ RabbitMQ failed to start via brew${NC}"
        echo "Try 'brew services restart rabbitmq' or inspect /usr/local/var/log/rabbitmq for details."
        exit 1
    fi
fi
echo ""

# Wait for PostgreSQL to be ready
echo -e "${BLUE}Waiting for PostgreSQL to be ready...${NC}"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if psql -d postgres -c "SELECT 1" &> /dev/null; then
        echo -e "${GREEN}✓ PostgreSQL is ready${NC}"
        break
    fi
    attempt=$((attempt + 1))
    echo "  Waiting... ($attempt/$max_attempts)"
    sleep 1
done

if [ $attempt -ge $max_attempts ]; then
    echo -e "${RED}❌ PostgreSQL failed to start${NC}"
    echo "Troubleshooting: Run 'brew services restart postgresql@15'"
    exit 1
fi
echo ""

# Create database if it doesn't exist
echo -e "${BLUE}Creating database...${NC}"
psql -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = 's13g'" | grep -q 1 || psql -d postgres -c "CREATE DATABASE s13g;"
echo -e "${GREEN}✓ Database 's13g' is ready${NC}"
echo ""

# Wait for RabbitMQ to be ready
echo -e "${BLUE}Waiting for RabbitMQ to be ready...${NC}"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    # prefer built-in diagnostics if available
    if command -v rabbitmq-diagnostics &> /dev/null; then
        if rabbitmq-diagnostics ping &> /dev/null; then
            echo -e "${GREEN}✓ RabbitMQ is ready${NC}"
            break
        fi
    else
        # fallback to checking port 5672
        if nc -z localhost 5672 &> /dev/null; then
            echo -e "${GREEN}✓ RabbitMQ port is open${NC}"
            break
        fi
    fi

    attempt=$((attempt + 1))
    echo "  Waiting... ($attempt/$max_attempts)"
    sleep 2
done

if [ $attempt -ge $max_attempts ]; then
    echo -e "${RED}❌ RabbitMQ failed to start${NC}"
    echo "Check service status: brew services list | grep rabbitmq"
    echo "Logs may be available in /usr/local/var/log/rabbitmq or /opt/homebrew/var/log/rabbitmq"
    exit 1
fi

# Enable management plugin (required for http://localhost:15672)
echo -e "${BLUE}Enabling RabbitMQ management plugin...${NC}"
if PATH="/opt/homebrew/opt/erlang/bin:$PATH" /opt/homebrew/sbin/rabbitmq-plugins enable rabbitmq_management &> /dev/null; then
    echo -e "${GREEN}✓ Management plugin enabled (http://localhost:15672)${NC}"
else
    echo -e "${YELLOW}⚠️  Could not enable management plugin — Management UI may not be available${NC}"
fi

echo ""

# Restore NuGet packages
echo -e "${BLUE}Restoring NuGet packages...${NC}"
dotnet restore
echo -e "${GREEN}✓ Packages restored${NC}"
echo ""

# Build API project only
echo -e "${BLUE}Building API project...${NC}"
if dotnet build src/Api/Api.csproj -c Release; then
    echo -e "${GREEN}✓ API project built${NC}"
else
    echo -e "${YELLOW}⚠️  API build completed with errors (tests may fail). Continuing...${NC}"
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

# Connection credentials for the design-time factory (Homebrew postgres defaults to current OS user)
export DB_USER=$(whoami)
export DB_PASS=""

cd src/Infrastructure
if dotnet ef database update --project Infrastructure.csproj; then
    echo -e "${GREEN}✓ Migrations applied${NC}"
else
    echo -e "${RED}❌ Migration failed. Check that PostgreSQL is running and DB_USER has access.${NC}"
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
echo "  User: $(whoami) (current OS user, no password)"
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
echo "  Stop PostgreSQL:  brew services stop postgresql@15"
echo "  Stop RabbitMQ:    brew services stop rabbitmq"
echo "  Start PostgreSQL: brew services start postgresql@15"
echo "  Start RabbitMQ:   brew services start rabbitmq"
echo ""
