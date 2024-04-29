#!/bin/bash

# Define variables
DOCKER_HUB_USERNAME="wenxiyan"
TEAM_NUMBER="team_01"
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

# Remove existing application container if it exists
docker ps -aq -f name=^18656_${TEAM_NUMBER}$ | xargs -r docker rm -f

# Run MySQL container with persistent storage
docker run \
  -e "MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}" \
  -e "MYSQL_DATABASE=${MYSQL_DATABASE}" \
  -p 3306:3306 \
  --network arbitrage-net \
  -v mysql_data:/var/lib/mysql \
  --name ${MYSQL_IMAGE_NAME} \
  -d mysql:latest

# Wait for MySQL to fully start
echo "Waiting for MySQL to start..."
sleep 10  # Adjust this sleep time if necessary to ensure MySQL is ready

# Copy the SQL dump into the MySQL container
# docker cp data.sql ${MYSQL_IMAGE_NAME}:/data.sql

# Execute the SQL dump file inside the MySQL container
# docker exec -i ${MYSQL_IMAGE_NAME} sh -c 'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" $MYSQL_DATABASE < /data.sql'

# Build the Docker image for your application
docker build -f Dockerfile --platform linux/amd64 -t ${DOCKER_HUB_USERNAME}/18656_${TEAM_NUMBER} .

# Run your application container
docker run -p ${APP_PORT}:${APP_PORT} \
           --network arbitrage-net \
           --link ${MYSQL_IMAGE_NAME}:mysql \
           -v $(pwd)/historicalData.txt:/app/historicalData.txt \
           --name 18656_${TEAM_NUMBER} \
           ${DOCKER_HUB_USERNAME}/18656_${TEAM_NUMBER}


# Output connection information
echo "To connect to your MySQL Server, use the following connection string in your application:"
echo "Server=mysql;Database=${MYSQL_DATABASE};User=root;Password=${MYSQL_ROOT_PASSWORD};"
