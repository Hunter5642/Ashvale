using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// A rock thrown out of the volcano at the start of a run, for show
/// </summary>
public class EruptionSprite
{
    /// <summary>
    /// How long each animation frame lasts, in seconds
    /// </summary>
    private const double ANIMATION_SPEED = 0.05;

    /// <summary>
    /// How much the 32x32 artwork is scaled up
    /// </summary>
    private const float SCALE = 1.5f;

    /// <summary>
    /// Half the drawn size
    /// </summary>
    private const float HALF_SIZE = RockAnimation.FRAME_SIZE / 2f * SCALE;


    /// <summary>
    /// The game's random number generator
    /// </summary>
    private readonly MathHelper.Random random;

    private readonly RockAnimation animation = new RockAnimation(ANIMATION_SPEED);

    private Vector2 position;

    private Vector2 velocity;

    /// <summary>
    /// Whether the rock is still on screen
    /// </summary>
    public bool Active { get; private set; }

    /// <summary>
    /// Constructs a new eruption rock
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public EruptionSprite(MathHelper.Random random)
    {
        this.random = random;
    }

    /// <summary>
    /// Loads the rock sprite sheet
    /// </summary>
    /// <param name="content">The ContentManager</param>
    public void LoadContent(ContentManager content)
    {
        animation.LoadContent(content);
    }

    /// <summary>
    /// Throws this rock up out of the volcano
    /// </summary>
    public void Erupt()
    {
        // the volcano on screen at the start
        position = ParallaxBackground.VolcanoMouths[0];
        velocity = new Vector2(0, -random.NextFloat(140, 260));
        Active = true;
    }

    /// <summary>
    /// Takes this rock out of play
    /// </summary>
    public void Reset()
    {
        Active = false;
    }

    /// <summary>
    /// Flies the rock in a straight line until it leaves the screen
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        if (!Active) return;

        position += velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

        animation.Update(gameTime);

        // done once off the screen
        if (position.Y + HALF_SIZE < 0
            || position.X + HALF_SIZE < 0
            || position.X - HALF_SIZE > AshvaleGame.WINDOW_WIDTH
            || position.Y - HALF_SIZE > AshvaleGame.WINDOW_HEIGHT)
        {
            Active = false;
        }
    }

    /// <summary>
    /// Draws the rock
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!Active) return;

        animation.Draw(spriteBatch, position, SCALE);
    }
}
