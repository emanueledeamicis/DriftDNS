#!/bin/sh
set -e

# Fix ownership of the data directory.
# This handles upgrades from older containers that ran as root,
# where app.db and related files may be owned by root.
chown -R appuser /app/data

exec su -s /bin/sh -c "exec dotnet DriftDNS.App.dll" appuser
