using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// The castle and volcano painting, laid end to end so it stretches past a single screen.
/// Every other copy is mirrored, so the edges always meet the matching edge and there is
/// no seam: castle, volcano, then the volcano again facing back, then the castle. It scrolls
/// at half the world's speed, which makes it read as far away behind the knight.
/// </summary>
public class ParallaxBackground
{
    /// <summary>
    /// How fast the background scrolls compared to the world
    /// </summary>
    public const float PARALLAX = 0.5f;

    /// <summary>
    /// The mouth of the volcano within one copy of the painting, at the size it is drawn
    /// </summary>
    private static readonly Vector2 MOUTH_IN_TILE = new Vector2(910, 90);

    /// <summary>
    /// How many copies it takes to cover everything the camera can see. The background only
    /// moves half as far as the world, so it needs about half as many screens
    /// </summary>
    private static readonly int tileCount =
        (int)MathF.Ceiling(((AshvaleGame.WORLD_WIDTH - AshvaleGame.WINDOW_WIDTH) * PARALLAX
                            + AshvaleGame.WINDOW_WIDTH) / AshvaleGame.WINDOW_WIDTH);

    /// <summary>
    /// Where the mouth of every volcano the camera can see is, in background coordinates.
    /// The mirrored copies have theirs on the opposite side
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
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("background");
    }

    /// <summary>
    /// Draws every copy of the painting. The SpriteBatch should already be using the camera's
    /// background transform, which does the scrolling
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        // one extra copy at the end, so a screen shake at the far edge never shows black
        for (int i = 0; i <= tileCount; i++)
        {
            var destination = new Rectangle(i * AshvaleGame.WINDOW_WIDTH, 0,
                                            AshvaleGame.WINDOW_WIDTH, AshvaleGame.WINDOW_HEIGHT);
            var effects = i % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(texture, destination, null, Color.White, 0f, Vector2.Zero, effects, 0f);
        }
    }
}
