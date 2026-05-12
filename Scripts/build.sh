#!/bin/bash

set -e

echo "Building Docker images..."
docker-compose -f docker-compose.yml build
echo "Build completed successfully!"

