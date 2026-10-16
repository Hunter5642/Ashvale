using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// The scorched road the knight runs along, the full width of the world, with rocks from
/// old eruptions scattered along it. It is drawn in the world layer, so it scrolls at full
/// speed in front of the slower background and gives the eye something to track.
/// </summary>
public class Ground
{
    /// <summary>
    /// The road's colors: dark basalt, with a glowing seam of lava along the top edge
    /// </summary>
    private static readonly Color ROCK = new Color(38, 26, 28);
    private static readonly Color LAVA_SEAM = new Color(255, 90, 20);

    /// <summary>
    /// The rock sprite sheet is a 4x3 grid of 32 pixel frames
    /// </summary>
    private const int FRAME_SIZE = 32;
    private const int FRAME_COLUMNS = 4;
    private const int FRAME_COUNT = 12;

    /// <summary>
    /// One rock lying by the road. Each is turned, sized and shaded on its own
    /// </summary>
    private struct Rubble
    {
        public Vector2 Position;
        public Rectangle Source;
        public float Rotation;
        public float Scale;
        public Color Tint;
    }

    private readonly Rubble[] rubble;

    private Texture2D rockTexture;

    /// <summary>
    /// Scatters the rubble along the whole road. It uses its own fixed seed so the road
    /// looks the same every time the game is played
    /// </summary>
    public Ground()
    {
        var random = new System.Random(580);
        var pieces = new System.Collections.Generic.List<Rubble>();

        for (float x = 40; x < AshvaleGame.WORLD_WIDTH; x += 90 + (float)random.NextDouble() * 220)
        {
            int frame = random.Next(FRAME_COUNT);
            float shade = 0.3f + (float)random.NextDouble() * 0.3f;
            pieces.Add(new Rubble
            {
                // half sunk into the road
                Position = new Vector2(x, AshvaleGame.GROUND_Y + 10),
                Source = new Rectangle((frame % FRAME_COLUMNS) * FRAME_SIZE, (frame / FRAME_COLUMNS) * FRAME_SIZE,
                                       FRAME_SIZE, FRAME_SIZE),
                Rotation = (float)random.NextDouble() * MathHelper.TwoPi,
                Scale = 0.5f + (float)random.NextDouble() * 0.9f,
                // darkened, with a touch more red than the original grey
                Tint = new Color(shade * 1.2f, shade, shade)
            });
        }

        rubble = pieces.ToArray();
    }

    /// <summary>
    /// Loads the rock sprite sheet the rubble is cut from
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        rockTexture = content.Load<Texture2D>("rock_round");
    }

    /// <summary>
    /// Draws the road and its rubble, in world coordinates
    /// </summary>
    /// <param name="gameTime">The game time, which makes the lava seam pulse</param>
    /// <param name="spriteBatch">The SpriteBatch, already using the camera's transform</param>
    /// <param name="pixel">A single white pixel to stretch into the road</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Texture2D pixel)
    {
        // the road runs a little past both ends of the world and well below the screen, so a
        // screen shake never shows its edges
        int depth = AshvaleGame.WINDOW_HEIGHT - AshvaleGame.GROUND_Y + 100;
        spriteBatch.Draw(pixel, new Rectangle(-100, AshvaleGame.GROUND_Y, AshvaleGame.WORLD_WIDTH + 200, depth), ROCK);

        float glow = 0.6f + 0.25f * (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2);
        spriteBatch.Draw(pixel, new Rectangle(-100, AshvaleGame.GROUND_Y, AshvaleGame.WORLD_WIDTH + 200, 3), LAVA_SEAM * glow);

        var origin = new Vector2(FRAME_SIZE / 2f, FRAME_SIZE / 2f);
        foreach (var piece in rubble)
        {
            spriteBatch.Draw(rockTexture, piece.Position, piece.Source, piece.Tint,
                             piece.Rotation, origin, piece.Scale, SpriteEffects.None, 0f);
        }
    }
}
