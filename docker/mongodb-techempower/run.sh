#!/usr/bin/env bash

#echo on
set -x

docker run \
    -d \
    --log-opt max-size=10m \
    --log-opt max-file=3 \
    --name mongodb-techempower \
    --network host \
    --restart always \
    mongodb-techempower
