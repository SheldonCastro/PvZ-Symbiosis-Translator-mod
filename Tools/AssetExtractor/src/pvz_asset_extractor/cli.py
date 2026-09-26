from __future__ import annotations

import argparse
from collections import Counter
import json
import time
from pathlib import Path

import PIL
import UnityPy

from . import __version__
from .storage import (State, WorkspaceError, asset_key, atomic_json, contained, file_hash, image_path,
                      input_directory, mark_duplicates, output_directory, reusable,
                      safe_name, snapshot, validate_png, workspace_lock, write_csv)
from .unity import discover, metadata, release, serialized_files, source_name, trim_image_cache, version_info

KINDS = ("Texture2D", "Sprite", "SpriteAtlas")


def failure(exc, **identity):
    return dict(identity, errorCategory=type(exc).__name__, error=str(exc))


def scan(source: Path, verbose=False) -> dict:
    start = time.monotonic()
    containers = discover(source)
    summary = dict(containersFound=len(containers), containersLoaded=0, containersFailed=0,
                   objects={}, errors=[], **version_info(containers))
    counts = Counter()
    versions = set(summary["unityVersions"])
    for index, path in enumerate(containers, 1):
        env = None
        try:
            env = UnityPy.load(str(path))
            files = list(serialized_files(env))
            if not files:
                raise ValueError("No serialized members loaded")
            for file in files:
                versions.add(file.unity_version)
                counts.update(obj.type.name for obj in file.objects.values())
            summary["containersLoaded"] += 1
        except Exception as exc:
            summary["containersFailed"] += 1
            summary["errors"].append(failure(exc, sourceFile=path.relative_to(source).as_posix()))
        finally:
            if env is not None:
                release(env)
        if verbose or index % 10 == 0 or index == len(containers):
            print(f"[Scan] {index}/{len(containers)} containers", flush=True)
    summary.update(objects=dict(sorted(counts.items())), unityVersions=sorted(versions),
                   elapsedSeconds=round(time.monotonic()-start, 3),
                   note="Inventory only; object deserialization and image decoding are verified by extract.")
    return summary


def export_object(obj, source_file, root, state, overwrite):
    entry = {"sourceFile": source_file, "pathId": int(obj.path_id), "type": obj.type.name, "name": ""}
    previous = state.entries.get(asset_key(entry))
    if not overwrite and previous and previous.get("status") == "metadata":
        return dict(previous), True
    if not overwrite and reusable(root, previous):
        return dict(previous), True
    image = None
    try:
        data = obj.read()
        entry = metadata(obj, data, source_file)
        if obj.type.name == "SpriteAtlas":
            entry["status"] = "metadata"
            return entry, False
        relative = image_path(obj.type.name, source_file, obj.path_id, entry["name"])
        path = contained(root, relative)
        # An orphan image from an interrupted write is reused only if decoding proves identical.
        image = data.image
        if min(image.size) <= 0:
            raise ValueError("Decoded dimensions must be positive")
        if obj.type.name == "Texture2D" and image.size != (entry["width"], entry["height"]):
            raise ValueError("Decoded texture dimensions differ from Unity metadata")
        entry.update(width=image.width, height=image.height, exportedFile=relative)
        path.parent.mkdir(parents=True, exist_ok=True)
        temp = contained(root, relative + ".tmp")
        image.save(temp, format="PNG")
        digest = file_hash(temp)
        if path.exists() and not overwrite:
            if file_hash(path) != digest:
                temp.unlink()
                raise FileExistsError("Existing PNG differs; use --overwrite or a new output directory")
            temp.unlink()
        else:
            temp.replace(path)
        entry.update(sha256=digest, status="exported")
        validate_png(path, entry)
        return entry, False
    except WorkspaceError:
        raise
    except Exception as exc:
        entry.update(status="failed", **failure(exc))
        return entry, False
    finally:
        if image is not None:
            image.close()


