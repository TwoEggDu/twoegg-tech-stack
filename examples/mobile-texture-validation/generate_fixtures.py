"""Generate original numeric fixtures; this does not run Unity or encode ASTC."""

from pathlib import Path
import hashlib
import json
import struct
import zlib


SIZE = 1024
OUTPUT = Path(__file__).resolve().parent / "fixtures"


def chunk(kind, data):
    return (struct.pack(">I", len(data)) + kind + data
            + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF))


def png_bytes(rows):
    header = struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0)
    # RGBA8 PNG, filter 0, no profile/gamma metadata: Unity import semantics
    # must be explicitly recorded instead of inferred from this container.
    pixels = b"".join(b"\x00" + row for row in rows)
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header)
            + chunk(b"IDAT", zlib.compress(pixels, 9)) + chunk(b"IEND", b""))


def base_rgb(x, y):
    band = y * 4 // SIZE
    if band == 0:
        width = (1, 2, 4, 8)[min(3, x * 4 // SIZE)]
        value = 230 if (x // width) % 2 else 25
        return value, value, value
    if band == 1:
        return (210, 80, 40) if ((x // 16) + (y // 16)) % 2 else (35, 145, 205)
    if band == 2:
        return (235, 220, 70) if (x + y) % 32 < 3 else (35, 45, 60)
    u = round(255 * x / (SIZE - 1))
    v = round(255 * (y % (SIZE // 4)) / (SIZE // 4 - 1))
    return u, 255 - u, v


def rows_for(kind):
    rows = []
    for y in range(SIZE):
        row = bytearray()
        for x in range(SIZE):
            if kind == "mask":
                # R: metallic candidate; G: occlusion candidate;
                # B: diagnostic signal; A: smoothness candidate.
                pixel = (round(255 * x / (SIZE - 1)),
                         48 if ((x // 32) + (y // 32)) % 2 else 255,
                         255 if (x // 8) % 2 else 0,
                         round(255 * y / (SIZE - 1)))
            else:
                alpha = round(255 * x / (SIZE - 1)) if kind == "alpha" else 255
                pixel = (*base_rgb(x, y), alpha)
            row.extend(pixel)
        rows.append(bytes(row))
    return rows


def write_texture(name, kind):
    rows = rows_for(kind)
    data = png_bytes(rows)
    (OUTPUT / name).write_bytes(data)
    channels = [hashlib.sha256(b"".join(row[c::4] for row in rows)).hexdigest()
                for c in range(4)]
    return {"path": name, "width": SIZE, "height": SIZE,
            "source_encoding": "PNG RGBA8", "png_bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest(),
            "decoded_channel_sha256": dict(zip("RGBA", channels)),
            "unity_import_status": "NOT_EXECUTED",
            "android_runtime_status": "NOT_EXECUTED"}


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    textures = [write_texture("base-alpha.png", "alpha"),
                write_texture("base-opaque.png", "opaque"),
                write_texture("metal-ao-smooth.png", "mask")]
    for channel in "RGB":
        assert (textures[0]["decoded_channel_sha256"][channel]
                == textures[1]["decoded_channel_sha256"][channel])
    assert textures[0]["decoded_channel_sha256"]["A"] != textures[1]["decoded_channel_sha256"]["A"]
    model = ("# Original UV diagnostic board, 2 x 2 source units, front normal +Z\n"
             "o UVBoard\n"
             "v -1 -1 0\nv 1 -1 0\nv 1 1 0\nv -1 1 0\n"
             "vt 0 0\nvt 1 0\nvt 1 1\nvt 0 1\nvn 0 0 1\n"
             "f 1/1/1 2/2/1 3/3/1\nf 1/1/1 3/3/1 4/4/1\n")
    (OUTPUT / "uv-board.obj").write_bytes(model.encode("ascii"))
    manifest = {"schema_version": 1, "generator": "generate_fixtures.py",
                "origin": "Original deterministic numeric patterns and mesh; no external art inputs.",
                "status": "SOURCE_FIXTURES_ONLY",
                "not_results": ["Unity import", "ASTC encoding", "build size",
                                "runtime format", "rendered quality", "performance"],
                "textures": textures,
                "model": {"path": "uv-board.obj", "source_vertices": 4,
                          "source_triangles": 2,
                          "sha256": hashlib.sha256(model.encode("ascii")).hexdigest(),
                          "unity_import_status": "NOT_EXECUTED"}}
    (OUTPUT / "source-manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"status": manifest["status"], "textures": len(textures),
                      "model_triangles": 2, "alpha_pair_RGB_identical": True}))


if __name__ == "__main__":
    main()
