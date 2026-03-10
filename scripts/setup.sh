#!/bin/bash
# S13G Setup Script
# This script sets up the entire development environment

set -e

echo "🚀 S13G Setup Script"
echo "===================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if Docker is running
echo -e "${BLUE}Checking Docker...${NC}"
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

if ! docker ps &> /dev/null; then
    echo -e "${RED}❌ Docker is not running. Please start Docker first.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker is running${NC}"
echo ""

# Check if Docker Compose is available
echo -e "${BLUE}Checking Docker Compose...${NC}"
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
elif docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
else
    echo -e "${RED}❌ Docker Compose is not available. Install Docker Desktop or the docker-compose plugin.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker Compose is available ($DOCKER_COMPOSE)${NC}"
echo ""

# Start services
echo -e "${BLUE}Starting PostgreSQL and RabbitMQ containers...${NC}"
$DOCKER_COMPOSE up -d
echo -e "${GREEN}✓ Containers started${NC}"
echo ""

# Wait for PostgreSQL to be ready
echo -e "${BLUE}Waiting for PostgreSQL to be ready...${NC}"
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if docker exec s13g-postgres pg_isready -U postgres &> /dev/null; then
        echo -e "${GREEN}✓ PostgreSQL is ready${NC}"
        break
    fi
    attempt=$((attempt + 1))
    echo "  Waiting... ($attempt/$max_attempts)"
    sleep 2
done

if [ $attempt -ge $max_attempts ]; then
    echo -e "${RED}❌ PostgreSQL failed to start${NC}"
    exit 1
fi
echo ""

# Wait for RabbitMQ to be ready
echo -e "${BLUE}Waiting for RabbitMQ to be ready...${NC}"
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if docker exec s13g-rabbitmq rabbitmq-diagnostics ping &> /dev/null; then
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

# Apply database migrations
echo -e "${BLUE}Restoring NuGet packages...${NC}"
dotnet restore
echo -e "${GREEN}✓ Packages restored${NC}"
echo ""

echo -e "${BLUE}Building solution...${NC}"
dotnet build -c Release
echo -e "${GREEN}✓ Solution built${NC}"
echo ""

echo -e "${BLUE}Applying database migrations...${NC}"
cd src/Api
dotnet ef database update || {
    echo -e "${YELLOW}Note: If migration fails, ensure PostgreSQL connection is correct in appsettings.json${NC}"
}
cd ../..
echo -e "${GREEN}✓ Migrations applied${NC}"
echo ""

# Display service information
echo -e "${BLUE}Service Information:${NC}"
echo -e "${GREEN}PostgreSQL:${NC}"
echo "  Host: localhost"
echo "  Port: 5432"
echo "  User: postgres"
echo "  Password: postgres"
echo "  Database: s13g"
echo ""
echo -e "${GREEN}RabbitMQ:${NC}"
echo "  AMQP: amqp://guest:guest@localhost:5672"
echo "  Management UI: http://localhost:15672"
echo ""

echo -e "${BLUE}Next steps:${NC}"
echo "1. Start the API:"
echo "   cd src/Api && dotnet run"
echo ""
echo "2. API will be available at: http://localhost:5000"
echo "3. Swagger UI: http://localhost:5000/swagger"
echo ""
echo -e "${GREEN}✓ Setup complete!${NC}"
