#!/usr/bin/env bash
set -eu

sqlcmd=/opt/mssql-tools18/bin/sqlcmd

"$sqlcmd" \
  -S sqlserver,1433 \
  -U sa \
  -P "$MSSQL_SA_PASSWORD" \
  -d master \
  -C \
  -b \
  -i /scripts/01-schema.sql

"$sqlcmd" \
  -S sqlserver,1433 \
  -U sa \
  -P "$MSSQL_SA_PASSWORD" \
  -d master \
  -C \
  -b \
  -i /scripts/02-seed.sql
