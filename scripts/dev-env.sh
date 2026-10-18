#!/usr/bin/env bash
# Source this file in the Linux cloud environment; Windows uses the installed SDK.
export DOTNET_ROOT=/workspace/.tools/dotnet
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_HOME=/workspace/.tools/dotnet-home
export NUGET_PACKAGES=/workspace/.tools/nuget
export NUGET_HTTP_CACHE_PATH=/workspace/.tools/nuget-http
export XDG_DATA_HOME=/workspace/.tools/data
export XDG_RUNTIME_DIR=/workspace/.tools/runtime
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_NOLOGO=1
mkdir -p "$XDG_RUNTIME_DIR"
chmod 700 "$XDG_RUNTIME_DIR"
