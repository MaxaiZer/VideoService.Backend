#!/bin/sh

minio server --console-address ":9001" /data &
echo "MinIO server was started"

sleep 3;

until mc ready local; do
    echo "Waiting for MinIO server to be ready..."
    sleep 5
done

/usr/bin/mc alias set myminio http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD";

if ! /usr/bin/mc mb myminio/"$BUCKET_NAME" --ignore-existing; then
    echo "Error: Unable to create bucket '$BUCKET_NAME'."
    exit 1
fi

if [ -n "$(/usr/bin/mc ilm ls myminio/"$BUCKET_NAME" 2>/dev/null)" ]; then
    echo "ILM rule for prefix 'tmp/' already exists."
else
    if ! /usr/bin/mc ilm add --expiry-days 3 --prefix "$TMP_FOLDER/" myminio/"$BUCKET_NAME"; then
        echo "Error: Unable to add ILM rule for prefix '$TMP_FOLDER/'."
        exit 1
    fi
fi

wait