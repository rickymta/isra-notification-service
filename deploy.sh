#!/bin/bash

# Notification Service Build and Deploy Script

set -e

echo "🚀 Starting Notification Service build and deployment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker and try again."
    exit 1
fi

# Build the .NET solution
print_status "Building .NET solution..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    print_error "Failed to build .NET solution"
    exit 1
fi

# Stop existing containers
print_status "Stopping existing containers..."
docker-compose down

# Build Docker images
print_status "Building Docker images..."
docker-compose build --no-cache
if [ $? -ne 0 ]; then
    print_error "Failed to build Docker images"
    exit 1
fi

# Start services
print_status "Starting services..."
docker-compose up -d
if [ $? -ne 0 ]; then
    print_error "Failed to start services"
    exit 1
fi

# Wait for services to be ready
print_status "Waiting for services to be ready..."
sleep 30

# Check service health
print_status "Checking service health..."

# Check API health
api_health=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health || echo "000")
if [ "$api_health" = "200" ]; then
    print_status "✅ API service is healthy"
else
    print_warning "⚠️  API service health check failed (HTTP $api_health)"
fi

# Check MongoDB
if docker exec notification-mongodb mongosh --eval "db.runCommand('ping')" > /dev/null 2>&1; then
    print_status "✅ MongoDB is healthy"
else
    print_warning "⚠️  MongoDB health check failed"
fi

# Check Redis
if docker exec notification-redis redis-cli -a redis123 ping | grep -q PONG; then
    print_status "✅ Redis is healthy"
else
    print_warning "⚠️  Redis health check failed"
fi

# Check RabbitMQ
if curl -s -u guest:guest http://localhost:15672/api/overview > /dev/null; then
    print_status "✅ RabbitMQ is healthy"
else
    print_warning "⚠️  RabbitMQ health check failed"
fi

# Check Elasticsearch
if curl -s http://localhost:9200/_health > /dev/null; then
    print_status "✅ Elasticsearch is healthy"
else
    print_warning "⚠️  Elasticsearch health check failed"
fi

print_status "🎉 Deployment completed!"
print_status ""
print_status "📋 Service URLs:"
print_status "  • API Swagger UI: http://localhost:8080/swagger"
print_status "  • API Health: http://localhost:8080/health"
print_status "  • API Metrics: http://localhost:8080/metrics"
print_status "  • RabbitMQ Management: http://localhost:15672 (guest/guest)"
print_status "  • MongoDB Express: http://localhost:8082 (admin/admin123)"
print_status "  • Redis Commander: http://localhost:8083"
print_status "  • Kibana: http://localhost:5601"
print_status "  • Grafana: http://localhost:3000 (admin/admin123)"
print_status "  • Prometheus: http://localhost:9090"
print_status ""
print_status "📊 To view logs: docker-compose logs -f [service-name]"
print_status "🛑 To stop services: docker-compose down"