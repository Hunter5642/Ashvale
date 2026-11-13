using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// A villager waiting to be rescued
/// </summary>
public class VillagerSprite
{
    /// <summary>
    /// The size of one frame in the sprite sheet
    /// </summary>
    private const int FRAME_WIDTH = 16;
    private const int FRAME_HEIGHT = 24;

    /// <summary>
    /// How many frames the waving animation has
    /// </summary>
    private const int FRAME_COUNT = 2;

    /// <summary>
    /// How long each animation frame lasts, in seconds
    /// </summary>
    private const double ANIMATION_SPEED = 0.4;

    /// <summary>
    /// How much the artwork is scaled up, the same as the tiles
    /// </summary>
    private const float SCALE = Tilemap.SCALE;

    /// <summary>
    /// The collision box, in pixels of the artwork
    /// </summary>
    private const float BOUNDS_WIDTH = 8;
    private const float BOUNDS_HEIGHT = 22;

    /// <summary>
    /// Where the first villager of a level stands
    /// </summary>
    private const float FIRST_VILLAGER_X = 1700;

    /// <summary>
    /// The least distance from the knight
    /// </summary>
    private const float MIN_KNIGHT_DISTANCE = 350;

    /// <summary>
    /// The most distance from the knight
    /// </summary>
    private const float MAX_KNIGHT_DISTANCE = 1600;

    /// <summary>
    /// How many spots to try before settling for the last one
    /// </summary>
    private const int MAX_ATTEMPTS = 50;

    /// <summary>
    /// The game's random number generator
    /// </summary>
    private readonly MathHelper.Random random;

    private Texture2D texture;

    private double animationTimer;

    private int animationFrame;

    private Vector2 position;

    /// <summary>
    /// Where the villager's feet are
    /// </summary>
    public Vector2 Position => position;

    /// <summary>
    /// The middle of the villager
    /// </summary>
    public Vector2 Center => position - new Vector2(0, FRAME_HEIGHT * SCALE / 2);

    /// <summary>
    /// The bounding volume of the villager
    /// </summary>
    public BoundingRectangle Bounds => new BoundingRectangle(
        position.X - BOUNDS_WIDTH * SCALE / 2,
        position.Y - BOUNDS_HEIGHT * SCALE,
        BOUNDS_WIDTH * SCALE,
        BOUNDS_HEIGHT * SCALE);

    /// <summary>
    /// Whether the villager has been rescued
    /// </summary>
    public bool Rescued { get; set; }

    /// <summary>
    /// Constructs a new villager
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public VillagerSprite(MathHelper.Random random)
    {
        this.random = random;

        MoveToFirstSpot();
    }

    /// <summary>
    /// Loads the villager sprite sheet
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("villager");
    }

    /// <summary>
    /// Puts the villager on the road, where every level starts
    /// </summary>
    public void MoveToFirstSpot()
    {
        position = new Vector2(FIRST_VILLAGER_X, AshvaleGame.GROUND_Y);
        Rescued = false;
    }

    /// <summary>
    /// Moves the villager to a random ledge, away from the knight
    /// </summary>
    /// <param name="knight">The knight's bounding box</param>
    /// <param name="tilemap">The level</param>
    public void MoveToRandomSpot(BoundingRectangle knight, Tilemap tilemap)
    {
        var ledges = tilemap.FindLedgeSpots();
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            position = ledges[random.Next(ledges.Count)];

            float distance = SidewaysDistanceFrom(knight);
            if (distance >= MIN_KNIGHT_DISTANCE && distance <= MAX_KNIGHT_DISTANCE) break;
        }

        Rescued = false;
    }

    /// <summary>
    /// How far the villager is from the middle of a box, left or right
    /// </summary>
    /// <param name="box">The box to measure to</param>
    /// <returns>The distance in pixels</returns>
    private float SidewaysDistanceFrom(BoundingRectangle box)
    {
        return System.Math.Abs(position.X - box.Center.X);
    }

    /// <summary>
    /// Animates the villager
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
    /// Draws the villager
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Rescued) return;

        var source = new Rectangle(animationFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);

        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_WIDTH / 2f, FRAME_HEIGHT), SCALE, SpriteEffects.None, 0f);
    }
}
