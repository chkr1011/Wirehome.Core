#!/bin/sh

echo "Setting up screen..."
screen -dmS main
screen -S main -X title Wirehome.Core
screen -S main -X chdir /opt/wirehome

echo "Starting Wirehome.Core..."
screen -S main -X exec ./run.sh