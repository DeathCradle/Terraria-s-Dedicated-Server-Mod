#!/bin/bash
while true
do
	mono TerrariaServer.exe -config server.config
	echo "Restarting server..."
	sleep 1
done
