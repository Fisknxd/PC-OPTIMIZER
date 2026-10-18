#!/usr/bin/env bash
# Idempotent Linux cloud preparation. Does not change application source/lockfiles.
set -euo pipefail
cd /workspace/PC-OPTIMIZER
mkdir -p /workspace/.tools/dotnet
source scripts/dev-env.sh
sdk_root=/workspace/.tools/dotnet
sdk_version=10.0.402
if [[ ! -x "$sdk_root/dotnet" ]] || [[ "$($sdk_root/dotnet --version)" != "$sdk_version" ]]; then
  setup_tmp=$(mktemp -d /tmp/frameforge-sdk.XXXXXX)
  trap 'rm -rf "$setup_tmp"' EXIT
  curl --fail --location --retry 2 --proto '=https' --tlsv1.2 \
    "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdk_version/dotnet-sdk-$sdk_version-linux-x64.tar.gz" \
    -o "$setup_tmp/sdk.tar.gz"
  printf '%s  %s\n' '1904a98dda2db34ba78ba3c4fb1b224455329af91c341d83547843c1839e112b04ae699ee92fa2449a8f62249196a938723078c0c60b9f65c88539d5687729c2' "$setup_tmp/sdk.tar.gz" | sha512sum --check --status
  tar -xzf "$setup_tmp/sdk.tar.gz" -C "$sdk_root"
fi
source scripts/dev-env.sh
dotnet restore FrameForge.sln --locked-mode
dotnet build FrameForge.sln -c Release --no-restore -m:2
dotnet test tests/FrameForge.Tests -c Release --no-build --logger 'trx;LogFileName=setup-tests.trx' --results-directory artifacts/test-results
python3 -m unittest discover -s scripts -p 'test_*.py'
