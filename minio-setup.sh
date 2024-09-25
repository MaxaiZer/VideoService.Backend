#!/bin/sh

minio server --console-address ":9001" /data &

until /usr/bin/mc alias set myminio http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"; do
  echo "Waiting for MinIO server..."
  sleep 5
done

/usr/bin/mc mb myminio/"$BUCKET_NAME"
echo "Bucket '$BUCKET_NAME' created successfully."

wait