#!/bin/sh

minio server --console-address ":9001" /data &
echo "MinIO server was started"

until /usr/bin/mc alias set myminio http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"; do
  echo "Waiting for MinIO server to be ready..."
  sleep 5
done

if /usr/bin/mc mb myminio/"$BUCKET_NAME" --ignore-existing; then
    echo "Bucket '$BUCKET_NAME' created successfully."
  else
    echo "Error: Unable to create bucket '$BUCKET_NAME'."
    exit 1
fi

wait