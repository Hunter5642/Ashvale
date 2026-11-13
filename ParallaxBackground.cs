using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// The castle and volcano painting, repeated and mirrored, scrolling at half speed
/// </summary>
public class ParallaxBackground
{
    /// <summary>
    /// How fast the background scrolls compared to the world
    /// </summary>
    public const float PARALLAX = 0.5f;

    /// <summary>
    /// The volcano's mouth in one copy of the painting
    /// </summary>
    private static readonly Vector2 MOUTH_IN_TILE = new Vector2(910, 90);

    /// <summary>
    /// How many copies of the painting are needed
    /// </summary>
    private static readonly int tileCount =
        (int)MathF.Ceiling(((AshvaleGame.WORLD_WIDTH - AshvaleGame.WINDOW_WIDTH) * PARALLAX
                            + AshvaleGame.WINDOW_WIDTH) / AshvaleGame.WINDOW_WIDTH);

    /// <summary>
    /// Where each volcano's mouth is, in background coordinates
    /// </summary>
    public static readonly Vector2[] VolcanoMouths = FindMouths();

    private Texture2D texture;

    private static Vector2[] FindMouths()
    {
        var mouths = new Vector2[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            float x = i % 2 == 0 ? MOUTH_IN_TILE.X : AshvaleGame.WINDOW_WIDTH - MOUTH_IN_TILE.X;
            mouths[i] = new Vector2(i * AshvaleGame.WINDOW_WIDTH + x, MOUTH_IN_TILE.Y);
        }
        return mouths;
    }

    /// <summary>
    /// Loads the background painting
    /// </summary>
    /// <param name="content">The ContentManager</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("background");
    }

    /// <summary>
    /// Draws every copy of the painting
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        // one extra copy for screen shake
        for (int i = 0; i <= tileCount; i++)
        {
            var destination = new Rectangle(i * AshvaleGame.WINDOW_WIDTH, 0,
                                            AshvaleGame.WINDOW_WIDTH, AshvaleGame.WINDOW_HEIGHT);
            var effects = i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(texture, destination, null, Color.White, 0f, Vector2.Zero, effects, 0f);
        }
    }
}
