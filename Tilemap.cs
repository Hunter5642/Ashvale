using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// A level made of tiles, read from a text file
/// </summary>
public class Tilemap
{
    /// <summary>
    /// How much the 16 pixel tiles are scaled up
    /// </summary>
    public const float SCALE = 2.5f;

    /// <summary>
    /// How far below a tile's top something still lands on it
    /// </summary>
    public const float LANDING_TOLERANCE = 1;

    /// <summary>
    /// The tile size and the map size
    /// </summary>
    private int _tileWidth, _tileHeight, _mapWidth, _mapHeight;

    /// <summary>
    /// The tileset texture
    /// </summary>
    private Texture2D _tilesetTexture;

    /// <summary>
    /// The tiles in the tileset
    /// </summary>
    private Rectangle[] _tiles;

    /// <summary>
    /// The map's tiles
    /// </summary>
    private int[] _map;

    /// <summary>
    /// The map file's name
    /// </summary>
    private string _filename;

    /// <summary>
    /// A tile's width in the world
    /// </summary>
    public float TileWidth => _tileWidth * SCALE;

    /// <summary>
    /// A tile's height in the world
    /// </summary>
    public float TileHeight => _tileHeight * SCALE;

    /// <summary>
    /// Creates a tilemap from a map file
    /// </summary>
    /// <param name="filename">The map file, like "level1.txt"</param>
    public Tilemap(string filename)
    {
        _filename = filename;
    }

    /// <summary>
    /// Reads the map file and loads its tileset
    /// </summary>
    /// <param name="content">The ContentManager</param>
    public void LoadContent(ContentManager content)
    {
        // open the map file next to the game
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

        // tile bounds in the tileset
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

        // the map, one line per row
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
    /// Whether a tile is solid
    /// </summary>
    /// <param name="column">The column</param>
    /// <param name="row">The row</param>
    /// <returns>Whether it is solid</returns>
    public bool IsSolid(int column, int row)
    {
        if (column < 0 || column >= _mapWidth || row < 0 || row >= _mapHeight) return false;
        return _map[row * _mapWidth + column] != 0;
    }

    /// <summary>
    /// Checks whether something falling landed on a tile this frame
    /// </summary>
    /// <param name="left">Its left edge</param>
    /// <param name="right">Its right edge</param>
    /// <param name="oldBottom">Its bottom edge last frame</param>
    /// <param name="newBottom">Its bottom edge now</param>
    /// <param name="top">The top of the tile it landed on</param>
    /// <returns>Whether it landed</returns>
    public bool TryLand(float left, float right, float oldBottom, float newBottom, out float top)
    {
        top = 0;

        if (newBottom < oldBottom) return false;

        // rows it passed this frame
        int firstRow = (int)MathF.Ceiling((oldBottom - LANDING_TOLERANCE) / TileHeight);
        int lastRow = (int)MathF.Floor(newBottom / TileHeight);

        // columns it covers, kept inside the map
        int firstColumn = Math.Clamp((int)MathF.Floor(left / TileWidth), 0, _mapWidth - 1);
        int lastColumn = Math.Clamp((int)MathF.Floor(right / TileWidth), 0, _mapWidth - 1);

        // highest tile first
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
    /// Finds every spot on top of a solid tile
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
    /// Finds every spot on a ledge, or on the road if there are no ledges
    /// </summary>
    /// <returns>Each spot, in world coordinates</returns>
    public List<Vector2> FindLedgeSpots()
    {
        var spots = FindStandingSpots();
        var ledges = spots.FindAll(spot => spot.Y < AshvaleGame.GROUND_Y);
        return ledges.Count > 0 ? ledges : spots;
    }

    /// <summary>
    /// Draws the map
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
