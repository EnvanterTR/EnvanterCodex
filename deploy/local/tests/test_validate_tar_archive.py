from __future__ import annotations

import importlib.util
import io
import tarfile
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "validate-tar-archive.py"
SPEC = importlib.util.spec_from_file_location("validate_tar_archive", SCRIPT)
VALIDATOR = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(VALIDATOR)


class ValidateTarArchiveTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.archive_path = Path(self.temp.name) / "media.tar.gz"

    def tearDown(self) -> None:
        self.temp.cleanup()

    def create_archive(self, entries: list[tuple[tarfile.TarInfo, bytes | None]]) -> None:
        with tarfile.open(self.archive_path, "w:gz") as archive:
            for info, contents in entries:
                archive.addfile(info, io.BytesIO(contents) if contents is not None else None)

    def file(self, name: str, contents: bytes = b"image") -> tuple[tarfile.TarInfo, bytes]:
        info = tarfile.TarInfo(name)
        info.size = len(contents)
        return info, contents

    def test_accepts_backup_tar_with_dot_root_and_nested_media(self) -> None:
        root = tarfile.TarInfo("./")
        root.type = tarfile.DIRTYPE
        folder = tarfile.TarInfo("./employees/")
        folder.type = tarfile.DIRTYPE
        self.create_archive([ (root, None), (folder, None), self.file("./employees/avatar.webp") ])
        self.assertEqual((3, 5), VALIDATOR.validate_archive(str(self.archive_path)))

    def test_rejects_parent_traversal_and_absolute_paths(self) -> None:
        for name in ("../outside", "/outside"):
            with self.subTest(name=name):
                self.create_archive([self.file(name)])
                with self.assertRaises(ValueError):
                    VALIDATOR.validate_archive(str(self.archive_path))

    def test_rejects_duplicate_normalized_paths(self) -> None:
        self.create_archive([self.file("./profile.png"), self.file("profile.png")])
        with self.assertRaisesRegex(ValueError, "duplicate archive path"):
            VALIDATOR.validate_archive(str(self.archive_path))

    def test_rejects_links_and_special_entries(self) -> None:
        link = tarfile.TarInfo("avatar-link")
        link.type = tarfile.SYMTYPE
        link.linkname = "../outside"
        self.create_archive([(link, None)])
        with self.assertRaisesRegex(ValueError, "unsupported archive entry type"):
            VALIDATOR.validate_archive(str(self.archive_path))

    def test_rejects_file_directory_conflicts(self) -> None:
        self.create_archive([self.file("folder/file.png"), self.file("folder")])
        with self.assertRaisesRegex(ValueError, "conflicts with an existing directory"):
            VALIDATOR.validate_archive(str(self.archive_path))

    def test_rejects_expanded_size_over_limit(self) -> None:
        self.create_archive([self.file("large.bin", b"12345")])
        original = VALIDATOR.MAX_EXPANDED_BYTES
        try:
            VALIDATOR.MAX_EXPANDED_BYTES = 4
            with self.assertRaisesRegex(ValueError, "expanded-size limit"):
                VALIDATOR.validate_archive(str(self.archive_path))
        finally:
            VALIDATOR.MAX_EXPANDED_BYTES = original

    def test_rejects_entry_count_over_limit(self) -> None:
        self.create_archive([self.file("one"), self.file("two")])
        original = VALIDATOR.MAX_ENTRIES
        try:
            VALIDATOR.MAX_ENTRIES = 1
            with self.assertRaisesRegex(ValueError, "entry limit"):
                VALIDATOR.validate_archive(str(self.archive_path))
        finally:
            VALIDATOR.MAX_ENTRIES = original


if __name__ == "__main__":
    unittest.main()
