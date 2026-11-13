using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// The rolling rock animation, used by BoulderSprite and EruptionSprite
/// </summary>
public class RockAnimation
{
    /// <summary>
    /// The size of one frame in the sprite sheet
    /// </summary>
    public const int FRAME_SIZE = 32;

    /// <summary>
    /// How many frames are in each row of the sheet
    /// </summary>
    private const int FRAME_COLUMNS = 4;

    /// <summary>
    /// How many frames the animation has
    /// </summary>
    private const int FRAME_COUNT = 12;

    /// <summary>
    /// How long each frame lasts, in seconds
    /// </summary>
    private readonly double frameTime;

    private Texture2D texture;

    private double animationTimer;

    private int animationFrame;

    /// <summary>
    /// Constructs the animation
    /// </summary>
    /// <param name="frameTime">How long each frame lasts, in seconds</param>
    public RockAnimation(double frameTime)
    {
        this.frameTime = frameTime;
    }

    /// <summary>
    /// Loads the rock sprite sheet
    /// </summary>
    /// <param name="content">The ContentManager</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("rock_round");
    }

    /// <summary>
    /// Moves to the next frame when it is time
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (animationTimer > frameTime)
        {
            animationFrame++;
            if (animationFrame >= FRAME_COUNT) animationFrame = 0;
            animationTimer -= frameTime;
        }
    }

    /// <summary>
    /// Draws the current frame, centered on a point
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    /// <param name="position">Where the middle of the rock goes</param>
    /// <param name="scale">How much to scale the 32x32 artwork</param>
    public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale)
    {
        var source = new Rectangle((animationFrame % FRAME_COLUMNS) * FRAME_SIZE,
                                   (animationFrame / FRAME_COLUMNS) * FRAME_SIZE,
                                   FRAME_SIZE, FRAME_SIZE);

        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_SIZE / 2f, FRAME_SIZE / 2f), scale, SpriteEffects.None, 0f);
    }
}
