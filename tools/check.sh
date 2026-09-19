#!/usr/bin/env bash
set -euo pipefail

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

STEAM_ROOT="${STEAM_ROOT:-/mnt/d/Steam}"
STS2_DATA_DIR="${STS2_DATA_DIR:-$STEAM_ROOT/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64}"
export STS2_DATA_DIR

cd "$(dirname "$0")/.."

echo "==> build"
dotnet build BrainFog.csproj -c Release -p:CopyModOnBuild=false \
  -p:SteamRoot="$STEAM_ROOT" \
  -p:Sts2DataDir="$STS2_DATA_DIR" \
  -v minimal -nologo

echo "==> test"
dotnet test tests/BrainFog.Tests/BrainFog.Tests.csproj -c Release \
  -v minimal -nologo
