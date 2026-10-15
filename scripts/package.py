#!/usr/bin/env python3
"""Package a previously published self-contained Windows x64 build; fail closed."""
from pathlib import Path
import hashlib
import shutil
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
ARTIFACTS = ROOT / "artifacts"
PUBLISH = ARTIFACTS / "publish" / "win-x64"
STAGE = ARTIFACTS / "package" / "FrameForge"
REQUIRED = ["FrameForge.exe", "FrameForge.dll", "FrameForge.Core.dll", "FrameForge.Windows.dll",
            "FrameForge.runtimeconfig.json", "FrameForge.deps.json", "coreclr.dll", "hostfxr.dll",
            "PresentationFramework.dll", "System.Management.dll"]


def validate_publish(folder):
    for name in REQUIRED:
        p = folder / name
        if not p.is_file() or p.stat().st_size == 0:
            raise ValueError(f"Missing/empty published file: {name}")
    with (folder / "FrameForge.exe").open("rb") as exe:
        if exe.read(2) != b"MZ":
            raise ValueError("Launcher is not a Windows PE executable")
        exe.seek(0x3C)
        pe_offset = struct.unpack("<I", exe.read(4))[0]
        exe.seek(pe_offset)
        if exe.read(4) != b"PE\0\0" or struct.unpack("<H", exe.read(2))[0] != 0x8664:
            raise ValueError("Expected x64 Windows launcher")


def package():
    validate_publish(PUBLISH)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    shutil.copytree(PUBLISH, STAGE, ignore=shutil.ignore_patterns("*.pdb"))
    for name in ["README.txt", "LICENSE"]:
        shutil.copy2(ROOT / name, STAGE / name)
    for name in ["docs", "licenses"]:
        shutil.copytree(ROOT / name, STAGE / name)
    files = sorted(p for p in STAGE.rglob("*") if p.is_file())
    (STAGE / "SHA256SUMS.txt").write_text("".join(
        f"{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.relative_to(STAGE).as_posix()}\n" for p in files), encoding="utf-8")
    target = ARTIFACTS / "FrameForge-Universal.zip"
    with zipfile.ZipFile(target, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for p in sorted(STAGE.rglob("*")):
            if p.is_file():
                z.write(p, "FrameForge/" + p.relative_to(STAGE).as_posix())
    with zipfile.ZipFile(target) as z:
        if z.testzip() is not None:
            raise ValueError("ZIP integrity check failed")
        for line in z.read("FrameForge/SHA256SUMS.txt").decode().splitlines():
            expected, path = line.split("  ", 1)
            if hashlib.sha256(z.read("FrameForge/" + path)).hexdigest() != expected:
                raise ValueError("ZIP file checksum mismatch: " + path)
    source = ARTIFACTS / "FrameForge-Source.zip"
    excluded = {".git", "bin", "obj", "artifacts", "__pycache__", ".vs", "TestResults"}
    with zipfile.ZipFile(source, "w", compression=zipfile.ZIP_DEFLATED) as z:
        for p in sorted(ROOT.rglob("*")):
            if p.is_file() and not any(part in excluded for part in p.relative_to(ROOT).parts):
                z.write(p, "FrameForge/" + p.relative_to(ROOT).as_posix())
    desktop = ARTIFACTS / "FrameForge-Desktop.zip"
    with zipfile.ZipFile(desktop, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as out:
        with zipfile.ZipFile(target) as app:
            for entry in app.infolist():
                out.writestr(entry.filename, app.read(entry.filename))
        with zipfile.ZipFile(source) as src:
            for entry in src.infolist():
                out.writestr("FrameForge/Source/" + entry.filename.removeprefix("FrameForge/"), src.read(entry.filename))
        out.write(ROOT / "scripts" / "Place-on-Desktop.cmd", "Place-on-Desktop.cmd")
        out.write(ROOT / "scripts" / "Update-FrameForge.cmd", "Update-FrameForge.cmd")
        out.writestr("START-HERE.txt", "Extract this entire ZIP into Downloads. Close the old app, then run Update-FrameForge.cmd. It updates the existing Desktop/FrameForge application (or a selected existing folder), keeps the previous folder in a dated backup and preserves LocalAppData settings/history. It also supports a fresh Desktop installation. Open the updated FrameForge.exe.\nEngineering preview: complete Windows/hardware acceptance before primary-PC system changes.\n")
    with zipfile.ZipFile(desktop) as z:
        if z.testzip() is not None:
            raise ValueError("Desktop ZIP integrity check failed")
        for required in ["Place-on-Desktop.cmd", "Update-FrameForge.cmd", "FrameForge/FrameForge.exe", "FrameForge/Source/FrameForge.sln"]:
            if required not in z.namelist():
                raise ValueError("Desktop package is incomplete: " + required)
    bundles = [target, source, desktop]
    ui_checks = ARTIFACTS / "ui-checks"
    if (ui_checks / "report.txt").is_file():
        ui_bundle = ARTIFACTS / "FrameForge-UI-checks.zip"
        with zipfile.ZipFile(ui_bundle, "w", compression=zipfile.ZIP_DEFLATED) as out:
            for p in sorted(ui_checks.rglob("*")):
                if p.is_file():
                    out.write(p, p.relative_to(ui_checks).as_posix())
        bundles.append(ui_bundle)
    (ARTIFACTS / "SHA256SUMS.txt").write_text("".join(
        f"{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}\n" for p in bundles), encoding="utf-8")
    print(f"Validated {target.name}: {target.stat().st_size:,} bytes")
    print(f"Validated {desktop.name}: {desktop.stat().st_size:,} bytes")
    print(f"Source: {source.name}")


if __name__ == "__main__":
    package()
