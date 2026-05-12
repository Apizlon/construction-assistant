#!/bin/bash

set -e

echo "Stopping containers and removing volumes..."
docker-compose -f docker-compose.yml down -v --remove-orphans
echo "Hard reset completed successfully!"

