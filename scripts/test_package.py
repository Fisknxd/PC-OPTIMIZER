import tempfile
import unittest
from pathlib import Path
from package import REQUIRED, validate_publish


class PackagingTests(unittest.TestCase):
    def test_missing_runtime_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d)
            (p / "FrameForge.exe").write_bytes(b"MZ")
            with self.assertRaisesRegex(ValueError, "Missing/empty"):
                validate_publish(p)

    def test_fake_executable_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d)
            for name in REQUIRED:
                (p / name).write_bytes(b"not-a-binary")
            with self.assertRaisesRegex(ValueError, "Windows PE"):
                validate_publish(p)

    def test_wrong_architecture_rejected(self):
        import struct
        with tempfile.TemporaryDirectory() as d:
            p = Path(d)
            for name in REQUIRED:
                (p / name).write_bytes(b"stub")
            exe = bytearray(128)
            exe[:2] = b"MZ"
            struct.pack_into("<I", exe, 0x3c, 64)
            exe[64:68] = b"PE\0\0"
            struct.pack_into("<H", exe, 68, 0x14c)
            (p / "FrameForge.exe").write_bytes(exe)
            with self.assertRaisesRegex(ValueError, "x64"):
                validate_publish(p)
