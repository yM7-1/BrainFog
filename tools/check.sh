#!/usr/bin/env bash
set -euo pipefail
export DOTNET_ROOT=/root/.dotnet
export PATH=/root/.dotnet:$PATH
cd "$(dirname "$0")/.."

echo "==> build"
dotnet build BlindSpire.csproj -c Release -p:CopyModOnBuild=false \
  -p:SteamRoot=/mnt/d/Steam \
  -p:Sts2DataDir='/mnt/d/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64' \
  -v minimal -nologo

echo "==> test"
dotnet test tests/BlindSpire.Tests/BlindSpire.Tests.csproj -c Release \
  -v minimal -nologo
