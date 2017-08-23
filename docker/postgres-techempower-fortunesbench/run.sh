#!/usr/bin/env bash

#echo on
set -x

if (( $# != 1 ))
then
    echo "Usage: run.sh server-name-or-ip"
fi

host=$1
threads=$(nproc --all)
connections=$(($threads * 8))

docker run \
    -it \
    --rm \
    --mount type=bind,source=/mnt,target=/tmp \
    --network host \
    -e PGBENCH_HOST=$host \
    -e PGBENCH_THREADS=$threads \
    -e PGBENCH_CONNECTIONS=$connections \
    postgres-techempower-fortunesbench
