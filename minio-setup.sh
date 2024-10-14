#!/bin/sh

minio server --console-address ":9001" /data &
echo "MinIO server was started"

sleep 3;

until mc ready local; do
    echo "Waiting for MinIO server to be ready..."
    sleep 5
done

mc alias set myminio http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD";

if ! mc mb myminio/"$BUCKET_NAME" --ignore-existing; then
    echo "Error: Unable to create bucket '$BUCKET_NAME'."
    exit 1
fi

if [ -z "$(mc anonymous list myminio/"$BUCKET_NAME"/"$PUBLIC_FOLDER"/ 2>/dev/null)" ]; then
    echo "Creating public read-only policy for folder '$PUBLIC_FOLDER'..."
    mc anonymous set download myminio/"$BUCKET_NAME"/"$PUBLIC_FOLDER"/
else
    echo "Public read-only policy for folder '$PUBLIC_FOLDER' already exists."
fi

if [ -n "$(mc ilm ls myminio/"$BUCKET_NAME" 2>/dev/null)" ]; then
    echo "Lifecycle management rule for prefix 'tmp/' already exists."
else
    if ! mc ilm add --expiry-days 3 --prefix "$TMP_FOLDER/" myminio/"$BUCKET_NAME"; then
        echo "Error: Unable to add lifecycle management rule for prefix '$TMP_FOLDER/'."
        exit 1
    fi
fi

wait