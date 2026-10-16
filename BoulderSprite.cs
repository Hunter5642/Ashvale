using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// A rock falling out of the sky for the knight to dodge. It speeds up as it falls and smashes
/// when it reaches the ground, and the game then drops it again somewhere over the part of
/// the world the camera is looking at, so there is always something in the air. The rocks thrown from the volcano at the start of a run are EruptionSprites.
/// </summary>
public class BoulderSprite
{
    /// <summary>
    /// The size of one frame in the sprite sheet
    /// </summary>
    private const int FRAME_SIZE = 32;

    /// <summary>
    /// How many frames are in each row of the sheet
    /// </summary>
    private const int FRAME_COLUMNS = 4;

    /// <summary>
    /// How many frames the rolling animation has in total
    /// </summary>
    private const int FRAME_COUNT = 12;

    /// <summary>
    /// How long each animation frame lasts, in seconds
    /// </summary>
    private const double ANIMATION_SPEED = 0.06;

    /// <summary>
    /// How much to blow the 32x32 artwork up by
    /// </summary>
    private const float SCALE = 2;

    /// <summary>
    /// How big the rock is for collisions. A little smaller than the artwork so near
    /// misses don't count as hits
    /// </summary>
    private const float RADIUS = FRAME_SIZE / 2f * SCALE * 0.8f;

    /// <summary>
    /// How fast gravity pulls rocks down. Gentler than the knight's, so they arc slowly
    /// </summary>
    private const float GRAVITY = 250;

    /// <summary>
    /// How fast a rock is already falling when it comes in from the top
    /// </summary>
    private const float FALL_SPEED = 100;

    /// <summary>
    /// The game's random number generator, for drop points and drift
    /// </summary>
    private readonly MathHelper.Random random;

    private Texture2D texture;

    private double animationTimer;

    private int animationFrame;

    private Vector2 position;

    private Vector2 velocity;

    /// <summary>
    /// Whether this rock is in play. A rock does nothing until it is launched, so the game
    /// can throw them one at a time instead of all at once
    /// </summary>
    public bool Active { get; private set; }

    /// <summary>
    /// True from the moment the rock hits the ground until it is dropped again, so the game
    /// knows to smash it and send it back up
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
    /// Loads the rock sprite sheet using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("rock_round");
    }

    /// <summary>
    /// Drops this rock in from above the top of the screen, somewhere between two points in
    /// the world. The game passes in the edges of the camera's view, so rocks always fall
    /// where the knight can see them
    /// </summary>
    /// <param name="left">The leftmost world X the rock can fall at</param>
    /// <param name="right">The rightmost world X the rock can fall at</param>
    public void FallFromSky(float left, float right)
    {
        position = new Vector2(random.NextFloat(left + RADIUS, right - RADIUS), -RADIUS);

        // again, NextFloat won't go below 0, so the drift is shifted into range afterwards
        velocity = new Vector2(random.NextFloat(0, 80) - 40, FALL_SPEED);
        Active = true;
        Landed = false;
    }

    /// <summary>
    /// Takes this rock back out of play
    /// </summary>
    public void Reset()
    {
        Active = false;
        Landed = false;
    }

    /// <summary>
    /// Moves the rock and spins it, until it hits the ground
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        if (!Active) return;

        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // gravity bends the rock's path downward a little more every frame
        velocity.Y += GRAVITY * t;
        position += velocity * t;

        // update the animation frame
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (animationTimer > ANIMATION_SPEED)
        {
            animationFrame++;
            if (animationFrame >= FRAME_COUNT) animationFrame = 0;
            animationTimer -= ANIMATION_SPEED;
        }

        // it smashes once its middle is just into the road, which looks like it struck
        if (position.Y + RADIUS * 0.5f >= AshvaleGame.GROUND_Y)
        {
            Active = false;
            Landed = true;
        }
    }

    /// <summary>
    /// Draws the current animation frame, centered on the rock's position
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!Active) return;

        // the sheet is a grid, so a frame needs both a column and a row
        var source = new Rectangle((animationFrame % FRAME_COLUMNS) * FRAME_SIZE,
                                   (animationFrame / FRAME_COLUMNS) * FRAME_SIZE,
                                   FRAME_SIZE, FRAME_SIZE);

        // the origin is the middle of the frame, so the artwork lines up with the bounding circle
        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_SIZE / 2f, FRAME_SIZE / 2f), SCALE, SpriteEffects.None, 0f);
    }
}
