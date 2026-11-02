using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// A level built out of tiles, read from a text file in the Content folder. The file holds the
/// tileset's name, the size of one tile in the tileset, and the size of the map, then one line 
/// of tile numbers for each row of the map, so the file looks like the level. 0 is an empty tile,
/// and every other tile is solid: the knight and the rocks land on top of it
/// </summary>
public class Tilemap
{
    /// <summary>
    /// How much the 16 pixel tiles are blown up by when they are drawn. The villager uses the
    /// same scale, so their pixels match
    /// </summary>
    public const float SCALE = 2.5f;

    /// <summary>
    /// How far below the top of a tile something can be and still land on it. After the knight
    /// is stood on a tile, working out where his feet are can come out a hair below its top,
    /// and without this he would slip through the floor on the next frame. Dropping through a
    /// ledge works by stepping just past this
    /// </summary>
    public const float LANDING_TOLERANCE = 1;

    /// <summary>
    /// the dimensions of tiles in the tileset, and of the map in tiles
    /// </summary>
    private int _tileWidth, _tileHeight, _mapWidth, _mapHeight;

    /// <summary>
    /// the tileset texture
    /// </summary>
    private Texture2D _tilesetTexture;

    /// <summary>
    /// the tile info in the tileset
    /// </summary>
    private Rectangle[] _tiles;

    /// <summary>
    /// the tile map data
    /// </summary>
    private int[] _map;

    /// <summary>
    /// the filename of the map file
    /// </summary>
    private string _filename;

    /// <summary>
    /// How wide one tile is in the world, once it is scaled up
    /// </summary>
    public float TileWidth => _tileWidth * SCALE;

    /// <summary>
    /// How tall one tile is in the world, once it is scaled up
    /// </summary>
    public float TileHeight => _tileHeight * SCALE;

    /// <summary>
    /// Creates a tilemap that will be read from the given file in the Content folder
    /// </summary>
    /// <param name="filename">The map file, like "level1.txt"</param>
    public Tilemap(string filename)
    {
        _filename = filename;
    }

    /// <summary>
    /// Reads the map file, loads its tileset, and cuts the tileset up into tiles
    /// </summary>
    /// <param name="content">The ContentManager to load the tileset with</param>
    public void LoadContent(ContentManager content)
    {
        // TitleContainer finds the file next to the game, wherever the game was started from,
        // the same way the ContentManager finds its .xnb files
        using var stream = TitleContainer.OpenStream(Path.Join(content.RootDirectory, _filename));
        using var reader = new StreamReader(stream);
        string data = reader.ReadToEnd();
        var lines = data.Split('\n');

        // first line is the tileset filename
        var tilesetFilename = lines[0].Trim();
        _tilesetTexture = content.Load<Texture2D>(tilesetFilename);

        // second line is the tile size
        var secondLine = lines[1].Split(',');
        _tileWidth = int.Parse(secondLine[0]);
        _tileHeight = int.Parse(secondLine[1]);

        // now we can determine our tile bounds
        int tilesetColumns = _tilesetTexture.Width / _tileWidth;
        int tilesetRows = _tilesetTexture.Height / _tileHeight;
        _tiles = new Rectangle[tilesetColumns * tilesetRows];

        for (int y = 0; y < tilesetRows; y++)
        {
            for (int x = 0; x < tilesetColumns; x++)
            {
                int index = y * tilesetColumns + x;
                _tiles[index] = new Rectangle(
                    x * _tileWidth,
                    y * _tileHeight,
                    _tileWidth,
                    _tileHeight
                );
            }
        }

        // third line is the map size
        var thirdLine = lines[2].Split(',');
        _mapWidth = int.Parse(thirdLine[0]);
        _mapHeight = int.Parse(thirdLine[1]);

        // the rest of the file is the map, one line for each row
        _map = new int[_mapWidth * _mapHeight];
        for (int y = 0; y < _mapHeight; y++)
        {
            var row = lines[3 + y].Split(',');
            for (int x = 0; x < _mapWidth; x++)
            {
                _map[y * _mapWidth + x] = int.Parse(row[x]);
            }
        }
    }

    /// <summary>
    /// Whether the tile at a spot in the map is solid. Anything off the edges of the map is empty
    /// </summary>
    /// <param name="column">Which column of the map, from the left</param>
    /// <param name="row">Which row of the map, from the top</param>
    /// <returns>True if something can land on this tile</returns>
    public bool IsSolid(int column, int row)
    {
        if (column < 0 || column >= _mapWidth || row < 0 || row >= _mapHeight) return false;
        return _map[row * _mapWidth + column] != 0;
    }

    /// <summary>
    /// Checks whether something moving down this frame dropped onto the top of a tile. Tiles
    /// only stop things coming down onto them, so the knight can jump up through a ledge and
    /// walk underneath one, and a ledge catches the rocks falling on it
    /// </summary>
    /// <param name="left">The left edge of the thing that is falling</param>
    /// <param name="right">The right edge of the thing that is falling</param>
    /// <param name="oldBottom">Where its bottom edge was last frame</param>
    /// <param name="newBottom">Where its bottom edge is now</param>
    /// <param name="top">The top of the tile it landed on, if it landed</param>
    /// <returns>True if it landed on a tile this frame</returns>
    public bool TryLand(float left, float right, float oldBottom, float newBottom, out float top)
    {
        top = 0;

        // moving up never lands on anything
        if (newBottom < oldBottom) return false;

        // the rows whose top edge it passed between last frame and this one
        int firstRow = (int)MathF.Ceiling((oldBottom - LANDING_TOLERANCE) / TileHeight);
        int lastRow = (int)MathF.Floor(newBottom / TileHeight);

        // the columns it covers. Anything hanging past the end of the map counts as over the
        // end tile, so a rock that drifts off the side still hits the road
        int firstColumn = Math.Clamp((int)MathF.Floor(left / TileWidth), 0, _mapWidth - 1);
        int lastColumn = Math.Clamp((int)MathF.Floor(right / TileWidth), 0, _mapWidth - 1);

        // check from the top down, so the first tile found is the highest one it reached
        for (int row = Math.Max(firstRow, 0); row <= lastRow && row < _mapHeight; row++)
        {
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                if (IsSolid(column, row))
                {
                    top = row * TileHeight;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Finds every spot someone could stand: the middle of the top edge of each solid tile that
    /// has nothing on top of it
    /// </summary>
    /// <returns>Each spot, in world coordinates</returns>
    public List<Vector2> FindStandingSpots()
    {
        var spots = new List<Vector2>();
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                if (IsSolid(x, y) && !IsSolid(x, y - 1))
                    spots.Add(new Vector2(x * TileWidth + TileWidth / 2, y * TileHeight));
            }
        }
        return spots;
    }

    /// <summary>
    /// Draws every tile in the map. The SpriteBatch should already be using the camera's
    /// transform, which does the scrolling
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                int index = _map[y * _mapWidth + x] - 1;
                if (index == -1) continue;
                spriteBatch.Draw(
                    _tilesetTexture,
                    new Vector2(
                        x * TileWidth,
                        y * TileHeight
                    ),
                    _tiles[index],
                    Color.White,
                    0f,
                    Vector2.Zero,
                    SCALE,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
