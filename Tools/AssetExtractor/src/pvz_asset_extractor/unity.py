"""UnityPy adapter. Only reads local containers; never calls UnityPy save APIs."""
from __future__ import annotations

import gc
import re
import struct
from pathlib import Path

import UnityPy
from PIL import Image
from UnityPy.files import SerializedFile


def discover(source: Path) -> list[Path]:
    candidates = []
    for path in sorted(source.rglob("*")):
        if not path.is_file() or path.suffix.lower() in {".ress", ".resource"}:
            continue
        known = path.suffix.lower() in {".assets", ".bundle", ".unity3d"} or bool(
            re.fullmatch(r"globalgamemanagers|level\d+", path.name))
        with path.open("rb") as stream:
            header = stream.read(64)
        signature = header.startswith((b"UnityFS\0", b"UnityRaw\0", b"UnityWeb\0", b"UnityWebData"))
        serialized = False
        if len(header) >= 48:
            meta, size, version, offset = struct.unpack(">4I", header[:16])
            if version >= 22:
                meta, size, offset = struct.unpack(">Iqq", header[20:40])
            serialized = 5 <= version <= 100 and 0 < meta <= size == path.stat().st_size and 0 < offset <= size
        if known or signature or serialized:
            candidates.append(path)
    return candidates


def serialized_files(env):
    def walk(node):
        if isinstance(node, SerializedFile):
            yield node
        else:
            for child in (getattr(node, "files", None) or {}).values():
                yield from walk(child)
    yield from walk(env)


def source_name(asset_file, container: Path, source: Path) -> str:
    outer = container.relative_to(source).as_posix()
    # Preserve nested bundle member names, not just basename, as object identity.
    parts = []
    node = asset_file
    while node is not None and node is not getattr(asset_file, "environment", None):
        name = str(getattr(node, "name", ""))
        if Path(name).name == container.name:
            break
        if name:
            parts.append(name.replace("\\", "/"))
        node = getattr(node, "parent", None)
    return outer + ("::" + "::".join(reversed(parts)) if parts else "")


def pointer(value) -> dict | None:
    if value is None or not getattr(value, "m_PathID", 0):
        return None
    result = {"pathId": int(value.m_PathID), "fileId": int(value.m_FileID)}
    asset_file = getattr(value, "assetsfile", None)
    if asset_file:
        if value.m_FileID == 0:
            result["serializedFile"] = asset_file.name
        elif 0 <= value.m_FileID - 1 < len(asset_file.externals):
            result["serializedFile"] = asset_file.externals[value.m_FileID - 1].path
    return result


def coordinates(value, names) -> dict | None:
    if value is None:
        return None
    return {key: getattr(value, key) for key in names if hasattr(value, key)}


def render_metadata(data) -> dict:
    result = {}
    for attr in ("texture", "alphaTexture"):
        ref = pointer(getattr(data, attr, None))
        if ref:
            result[attr] = ref
    for attr, fields in (("textureRect", ("x", "y", "width", "height")),
                         ("textureRectOffset", ("x", "y"))):
        value = coordinates(getattr(data, attr, None), fields)
        if value is not None:
            result[attr] = value
    raw = getattr(data, "settingsRaw", None)
    if raw is not None:
        result["settingsRaw"] = int(raw)
        result["packed"] = bool(raw & 1)
        result["packingMode"] = (raw >> 1) & 1
        result["packingRotation"] = (raw >> 2) & 15
    return result


def metadata(obj, data, source_file: str) -> dict:
    entry = {"sourceFile": source_file, "pathId": int(obj.path_id), "type": obj.type.name,
             "name": getattr(data, "m_Name", "")}
    if obj.type.name == "Texture2D":
        entry.update(width=int(data.m_Width), height=int(data.m_Height), format=str(data.m_TextureFormat))
        stream = getattr(data, "m_StreamData", None)
        if stream is not None:
            entry["streamData"] = {key: getattr(stream, key) for key in ("path", "offset", "size") if hasattr(stream, key)}
    elif obj.type.name == "Sprite":
        for attr, key, fields in (("m_Rect", "rect", ("x", "y", "width", "height")),
                                  ("m_Pivot", "pivot", ("x", "y")),
                                  ("m_Border", "border", ("x", "y", "z", "w"))):
            value = coordinates(getattr(data, attr, None), fields)
            if value is not None:
                entry[key] = value
        tags = getattr(data, "m_AtlasTags", None)
        if tags is not None:
            entry["atlasTags"] = tags
        atlas_ptr = getattr(data, "m_SpriteAtlas", None)
        entry["renderData"] = render_metadata(data.m_RD)
        # The atlas render data can override the Sprite's direct texture link.
        atlas_ref = pointer(atlas_ptr)
        if atlas_ref:
            entry["atlas"] = atlas_ref
            try:
                atlas = atlas_ptr.deref_parse_as_object()
                match = next(value for key, value in atlas.m_RenderDataMap if key == data.m_RenderDataKey)
                entry["atlasRenderData"] = render_metadata(match)
            except Exception as exc:
                entry["metadataWarnings"] = [f"Atlas relationship: {type(exc).__name__}: {exc}"]
        texture = entry.get("atlasRenderData", entry["renderData"]).get("texture")
        if texture:
            entry["texturePathId"] = texture["pathId"]
            entry["textureReference"] = texture
    elif obj.type.name == "SpriteAtlas":
        entry["sprites"] = [pointer(p) for p in data.m_PackedSprites if pointer(p)]
        entry["renderData"] = [render_metadata(value) for _, value in data.m_RenderDataMap]
        entry["tag"] = data.m_Tag
        entry["isVariant"] = data.m_IsVariant
    return entry


def trim_image_cache(env, limit=64 * 1024 * 1024):
    # UnityPy caches decoded sprite atlas images; bound those caches across a container.
    files = list(serialized_files(env))
    images = [(file, key, value) for file in files for key, value in file._cache.items() if isinstance(value, Image.Image)]
    size = sum(image.width * image.height * len(image.getbands()) for _, _, image in images)
    if size > limit:
        for file, key, image in images:
            image.close()
            file._cache.pop(key, None)


def release(env):
    trim_image_cache(env, -1)
    # File readers can hold open handles. Closing them is safe only after this container finishes.
    seen = set()
    def close(node):
        if id(node) in seen:
            return
        seen.add(id(node))
        for child in (getattr(node, "files", None) or {}).values():
            close(child)
        reader = getattr(node, "reader", node)
        try:
            reader.close()
        except AttributeError:
            pass
    close(env)
    gc.collect()


def version_info(containers: list[Path]) -> dict:
    versions, unity_versions, errors = set(), set(), []
    for path in containers:
        if path.name != "globalgamemanagers":
            continue
        env = None
        try:
            env = UnityPy.load(str(path))
            for file in serialized_files(env):
                unity_versions.add(file.unity_version)
                for obj in file.objects.values():
                    if obj.type.name == "PlayerSettings":
                        data = obj.read()
                        value = getattr(data, "bundleVersion", None)
                        if value:
                            versions.add(str(value))
        except Exception as exc:
            errors.append(f"{path.name}: {type(exc).__name__}: {exc}")
        finally:
            if env is not None:
                release(env)
    return {"detectedGameVersion": next(iter(versions)) if len(versions) == 1 else None,
            "unityVersions": sorted(unity_versions), "versionDetectionErrors": errors}
