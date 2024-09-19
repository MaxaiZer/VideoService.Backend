#!/bin/sh

PFX_FILE="/https/aspnetapp.pfx"
CERT_FILE="/etc/ssl/certs/aspnetapp.crt"
KEY_FILE="/etc/ssl/private/aspnetapp.key"
PFX_PASSWORD="qwerty"

openssl pkcs12 -in "$PFX_FILE" -clcerts -nokeys -out "$CERT_FILE" -passin pass:"$PFX_PASSWORD"

openssl pkcs12 -in "$PFX_FILE" -nocerts -nodes -out "$KEY_FILE" -passin pass:"$PFX_PASSWORD"

chmod 600 "$KEY_FILE"

nginx -g 'daemon off;'