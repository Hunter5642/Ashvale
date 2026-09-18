using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// The spinning coin the knight is chasing. There is only ever one on screen: collecting it
/// moves it somewhere else rather than removing it.
/// </summary>
public class CoinSprite
{
    /// <summary>
    /// The size of one frame in the sprite sheet
    /// </summary>
    private const int FRAME_SIZE = 16;

    /// <summary>
    /// How many frames the spin animation has
    /// </summary>
    private const int FRAME_COUNT = 8;

    /// <summary>
    /// How long each animation frame lasts, in seconds
    /// </summary>
    private const double ANIMATION_SPEED = 0.1;

    /// <summary>
    /// How much to blow the 16x16 artwork up by
    /// </summary>
    private const float SCALE = 2.5f;

    /// <summary>
    /// How big the coin is for collisions
    /// </summary>
    private const float RADIUS = FRAME_SIZE / 2f * SCALE;

    /// <summary>
    /// How far from the sides of the screen the coin can appear
    /// </summary>
    private const float SIDE_MARGIN = 120;

    /// <summary>
    /// The lowest and highest the coin can appear, measured up from the ground. Standing, the
    /// knight reaches 140 pixels up and jumping he reaches 293, so anything past about 160 has
    /// to be jumped for. The high end stops short of his jump so a coin is never out of reach
    /// </summary>
    private const float MIN_HEIGHT = 175;
    private const float MAX_HEIGHT = 275;

    /// <summary>
    /// How high the first coin of a run sits. Low enough to walk into, so the opening grab
    /// needs no jump
    /// </summary>
    private const float FIRST_COIN_HEIGHT = 40;

    /// <summary>
    /// How far the coin has to land from the knight's body, so it never turns up on top of him
    /// the instant he grabs the last one
    /// </summary>
    private const float MIN_KNIGHT_DISTANCE = 100;

    /// <summary>
    /// How many spots to try before settling for the last one. The knight only rules out a
    /// couple hundred pixels of a 1200 pixel screen, so a few rolls always find room
    /// </summary>
    private const int MAX_ATTEMPTS = 50;

    /// <summary>The game's random number generator, for picking the next spot</summary>
    private readonly MathHelper.Random random;

    private Texture2D texture;

    private double animationTimer;

    private int animationFrame;

    private Vector2 position;

    /// <summary>The bounding volume of the coin</summary>
    public BoundingCircle Bounds => new BoundingCircle(position, RADIUS);

    /// <summary>
    /// Whether the coin has been picked up.  A collected coin isn't drawn, and moving it to a
    /// new spot brings it back
    /// </summary>
    public bool Collected { get; set; }

    /// <summary>
    /// Constructs a new coin
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public CoinSprite(MathHelper.Random random)
    {
        this.random = random;

        // the game moves it again when play starts; this just keeps it somewhere sane until then
        MoveToRightEdge();
    }

    /// <summary>
    /// Loads the coin sprite sheet using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("coins");
    }

    /// <summary>
    /// Puts the coin at the right-hand end of the screen, just off the ground.  Every run
    /// starts its first coin here, so the opening move is always a run to the right
    /// </summary>
    public void MoveToRightEdge()
    {
        position = new Vector2(AshvaleGame.WINDOW_WIDTH - SIDE_MARGIN, AshvaleGame.GROUND_Y - FIRST_COIN_HEIGHT);
        Collected = false;
    }

    /// <summary>
    /// Moves the coin to a new random spot within the knight's reach, but at least
    /// MIN_KNIGHT_DISTANCE pixels away from him, so he always has to travel for it
    /// </summary>
    /// <param name="knight">The knight's bounding box, the spot has to stay clear of</param>
    public void MoveToRandomSpot(BoundingRectangle knight)
    {
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            position = new Vector2(
                random.NextFloat(SIDE_MARGIN, AshvaleGame.WINDOW_WIDTH - SIDE_MARGIN),
                AshvaleGame.GROUND_Y - random.NextFloat(MIN_HEIGHT, MAX_HEIGHT));

            if (DistanceFrom(knight) >= MIN_KNIGHT_DISTANCE) break;
        }

        Collected = false;
    }

    /// <summary>
    /// How far the coin sits from the nearest point of a bounding box, using the same
    /// nearest-point trick CollisionHelper uses for a circle against a rectangle
    /// </summary>
    /// <param name="box">The box to measure to</param>
    /// <returns>The distance in pixels, or 0 if the coin is inside the box</returns>
    private float DistanceFrom(BoundingRectangle box)
    {
        float nearestX = MathHelper.Clamp(position.X, box.Left, box.Right);
        float nearestY = MathHelper.Clamp(position.Y, box.Top, box.Bottom);
        return Vector2.Distance(position, new Vector2(nearestX, nearestY));
    }

    /// <summary>
    /// Spins the coin
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;

        if (animationTimer > ANIMATION_SPEED)
        {
            animationFrame++;
            if (animationFrame >= FRAME_COUNT) animationFrame = 0;
            animationTimer -= ANIMATION_SPEED;
        }
    }

    /// <summary>
    /// Draws the current animation frame, centered on the coin's position
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Collected) return;

        var source = new Rectangle(animationFrame * FRAME_SIZE, 0, FRAME_SIZE, FRAME_SIZE);

        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_SIZE / 2f, FRAME_SIZE / 2f), SCALE, SpriteEffects.None, 0f);
    }
}
