"""
Generates the two particle textures the tutorial's art doesn't cover:

  Content/smoke.png  - a lumpy, soft-edged white puff, used for the dust clouds. It is white
                       so the game can tint it, and lumpy so you can see it turn as it rotates
  Content/shard.png  - a jagged white rock chip, used for the debris and the falling ash

Uses only the standard library, so it runs anywhere Python 3 does:

    python3 Tools/generate_particle_textures.py
"""

import math
import os
import random
import struct
import zlib

CONTENT = os.path.join(os.path.dirname(__file__), "..", "Content")


def write_png(path, width, height, pixels):
    """Writes a list of (r, g, b, a) rows out as an RGBA PNG"""
    raw = b"".join(b"\x00" + bytes(c for px in row for c in px) for row in pixels)

    def chunk(kind, data):
        return (struct.pack(">I", len(data)) + kind + data
                + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF))

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(raw, 9)))
        f.write(chunk(b"IEND", b""))


def smoke(size=64):
    """A few overlapping soft blobs, so the outline is bumpy rather than a perfect circle"""
    rng = random.Random(580)
    blobs = [(0.5, 0.5, 0.30)]
    for _ in range(6):
        angle = rng.uniform(0, math.tau)
        blobs.append((0.5 + math.cos(angle) * 0.17, 0.5 + math.sin(angle) * 0.17,
                      rng.uniform(0.13, 0.2)))

    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            u, v = (x + 0.5) / size, (y + 0.5) / size
            density = 0.0
            for bx, by, r in blobs:
                d = math.hypot(u - bx, v - by) / r
                density += math.exp(-d * d * 2.2)
            alpha = min(1.0, density) ** 1.4
            # fade to nothing well before the edge so the square never shows
            edge = math.hypot(u - 0.5, v - 0.5) / 0.5
            alpha *= max(0.0, 1 - edge ** 4)
            row.append((255, 255, 255, int(alpha * 255)))
        rows.append(row)
    write_png(os.path.join(CONTENT, "smoke.png"), size, size, rows)


def shard(size=16):
    """An irregular seven-sided chip of rock, filled solid with softened edges"""
    rng = random.Random(1580)
    points = []
    for i in range(7):
        angle = i / 7 * math.tau + rng.uniform(-0.3, 0.3)
        r = rng.uniform(0.25, 0.48) * size
        points.append((size / 2 + math.cos(angle) * r, size / 2 + math.sin(angle) * r))

    def inside(px, py):
        hit = False
        for i in range(len(points)):
            (x1, y1), (x2, y2) = points[i], points[i - 1]
            if (y1 > py) != (y2 > py) and px < (x2 - x1) * (py - y1) / (y2 - y1) + x1:
                hit = not hit
        return hit

    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            # 4x4 samples per pixel for smooth edges
            covered = sum(inside(x + (i + 0.5) / 4, y + (j + 0.5) / 4)
                          for i in range(4) for j in range(4))
            row.append((255, 255, 255, covered * 255 // 16))
        rows.append(row)
    write_png(os.path.join(CONTENT, "shard.png"), size, size, rows)


if __name__ == "__main__":
    smoke()
    shard()
    print("wrote smoke.png and shard.png to Content/")
