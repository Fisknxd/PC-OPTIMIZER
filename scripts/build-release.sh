#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
dotnet restore FrameForge.sln --locked-mode
dotnet build FrameForge.sln -c Release --no-restore -m:2
dotnet test tests/FrameForge.Tests -c Release --no-build --logger 'trx;LogFileName=release-tests.trx' --results-directory artifacts/test-results
dotnet publish src/FrameForge.App -c Release -r win-x64 --self-contained true --no-restore -o artifacts/publish/win-x64
python3 -m unittest discover -s scripts -p 'test_*.py'
python3 scripts/package.py
