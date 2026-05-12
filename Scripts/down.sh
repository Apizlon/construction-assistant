#!/bin/bash

set -e

echo "Stopping Docker containers..."
docker-compose -f docker-compose.yml down
echo "All services stopped successfully!"

