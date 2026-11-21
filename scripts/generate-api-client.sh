#!/usr/bin/env bash
# Regenerates the frontend's Kiota client from the committed OpenAPI
# document (ADR-0004). Run it after docs/api/openapi.json changed;
# CI runs it too and fails when the result differs from the commit.
#
# JSON is the only format the API speaks, so the client registers
# the JSON serializer alone instead of Kiota's default four.
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet tool restore

# Kiota reports usage data to Microsoft unless told otherwise.
export KIOTA_CLI_TELEMETRY_OPTOUT=true

dotnet kiota generate \
  --language TypeScript \
  --openapi docs/api/openapi.json \
  --output frontend/src/lib/api \
  --class-name ArenaApiClient \
  --exclude-backward-compatible \
  --serializer=@microsoft/kiota-serialization-json.JsonSerializationWriterFactory \
  --deserializer=@microsoft/kiota-serialization-json.JsonParseNodeFactory \
  --structured-mime-types application/json \
  --disable-validation-rules NoServerEntry \
  --clean-output \
  --log-level Warning