def extract(source, output, game_version=None, overwrite=False, verbose=False):
    start = time.monotonic()
    containers = discover(source)
    info = version_info(containers)
    version = game_version or info["detectedGameVersion"] or "unknown"
    root = contained(output, safe_name(version))
    print(f"[Input] Hashing input files; game version={version}", flush=True)
    before = snapshot(source)
    identity = {"schemaVersion": 1, "extractorVersion": __version__, "unityPyVersion": UnityPy.__version__,
                "pillowVersion": PIL.__version__, "gameVersion": version, "files": before}
    with workspace_lock(root):
        state = State(root, identity)
        summary = dict(containersFound=len(containers), containersLoaded=0, containersFailed=0,
                       gameVersion=version, gameVersionSource="override" if game_version else "metadata" if info["detectedGameVersion"] else "unknown",
                       **info, objectsFound={kind: 0 for kind in KINDS}, exported={kind: 0 for kind in KINDS},
                       failed={kind: 0 for kind in KINDS}, reused={kind: 0 for kind in KINDS})
        versions = set(info["unityVersions"])
        entries, failures, others = [], [], Counter()
        for index, path in enumerate(containers, 1):
            env = None
            try:
                env = UnityPy.load(str(path))
                files = list(serialized_files(env))  # Snapshot excludes later dependency loads.
                if not files:
                    raise ValueError("No serialized members loaded")
                summary["containersLoaded"] += 1
                for file in files:
                    versions.add(file.unity_version)
                    source_file = source_name(file, path, source)
                    objects = sorted(file.objects.values(), key=lambda obj: obj.path_id)
                    for obj in objects:
                        kind = obj.type.name
                        if kind not in KINDS:
                            others[kind] += 1
                            continue
                        summary["objectsFound"][kind] += 1
                        entry, reused = export_object(obj, source_file, root, state, overwrite)
                        if not reused:
                            state.record(entry)
                        entries.append(entry)
                        if entry["status"] == "failed":
                            summary["failed"][kind] += 1
                            failures.append(entry)
                        else:
                            summary["exported"][kind] += 1
                            summary["reused"][kind] += int(reused)
                        trim_image_cache(env)
                        total = sum(summary["objectsFound"].values())
                        if verbose or total % 250 == 0:
                            print(f"[Assets] {total} processed; textures={summary['exported']['Texture2D']}; sprites={summary['exported']['Sprite']}; failures={len(failures)}", flush=True)
            except WorkspaceError:
                raise
            except Exception as exc:
                summary["containersFailed"] += 1
                failures.append(failure(exc, sourceFile=path.relative_to(source).as_posix()))
            finally:
                if env is not None:
                    release(env)
            print(f"[Containers] {index}/{len(containers)} processed", flush=True)
        entries.sort(key=lambda entry: (entry["sourceFile"], entry["pathId"], entry["type"]))
        mark_duplicates(entries)
        for kind, filename in (("Texture2D", "textures"), ("Sprite", "sprites"), ("SpriteAtlas", "atlases")):
            atomic_json(contained(root, f"Manifests/{filename}.json"), [entry for entry in entries if entry["type"] == kind])
        write_csv(contained(root, "Manifests/assets.csv"), entries)
        print("[Verify] Reopening every exported PNG and checking source hashes", flush=True)
        png_errors = []
        for entry in entries:
            if entry["status"] == "exported":
                try:
                    validate_png(contained(root, entry["exportedFile"]), entry)
                except Exception as exc:
                    png_errors.append(failure(exc, sourceFile=entry["sourceFile"], pathId=entry["pathId"], type=entry["type"]))
        unchanged = snapshot(source) == before
        summary.update(inputUnchanged=unchanged, pngValidation="FAIL" if png_errors else "PASS",
                       pngValidated=sum(entry["status"] == "exported" for entry in entries),
                       otherObjectTypes=dict(sorted(others.items())), unityVersions=sorted(versions),
                       unityPyVersion=UnityPy.__version__, pillowVersion=PIL.__version__,
                       output=str(root), totalPngBytes=sum(contained(root, entry["exportedFile"]).stat().st_size for entry in entries if entry["status"] == "exported"),
                       elapsedSeconds=round(time.monotonic()-start, 3))
        failures.extend(png_errors)
        summary["status"] = "FAIL" if not unchanged or png_errors else "PARTIAL" if failures else "PASS"
        if not containers:
            summary["status"] = "FAIL"
        atomic_json(contained(root, "Reports/failures.json"), failures)
        atomic_json(contained(root, "Reports/summary.json"), summary)
        return summary


def main(argv=None):
    parser = argparse.ArgumentParser(description="Read-only offline Unity texture and sprite extractor")
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("scan", "extract"):
        command = commands.add_parser(name)
        command.add_argument("--input", required=True)
        command.add_argument("--verbose", action="store_true")
        if name == "extract":
            command.add_argument("--output", required=True)
            command.add_argument("--game-version")
            command.add_argument("--overwrite", action="store_true", help="Regenerate images for the same verified input dataset")
    args = parser.parse_args(argv)
    try:
        source = input_directory(args.input)
        if args.command == "scan":
            summary = scan(source, args.verbose)
            code = 2 if summary["containersFailed"] or not summary["containersFound"] else 0
        else:
            output = output_directory(source, args.output)
            summary = extract(source, output, args.game_version, args.overwrite, args.verbose)
            code = {"PASS": 0, "PARTIAL": 2, "FAIL": 1}[summary["status"]]
        print(json.dumps(summary, ensure_ascii=False, indent=2), flush=True)
        return code
    except (Exception, KeyboardInterrupt) as exc:
        print(f"ERROR {type(exc).__name__}: {exc}", flush=True)
        return 1
