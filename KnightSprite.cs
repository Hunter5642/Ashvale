using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// A class representing the player's knight. The knight artwork ships
/// as six loose body parts, so this class holds an anchor position and draws every
/// part at a fixed offset from it. Moving the anchor moves the whole knight.
/// </summary>
public class KnightSprite
{
    /// <summary>
    /// How much to shrink the artwork by on the title screen
    /// </summary>
    private const float TITLE_SCALE = 0.75f;

    /// <summary>
    /// How fast the knight drifts, in pixels per second
    /// </summary>
    private const float BOB_SPEED = 20;

    /// <summary>
    /// How many seconds before the knight reverses direction
    /// </summary>
    private const double BOB_INTERVAL = 1.2;

    /// <summary>
    /// How fast gravity speeds the knight up as he falls, in pixels per second per second
    /// </summary>
    private const float GRAVITY = 2000;

    /// <summary>
    /// How fast the knight leaves the ground when he jumps, in pixels per second
    /// </summary>
    private const float JUMP_SPEED = 800;

    /// <summary>
    /// How far below Position the knight's feet are drawn, before scaling. His right leg is
    /// drawn 55 pixels below Position and is 200 pixels tall
    /// </summary>
    private const float FOOT_OFFSET = 55 + 200;

    /// <summary>
    /// The box used for collisions, before scaling, measured from Position. It covers his head,
    /// body and legs but not his outstretched arms, so a near miss isn't a hit. Shrink the
    /// width or height to make the game more forgiving
    /// </summary>
    private const float BOUNDS_X = -52;
    private const float BOUNDS_Y = -118;
    private const float BOUNDS_WIDTH = 161;
    private const float BOUNDS_HEIGHT = 373;

    private Texture2D body;
    private Texture2D head;
    private Texture2D leftArm;
    private Texture2D rightArm;
    private Texture2D leftLeg;
    private Texture2D rightLeg;

    private double bobTimer;

    private bool movingDown = false;

    /// <summary>How fast the knight is moving, in pixels per second.  Only Y is used so far</summary>
    private Vector2 velocity;

    /// <summary>True while the knight is standing on the ground, so he can't jump in midair</summary>
    private bool onGround;

    /// <summary>
    /// The point the knight is built around
    /// Every body part is positioned relative to this
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// How much to shrink the artwork by. Half the title screen size fits the play area
    /// </summary>
    public float Scale { get; set; } = TITLE_SCALE;

    /// <summary>
    /// Which way the knight is looking. The artwork faces left, so drawing him facing right
    /// mirrors both the pictures and where each body part sits
    /// </summary>
    public bool FacingRight { get; set; }

    /// <summary>
    /// Whether the knight is drifting up and down on the title screen. Set this false
    /// when the game starts so gravity takes over
    /// </summary>
    public bool Bobbing { get; set; } = true;

    /// <summary>The bounding volume of the knight, which follows Position and the way he faces</summary>
    public BoundingRectangle Bounds => new BoundingRectangle(
        Position + new Vector2(FacingRight ? -(BOUNDS_X + BOUNDS_WIDTH) : BOUNDS_X, BOUNDS_Y) * Scale,
        BOUNDS_WIDTH * Scale,
        BOUNDS_HEIGHT * Scale);

    /// <summary>
    /// The Y value Position sits at when the knight is standing on the ground. Position is
    /// his anchor point, not his feet, so the ground line is his feet higher up
    /// </summary>
    private float GroundLine => AshvaleGame.GROUND_Y - FOOT_OFFSET * Scale;

    /// <summary>
    /// Loads the six body part textures using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager</param>
    public void LoadContent(ContentManager content)
    {
        body = content.Load<Texture2D>("knight_body");
        head = content.Load<Texture2D>("knight_head");
        leftArm = content.Load<Texture2D>("knight_leftarm");
        rightArm = content.Load<Texture2D>("knight_rightarm");
        leftLeg = content.Load<Texture2D>("knight_leftleg");
        rightLeg = content.Load<Texture2D>("knight_rightleg");
    }

    /// <summary>
    /// Stands the knight on the ground at the given X, still and ready to play
    /// </summary>
    /// <param name="x">Where along the ground to put him</param>
    public void PlaceOnGround(float x)
    {
        Position = new Vector2(x, GroundLine);
        velocity = Vector2.Zero;
        onGround = true;
    }

    /// <summary>
    /// Starts a jump, but only if the knight is standing on the ground
    /// </summary>
    public void Jump()
    {
        if (!onGround) return;

        // Y grows downward, so an upward jump is a negative velocity
        velocity.Y = -JUMP_SPEED;
        onGround = false;
    }

    /// <summary>
    /// Bobs the knight gently up and down on the title screen, or falls and jumps in game
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (Bobbing)
        {
            // update the bob timer
            bobTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // reverse direction every 1.2 seconds
            if (bobTimer > BOB_INTERVAL)
            {
                movingDown = !movingDown;
                bobTimer -= BOB_INTERVAL;
            }

            // move the knight in the direction he is currently drifting
            if (movingDown)
                Position += new Vector2(0, 1) * BOB_SPEED * t;
            else
                Position += new Vector2(0, -1) * BOB_SPEED * t;

            return;
        }

        // gravity speeds up the fall a little more every frame, and the velocity moves the knight
        velocity.Y += GRAVITY * t;
        Position += velocity * t;

        // landing: never let the knight sink below the ground
        if (Position.Y >= GroundLine)
        {
            Position.Y = GroundLine;
            velocity.Y = 0;
            onGround = true;
        }

        // keep his collision box on screen, whichever way he is facing
        float boxOffset = (FacingRight ? -(BOUNDS_X + BOUNDS_WIDTH) : BOUNDS_X) * Scale;
        Position.X = MathHelper.Clamp(Position.X,
                                      -boxOffset,
                                      AshvaleGame.WINDOW_WIDTH - boxOffset - BOUNDS_WIDTH * Scale);
    }

    /// <summary>
    /// Draws the assembled knight
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Offsets are measured from Position to each part's top-left corner
        DrawPart(spriteBatch, leftArm, new Vector2(-215, -140));
        DrawPart(spriteBatch, leftLeg, new Vector2(-58, 60));
        DrawPart(spriteBatch, rightLeg, new Vector2(4, 55));
        DrawPart(spriteBatch, body, new Vector2(-52, -46));
        DrawPart(spriteBatch, head, new Vector2(-40, -118));
        DrawPart(spriteBatch, rightArm, new Vector2(18, -40));
    }

    /// <summary>
    /// Draws a single body part at a scaled offset from the knight's position, mirrored
    /// when the knight is facing right
    /// </summary>
    private void DrawPart(SpriteBatch spriteBatch, Texture2D texture, Vector2 offset)
    {
        SpriteEffects effects = SpriteEffects.None;

        if (FacingRight)
        {
            // A part covers offset.X to offset.X + Width, so mirroring it about Position puts
            // it at -(offset.X + Width).  Flipping the artwork without this would scatter the
            // parts, because each picture would mirror where it stands instead of swapping sides
            offset.X = -(offset.X + texture.Width);
            effects = SpriteEffects.FlipHorizontally;
        }

        spriteBatch.Draw(texture, Position + offset * Scale, null, Color.White,
                         0f, new Vector2(0,0), Scale, effects, 0f);
    }
}
