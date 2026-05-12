#!/bin/bash

set -e

echo "Starting Docker containers..."
docker-compose -f docker-compose.yml up -d

echo "Waiting for services to be ready..."
sleep 10

echo "All services started successfully!"
echo ""
echo "Services URLs:"
echo "  - Auth Service: http://localhost:5001"
echo "  - User Service: http://localhost:5002"
echo "  - PostgreSQL: localhost:5301"
echo ""
echo "Default credentials:"
echo "  - PostgreSQL: postgres/postgres"

