using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// A rock falling from the sky for the knight to dodge
/// </summary>
public class BoulderSprite
{
    /// <summary>
    /// How long each animation frame lasts, in seconds
    /// </summary>
    private const double ANIMATION_SPEED = 0.06;

    /// <summary>
    /// How much the 32x32 artwork is scaled up
    /// </summary>
    private const float SCALE = 2;

    /// <summary>
    /// The collision radius, a little smaller than the artwork
    /// </summary>
    private const float RADIUS = RockAnimation.FRAME_SIZE / 2f * SCALE * 0.8f;

    /// <summary>
    /// How fast gravity pulls the rock down
    /// </summary>
    private const float GRAVITY = 250;

    /// <summary>
    /// How fast the rock is falling when it appears
    /// </summary>
    private const float FALL_SPEED = 100;

    /// <summary>
    /// The game's random number generator
    /// </summary>
    private readonly MathHelper.Random random;

    private readonly RockAnimation animation = new RockAnimation(ANIMATION_SPEED);

    private Vector2 position;

    private Vector2 velocity;

    /// <summary>
    /// Whether the rock is in play
    /// </summary>
    public bool Active { get; private set; }

    /// <summary>
    /// Whether the rock just hit a ledge or the road
    /// </summary>
    public bool Landed { get; private set; }

    /// <summary>
    /// Where the middle of the rock is
    /// </summary>
    public Vector2 Position => position;

    /// <summary>
    /// The bounding volume of the rock
    /// </summary>
    public BoundingCircle Bounds => new BoundingCircle(position, RADIUS);

    /// <summary>
    /// Constructs a new rock
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public BoulderSprite(MathHelper.Random random)
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
    /// Drops the rock in from above the screen, somewhere between two points
    /// </summary>
    /// <param name="left">The leftmost world X the rock can fall at</param>
    /// <param name="right">The rightmost world X the rock can fall at</param>
    public void FallFromSky(float left, float right)
    {
        position = new Vector2(random.NextFloat(left + RADIUS, right - RADIUS), -RADIUS);

        // drift left or right
        velocity = new Vector2(random.NextFloat(0, 80) - 40, FALL_SPEED);
        Active = true;
        Landed = false;
    }

    /// <summary>
    /// Takes the rock out of play
    /// </summary>
    public void Reset()
    {
        Active = false;
        Landed = false;
    }

    /// <summary>
    /// Moves the rock until it hits a ledge or the road
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="tilemap">The level</param>
    public void Update(GameTime gameTime, Tilemap tilemap)
    {
        if (!Active) return;

        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float oldBottom = position.Y + RADIUS * 0.5f;

        // gravity
        velocity.Y += GRAVITY * t;
        position += velocity * t;

        animation.Update(gameTime);

        // smash on a ledge or the road
        float newBottom = position.Y + RADIUS * 0.5f;
        if (tilemap.TryLand(position.X - RADIUS * 0.5f, position.X + RADIUS * 0.5f,
                            oldBottom, newBottom, out float top))
        {
            position.Y = top;
            Active = false;
            Landed = true;
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
