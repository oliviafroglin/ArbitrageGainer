#!/bin/bash

# Define variables
DOCKER_HUB_USERNAME="wenxiyan"
TEAM_NUMBER="01"
APP_PORT=8080
MYSQL_IMAGE_NAME="mysql_18656_${TEAM_NUMBER}"
MYSQL_ROOT_PASSWORD="Functional!"
MYSQL_DATABASE="team_database_schema"

# Ensure the persistent volume exists and create if not
docker volume inspect mysql_data >/dev/null 2>&1 || \
    docker volume create mysql_data

# Check if the network exists and create if not
docker network inspect arbitrage-net >/dev/null 2>&1 || \
    docker network create arbitrage-net

# Remove existing MySQL container if it exists
docker ps -aq -f name=^${MYSQL_IMAGE_NAME}$ | xargs -r docker rm -f

# Run MySQL container with persistent storage
docker run -e "MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}" \
           -e "MYSQL_DATABASE=${MYSQL_DATABASE}" \
           -p 3306:3306 \
           --name ${MYSQL_IMAGE_NAME} \
           --network arbitrage-net \
           -v mysql_data:/var/lib/mysql \
           -d mysql:latest

# Build the Docker image for your application
docker build -f Dockerfile --platform linux/amd64 -t ${DOCKER_HUB_USERNAME}/18656_${TEAM_NUMBER} .

# Run your application container
docker run -p ${APP_PORT}:${APP_PORT} \
           --network arbitrage-net \
           --link ${MYSQL_IMAGE_NAME}:mysql \
           ${DOCKER_HUB_USERNAME}/18656_${TEAM_NUMBER}

# Output connection information
echo "To connect to your MySQL Server, use the following connection string in your application:"
echo "Server=mysql;Database=${MYSQL_DATABASE};User=root;Password=${MYSQL_ROOT_PASSWORD};"
