#!/usr/bin/env python3
"""Validate a media/key tar.gz before any production restore extracts it."""

from __future__ import annotations

import sys
import tarfile
from pathlib import PurePosixPath

MAX_ENTRIES = 250_000
MAX_EXPANDED_BYTES = 64 * 1024 * 1024 * 1024
MAX_PATH_LENGTH = 1024
CHUNK_SIZE = 128 * 1024


def normalize_member_name(name: str, is_directory: bool) -> str:
    if not name or len(name) > MAX_PATH_LENGTH or name.startswith("/") or "\\" in name or ":" in name:
        raise ValueError(f"unsafe archive path: {name!r}")
    raw_parts = name.split("/")
    while raw_parts and raw_parts[0] == ".":
        raw_parts.pop(0)  # GNU tar commonly prefixes entries with ./.
    if is_directory and raw_parts and raw_parts[-1] == "":
        raw_parts.pop()
    if not raw_parts and is_directory:
        return ""  # GNU tar may include the archive root itself as ./.
    if not raw_parts or any(part in ("", ".", "..") for part in raw_parts):
        raise ValueError(f"unsafe archive path: {name!r}")
    if any(any(ord(char) < 32 or ord(char) == 127 for char in part) for part in raw_parts):
        raise ValueError(f"control character in archive path: {name!r}")
    normalized = "/".join(raw_parts)
    if len(normalized) > MAX_PATH_LENGTH:
        raise ValueError(f"archive path exceeds {MAX_PATH_LENGTH} characters")
    return normalized


def validate_archive(path: str) -> tuple[int, int]:
    names: set[str] = set()
    files: set[str] = set()
    directories: set[str] = set()
    required_directories: set[str] = set()
    count = 0
    expanded_bytes = 0
    with tarfile.open(path, mode="r|gz") as archive:
        for member in archive:
            count += 1
            if count > MAX_ENTRIES:
                raise ValueError(f"archive exceeds the {MAX_ENTRIES:,}-entry limit")
            if not (member.isfile() or member.isdir()):
                raise ValueError(f"unsupported archive entry type: {member.name!r}")
            if member.isdir() and member.size != 0:
                raise ValueError(f"directory entry has a payload: {member.name!r}")
            normalized = normalize_member_name(member.name, member.isdir())
            if normalized in names:
                raise ValueError(f"duplicate archive path: {normalized!r}")
            if normalized in files or (not member.isdir() and (normalized in directories or normalized in required_directories)):
                raise ValueError(f"archive file conflicts with an existing directory path: {normalized!r}")
            names.add(normalized)
            (directories if member.isdir() else files).add(normalized)
            parts = PurePosixPath(normalized).parts
            for index in range(1, len(parts)):
                ancestor = "/".join(parts[:index])
                if ancestor in files:
                    raise ValueError(f"file is used as an archive directory: {ancestor!r}")
                required_directories.add(ancestor)
            if member.size < 0 or expanded_bytes > MAX_EXPANDED_BYTES - member.size:
                raise ValueError(f"archive exceeds the {MAX_EXPANDED_BYTES // (1024 ** 3)} GiB expanded-size limit")
            expanded_bytes += member.size
            if member.isfile():
                stream = archive.extractfile(member)
                if stream is None:
                    raise ValueError(f"archive file could not be read: {member.name!r}")
                actual_size = 0
                with stream:
                    while chunk := stream.read(CHUNK_SIZE):
                        actual_size += len(chunk)
                        if actual_size > member.size or expanded_bytes - member.size + actual_size > MAX_EXPANDED_BYTES:
                            raise ValueError(f"archive file exceeds its declared or allowed size: {member.name!r}")
                if actual_size != member.size:
                    raise ValueError(f"archive file size does not match its header: {member.name!r}")
    return count, expanded_bytes


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: validate-tar-archive.py <archive.tar.gz>", file=sys.stderr)
        raise SystemExit(2)
    try:
        entry_count, expanded_size = validate_archive(sys.argv[1])
    except (OSError, tarfile.TarError, ValueError) as error:
        print(f"Unsafe or unreadable archive: {error}", file=sys.stderr)
        raise SystemExit(1)
    print(f"Tar archive accepted: {entry_count} entries, {expanded_size} expanded bytes.")
