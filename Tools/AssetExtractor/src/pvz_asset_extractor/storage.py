"""Filesystem safety, deterministic indexes, and restartable extraction state."""
from __future__ import annotations

import csv
import hashlib
import json
import os
import re
from contextlib import contextmanager
from pathlib import Path, PureWindowsPath

from PIL import Image


class WorkspaceError(ValueError):
    """Unsafe or mismatched output; abort instead of treating as an asset failure."""


def safe_name(value: str, fallback: str = "asset") -> str:
    value = re.sub(r'[<>:"/\\|?*\x00-\x1f\x7f]', "_", str(value or ""))
    value = value.strip(" .")[:80].rstrip(" .") or fallback
    if re.fullmatch(r"CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³]", value.split(".")[0], re.I):
        value = "_" + value
    return value


def file_hash(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def input_directory(value: str | Path) -> Path:
    path = Path(value).resolve(strict=True)
    if not path.is_dir():
        raise ValueError("Input must be an existing directory")
    return path


def output_directory(source: Path, value: str | Path) -> Path:
    path = Path(value).resolve()
    if path.is_relative_to(source) or source.is_relative_to(path):
        raise ValueError("Input and output directories must not overlap")
    return path


def contained(root: Path, relative: str) -> Path:
    win = PureWindowsPath(relative)
    if win.drive or win.root or ".." in win.parts or "\\" in relative or ":" in relative:
        raise WorkspaceError("Unsafe output path: " + relative)
    target = (root / relative).resolve()
    if target == root.resolve() or not target.is_relative_to(root.resolve()):
        raise WorkspaceError("Output path escapes workspace: " + relative)
    return target


def image_path(kind: str, source: str, path_id: int, name: str) -> str:
    # The full source identity hash also separates names that sanitize identically.
    folder = safe_name(source) + "_" + hashlib.sha256(source.encode()).hexdigest()[:16]
    category = "Texture2D" if kind == "Texture2D" else "Sprites"
    return f"{category}/{folder}/{int(path_id)}_{safe_name(name, kind)}.png"


def atomic_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + ".tmp")
    # Refuse symlinked temporary files as well as escaped final targets.
    if temp.is_symlink():
        raise ValueError("Symlinked temporary file: " + str(temp))
    with temp.open("w", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2, sort_keys=True, allow_nan=False)
        stream.write("\n")
    temp.replace(path)


def snapshot(source: Path) -> dict:
    result = {}
    for path in sorted(source.rglob("*")):
        if path.is_file():
            if not path.resolve().is_relative_to(source):
                raise ValueError("Input contains an external file link: " + str(path))
            result[path.relative_to(source).as_posix()] = {"size": path.stat().st_size, "sha256": file_hash(path)}
    return result


def asset_key(entry: dict) -> str:
    return json.dumps([entry["sourceFile"], entry["pathId"], entry["type"]], ensure_ascii=False)


def validate_png(path: Path, entry: dict) -> None:
    with Image.open(path) as image:
        if image.format != "PNG":
            raise ValueError("Not a PNG")
        image.verify()
    with Image.open(path) as image:
        image.load()
        if min(image.size) <= 0 or image.size != (entry["width"], entry["height"]):
            raise ValueError(f"PNG dimensions disagree: {image.size}")
    if file_hash(path) != entry["sha256"]:
        raise ValueError("PNG SHA-256 mismatch")


def reusable(root: Path, entry: dict | None) -> bool:
    if not entry or entry.get("status") != "exported":
        return False
    path = contained(root, entry["exportedFile"])
    if not path.exists():
        return False
    try:
        validate_png(path, entry)
    except Exception as exc:
        raise WorkspaceError("Existing PNG is invalid; use --overwrite: " + str(path)) from exc
    return True


def mark_duplicates(entries: list[dict]) -> None:
    seen = set()
    for entry in entries:
        digest = entry.get("sha256")
        if digest:
            entry["duplicateContent"] = digest in seen
            if digest in seen:
                entry["duplicateOfContentHash"] = digest
            seen.add(digest)


CSV_FIELDS = ["type", "name", "sourceFile", "pathId", "width", "height", "format",
              "texturePathId", "sha256", "exportedFile", "status", "error"]


def write_csv(path: Path, entries: list[dict]) -> None:
    # UTF-8 BOM for Excel; neutralize spreadsheet formula execution in untrusted names.
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=CSV_FIELDS, extrasaction="ignore")
        writer.writeheader()
        for entry in entries:
            row = {key: value for key, value in entry.items() if key in CSV_FIELDS}
            for key, value in row.items():
                if isinstance(value, str) and value.lstrip().startswith(("=", "+", "-", "@")):
                    row[key] = "'" + value
            writer.writerow(row)


@contextmanager
def workspace_lock(root: Path):
    root.mkdir(parents=True, exist_ok=True)
    with contained(root, ".extractor.lock").open("a+b") as stream:
        stream.seek(0)
        if os.name == "nt":
            import msvcrt
            if not stream.read(1):
                stream.write(b"0")
                stream.flush()
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl
            fcntl.flock(stream.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        try:
            yield
        finally:
            stream.seek(0)
            if os.name == "nt":
                msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(stream.fileno(), fcntl.LOCK_UN)


class State:
    def __init__(self, root: Path, identity: dict):
        self.root = root
        self.entries = {}
        header = contained(root, "Reports/source_identity.json")
        journal = contained(root, "Reports/progress.jsonl")
        if header.exists():
            if json.loads(header.read_text(encoding="utf-8")) != identity:
                raise ValueError("Source data or decoder changed; use a new output directory")
        else:
            if any(p.name != ".extractor.lock" for p in root.iterdir()):
                raise ValueError("Nonempty output has no matching source identity; choose a new output directory")
            atomic_json(header, identity)
        if journal.exists():
            # Drop only an incomplete final line from an interrupted append.
            with journal.open("r+b") as stream:
                while True:
                    start = stream.tell()
                    line = stream.readline()
                    if not line:
                        break
                    if not line.endswith(b"\n"):
                        stream.truncate(start)
                        break
                    entry = json.loads(line)
                    self.entries[asset_key(entry)] = entry
        self.journal = journal

    def record(self, entry: dict) -> None:
        with self.journal.open("a", encoding="utf-8", newline="\n") as stream:
            stream.write(json.dumps(entry, ensure_ascii=False, allow_nan=False) + "\n")
            stream.flush()
            os.fsync(stream.fileno())
        self.entries[asset_key(entry)] = entry
