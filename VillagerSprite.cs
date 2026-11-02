using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// A villager stranded outside the castle, waving for help. There is only ever one out at a
/// time: reaching them sends them running for the castle, and the next one turns up somewhere
/// else, up on a ledge.
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
    /// How much to blow the artwork up by. The same as the tiles, so their pixels match
    /// </summary>
    private const float SCALE = Tilemap.SCALE;

    /// <summary>
    /// The box used for collisions, in pixels of the artwork: their body from the top of their
    /// head down to their boots, but not their waving arms
    /// </summary>
    private const float BOUNDS_WIDTH = 8;
    private const float BOUNDS_HEIGHT = 22;

    /// <summary>
    /// Where the first villager of a level stands. On the road, so the opening rescue needs no
    /// climbing, and far enough in that the camera has to scroll to reach them
    /// </summary>
    private const float FIRST_VILLAGER_X = 1700;

    /// <summary>
    /// How far across the screen the villager has to be from the middle of the knight. Only
    /// the sideways distance counts, so he always has to run a good way for the next one
    /// </summary>
    private const float MIN_KNIGHT_DISTANCE = 350;

    /// <summary>
    /// The farthest across the world the villager can be from the knight, so the next one is
    /// never a slog from one end of the world to the other
    /// </summary>
    private const float MAX_KNIGHT_DISTANCE = 1600;

    /// <summary>
    /// How many spots to try before settling for the last one
    /// </summary>
    private const int MAX_ATTEMPTS = 50;

    /// <summary>
    /// The game's random number generator, for picking the next spot
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
    /// Whether the villager has been rescued. A rescued villager isn't drawn, and moving them to
    /// a new spot brings the next one out
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
    /// Loads the villager sprite sheet using the ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("villager");
    }

    /// <summary>
    /// Puts the villager on the road past the right-hand edge of the first screen. Every level
    /// starts its first villager here, so the opening move is always a run to the right
    /// </summary>
    public void MoveToFirstSpot()
    {
        position = new Vector2(FIRST_VILLAGER_X, AshvaleGame.GROUND_Y);
        Rescued = false;
    }

    /// <summary>
    /// Moves the villager up onto a random ledge a good distance away from the knight so he always has to travel
    /// and climb for them
    /// </summary>
    /// <param name="knight">The knight's bounding box, the spot has to stay clear of</param>
    /// <param name="tilemap">The level, whose ledges the villager can stand on</param>
    public void MoveToRandomSpot(BoundingRectangle knight, Tilemap tilemap)
    {
        // every spot on top of a ledge, leaving out the road
        var ledges = new List<Vector2>();
        foreach (var spot in tilemap.FindStandingSpots())
            if (spot.Y < AshvaleGame.GROUND_Y) ledges.Add(spot);

        // a level with no ledges at all puts them on the road instead
        if (ledges.Count == 0) ledges = tilemap.FindStandingSpots();

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            position = ledges[random.Next(ledges.Count)];

            float distance = SidewaysDistanceFrom(knight);
            if (distance >= MIN_KNIGHT_DISTANCE && distance <= MAX_KNIGHT_DISTANCE) break;
        }

        Rescued = false;
    }

    /// <summary>
    /// How far the villager stands to the left or right of the middle of a bounding box,
    /// ignoring how high or low they are
    /// </summary>
    /// <param name="box">The box to measure to</param>
    /// <returns>The distance across the screen in pixels</returns>
    private float SidewaysDistanceFrom(BoundingRectangle box)
    {
        float boxMiddle = box.X + box.Width / 2;
        return System.Math.Abs(position.X - boxMiddle);
    }

    /// <summary>
    /// Waves the villager's arms
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
    /// Draws the current animation frame, standing on the villager's position
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Rescued) return;

        var source = new Rectangle(animationFrame * FRAME_WIDTH, 0, FRAME_WIDTH, FRAME_HEIGHT);

        // the origin is the bottom middle of the frame, so position is where their feet go
        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_WIDTH / 2f, FRAME_HEIGHT), SCALE, SpriteEffects.None, 0f);
    }
}
