"""
Generates the pixel art for the tilemap and the villagers:

  Content/tileset.png  - five 16x16 tiles in a row: the road with its glowing seam of lava,
                         a cracked piece of road, and the left end, middle and right end of
                         a stone ledge. In a map file they are numbered 1 to 5, and 0 is empty
  Content/villager.png - two 16x24 frames of a villager, one standing and one waving both
                         arms for help

Uses only the standard library, and borrows write_png from the particle texture script:

    python3 Tools/generate_tiles.py
"""

import os
import random

from generate_particle_textures import CONTENT, write_png

TILE = 16

# The road keeps the colors Ground.cs used to draw it with: dark basalt and a lava seam
ROCK = (38, 26, 28, 255)
ROCK_LIGHT = (58, 40, 40, 255)
ROCK_DARK = (24, 16, 18, 255)
LAVA_HOT = (255, 190, 80, 255)
LAVA = (255, 90, 20, 255)
LAVA_DIM = (140, 40, 18, 255)

# The ledges are a lighter, ash covered stone so they stand out from the road
ASH = (168, 156, 148, 255)
STONE_LIGHT = (122, 104, 96, 255)
STONE = (86, 70, 66, 255)
STONE_DARK = (52, 40, 40, 255)
OUTLINE = (24, 16, 18, 255)
CLEAR = (0, 0, 0, 0)

# How tall the ledge slab is. The rest of its tile is left empty, so a ledge looks like a
# thin shelf rather than a solid block
LEDGE_HEIGHT = 11


def speckle(tile, rng, rows, base, light, dark):
    """Scatters lighter and darker pixels over the given rows of a tile, so it isn't flat"""
    for y in rows:
        for x in range(TILE):
            if tile[y][x] != base:
                continue
            roll = rng.random()
            if roll < 0.12:
                tile[y][x] = light
            elif roll < 0.22:
                tile[y][x] = dark


def road(cracked=False):
    """The road: a seam of lava along the top, cooling into dark rock below"""
    rng = random.Random(580 if not cracked else 581)
    tile = [[ROCK] * TILE for _ in range(TILE)]
    tile[0] = [LAVA_HOT] * TILE
    tile[1] = [LAVA] * TILE
    tile[2] = [LAVA_DIM] * TILE
    speckle(tile, rng, range(3, TILE), ROCK, ROCK_LIGHT, ROCK_DARK)

    if cracked:
        # a crack running down from the seam, glowing where it is hottest near the top
        crack = [(7, 3), (7, 4), (8, 5), (8, 6), (7, 7), (6, 8), (6, 9), (7, 10), (7, 11)]
        for i, (x, y) in enumerate(crack):
            tile[y][x] = LAVA if i < 4 else LAVA_DIM
    return tile


def ledge(left_end=False, right_end=False):
    """One piece of a stone ledge. The end pieces get a dark outline and rounded corners"""
    rng = random.Random(582 + left_end * 2 + right_end)
    tile = [[CLEAR] * TILE for _ in range(TILE)]
    for x in range(TILE):
        tile[0][x] = ASH if rng.random() < 0.7 else STONE_LIGHT
        tile[1][x] = STONE_LIGHT
        for y in range(2, LEDGE_HEIGHT - 2):
            tile[y][x] = STONE
        tile[LEDGE_HEIGHT - 2][x] = STONE_DARK
        tile[LEDGE_HEIGHT - 1][x] = OUTLINE
    speckle(tile, rng, range(2, LEDGE_HEIGHT - 2), STONE, STONE_LIGHT, STONE_DARK)

    for end, x in ((left_end, 0), (right_end, TILE - 1)):
        if not end:
            continue
        for y in range(LEDGE_HEIGHT):
            tile[y][x] = OUTLINE
        tile[0][x] = CLEAR
        tile[LEDGE_HEIGHT - 1][x] = CLEAR
    return tile


def tileset():
    """Lays the five tiles out side by side, in the order the map files number them"""
    tiles = [road(), road(cracked=True), ledge(left_end=True), ledge(), ledge(right_end=True)]
    rows = [[px for tile in tiles for px in tile[y]] for y in range(TILE)]
    write_png(os.path.join(CONTENT, "tileset.png"), TILE * len(tiles), TILE, rows)


# The villager is drawn as text, one character per pixel
PALETTE = {
    ".": CLEAR,
    "k": (30, 22, 26, 255),     # outline
    "s": (236, 188, 146, 255),  # skin
    "h": (110, 66, 40, 255),    # hair
    "t": (64, 120, 200, 255),   # tunic
    "d": (44, 86, 150, 255),    # tunic hem
    "b": (92, 60, 36, 255),     # belt
    "p": (70, 56, 50, 255),     # trousers
    "o": (60, 40, 30, 255),     # boots
}

HEAD = [
    "......kkkk......",
    ".....khhhhk.....",
    "....khhhhhhk....",
    "....khsssshk....",
    "....kskssksk....",
    "....kssssssk....",
    ".....kssssk.....",
    "......kssk......",
]

LEGS = [
    "....kddddddk....",
    "....kkkkkkkk....",
    ".....kpk.kpk....",
    ".....kpk.kpk....",
    ".....kpk.kpk....",
    "....kook.kook...",
    "....kkkk.kkkk...",
]

STANDING = ["................", "................"] + HEAD + [
    "...kkttttttkk...",
    "..ktkttttttktk..",
    "..ktkttttttktk..",
    "..ktkttttttktk..",
    "..kskbbbbbbksk..",
    "...kkttttttkk...",
    "....kttttttk....",
] + LEGS

WAVING = [
    "................",
    "..k..........k..",
    ".ksk..kkkk..ksk.",
    ".ksk.khhhhk.ksk.",
    ".ktkkhhhhhhkktk.",
    ".ktkkhsssshkktk.",
    ".ktkkskssksk" + "ktk.",
    ".ktkkssssssk" + "ktk.",
    ".ktk.kssssk.ktk.",
    ".ktk..kssk..ktk.",
    "..kkkttttttkkk..",
    "....kttttttk....",
    "....kttttttk....",
    "....kttttttk....",
    "....kbbbbbbk....",
    "....kttttttk....",
    "....kttttttk....",
] + LEGS


def villager():
    """Puts the standing and waving frames side by side"""
    frames = [STANDING, WAVING]
    for frame in frames:
        assert len(frame) == 24, len(frame)
        for line in frame:
            assert len(line) == TILE, line
    rows = [[PALETTE[c] for frame in frames for c in frame[y]] for y in range(24)]
    write_png(os.path.join(CONTENT, "villager.png"), TILE * len(frames), 24, rows)


if __name__ == "__main__":
    tileset()
    villager()
    print("wrote tileset.png and villager.png to Content/")
