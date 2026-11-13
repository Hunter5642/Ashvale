using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// The player's knight, drawn from six body parts around one position
/// </summary>
public class KnightSprite
{
    /// <summary>
    /// The knight's size on the title screen
    /// </summary>
    private const float TITLE_SCALE = 0.75f;

    /// <summary>
    /// How fast the knight bobs on the title screen
    /// </summary>
    private const float BOB_SPEED = 20;

    /// <summary>
    /// Seconds between changes of bobbing direction
    /// </summary>
    private const double BOB_INTERVAL = 1.2;

    /// <summary>
    /// How fast gravity pulls the knight down
    /// </summary>
    private const float GRAVITY = 2000;

    /// <summary>
    /// How fast the knight jumps. His feet rise 250 pixels
    /// </summary>
    private const float JUMP_SPEED = 1000;

    /// <summary>
    /// How far below Position his feet are, before scaling
    /// </summary>
    private const float FOOT_OFFSET = 55 + 200;

    /// <summary>
    /// The collision box, before scaling, measured from Position
    /// </summary>
    private const float BOUNDS_X = -52;
    private const float BOUNDS_Y = -118;
    private const float BOUNDS_WIDTH = 161;
    private const float BOUNDS_HEIGHT = 373;

    /// <summary>
    /// Where the shield arm is drawn from Position, before scaling
    /// </summary>
    private static readonly Vector2 SHIELD_ARM_OFFSET = new Vector2(18, -40);

    /// <summary>
    /// The glow color of the shield
    /// </summary>
    private static readonly Color GLOW_COLOR = new Color(60, 170, 255);

    /// <summary>
    /// The size and brightness of each layer of the glow's halo
    /// </summary>
    private static readonly float[] HALO_SIZES = { 1.1f, 1.22f, 1.36f, 1.5f };
    private static readonly float[] HALO_BRIGHTNESS = { 1f, 0.75f, 0.5f, 0.25f };

    /// <summary>
    /// Brightness of the shine over the shield
    /// </summary>
    private const float SHINE_BRIGHTNESS = 0.6f;

    /// <summary>
    /// How fast the glow pulses
    /// </summary>
    private const float PULSE_SPEED = 5;

    private Texture2D body;
    private Texture2D head;
    private Texture2D leftArm;
    private Texture2D rightArm;
    private Texture2D leftLeg;
    private Texture2D rightLeg;

    /// <summary>
    /// A white copy of the shield arm, used for the glow
    /// </summary>
    private Texture2D shieldOutline;

    private double bobTimer;

    private bool movingDown = false;

    /// <summary>
    /// The knight's velocity
    /// </summary>
    private Vector2 velocity;

    /// <summary>
    /// Whether the knight is on the ground
    /// </summary>
    private bool onGround;

    /// <summary>
    /// True on the frame the knight lands
    /// </summary>
    public bool JustLanded { get; private set; }

    /// <summary>
    /// The point the knight is drawn around
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The knight's size
    /// </summary>
    public float Scale { get; set; } = TITLE_SCALE;

    /// <summary>
    /// Which way the knight faces
    /// </summary>
    public bool FacingRight { get; set; }

    /// <summary>
    /// Whether the knight is bobbing on the title screen
    /// </summary>
    public bool Bobbing { get; set; } = true;

    /// <summary>
    /// Whether the shield is glowing, ready to block a rock
    /// </summary>
    public bool ShieldGlowing { get; set; }

    /// <summary>
    /// The middle of the knight's shield
    /// </summary>
    public Vector2 ShieldCenter =>
        Position + (PartOffset(rightArm, SHIELD_ARM_OFFSET) + new Vector2(rightArm.Width, rightArm.Height) / 2) * Scale;

    /// <summary>
    /// The knight's collision box
    /// </summary>
    public BoundingRectangle Bounds => new BoundingRectangle(
        Position + new Vector2(BoxOffsetX, BOUNDS_Y * Scale),
        BOUNDS_WIDTH * Scale,
        BOUNDS_HEIGHT * Scale);

    /// <summary>
    /// The X offset of the collision box from Position
    /// </summary>
    private float BoxOffsetX => (FacingRight ? -(BOUNDS_X + BOUNDS_WIDTH) : BOUNDS_X) * Scale;

    /// <summary>
    /// Flips the artwork when the knight faces right
    /// </summary>
    private SpriteEffects Flip => FacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

    /// <summary>
    /// Where Position sits when the knight stands on the road
    /// </summary>
    private float GroundLine => AshvaleGame.GROUND_Y - FOOT_OFFSET * Scale;

    /// <summary>
    /// Where the knight's feet are
    /// </summary>
    private float Feet => Position.Y + FOOT_OFFSET * Scale;

    /// <summary>
    /// Loads the body parts and makes the glow texture
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

        // white copy of the shield arm
        var pixels = new Color[rightArm.Width * rightArm.Height];
        rightArm.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte alpha = pixels[i].A;
            pixels[i] = new Color(alpha, alpha, alpha, alpha);
        }
        shieldOutline = new Texture2D(rightArm.GraphicsDevice, rightArm.Width, rightArm.Height);
        shieldOutline.SetData(pixels);
    }

    /// <summary>
    /// Puts the knight back on the title screen
    /// </summary>
    /// <param name="position">Where to put him</param>
    public void ReturnToTitle(Vector2 position)
    {
        Position = position;
        Scale = TITLE_SCALE;
        FacingRight = true;
        Bobbing = true;
        bobTimer = 0;
        movingDown = false;
        velocity = Vector2.Zero;
    }

    /// <summary>
    /// Stands the knight on the road
    /// </summary>
    /// <param name="x">Where on the road</param>
    public void PlaceOnGround(float x)
    {
        Position = new Vector2(x, GroundLine);
        velocity = Vector2.Zero;
        onGround = true;
    }

    /// <summary>
    /// Jumps, if the knight is on the ground
    /// </summary>
    /// <returns>Whether he jumped</returns>
    public bool Jump()
    {
        if (!onGround) return false;

        velocity.Y = -JUMP_SPEED;
        onGround = false;
        return true;
    }

    /// <summary>
    /// Drops the knight through the ledge he is on
    /// </summary>
    /// <returns>Whether he dropped</returns>
    public bool DropThrough()
    {
        if (!onGround || Feet >= AshvaleGame.GROUND_Y) return false;

        // step just below the ledge top
        Position.Y += Tilemap.LANDING_TOLERANCE + 1;
        velocity.Y = 0;
        onGround = false;
        return true;
    }

    /// <summary>
    /// Bobs the knight on the title screen, or moves him in play
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="tilemap">The level</param>
    public void Update(GameTime gameTime, Tilemap tilemap)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;
        JustLanded = false;

        if (Bobbing)
        {
            bobTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (bobTimer > BOB_INTERVAL)
            {
                movingDown = !movingDown;
                bobTimer -= BOB_INTERVAL;
            }

            if (movingDown)
                Position += new Vector2(0, 1) * BOB_SPEED * t;
            else
                Position += new Vector2(0, -1) * BOB_SPEED * t;

            return;
        }

        float oldFeet = Feet;

        // gravity
        velocity.Y += GRAVITY * t;
        Position += velocity * t;

        // land on a tile
        bool wasOnGround = onGround;
        onGround = false;
        if (tilemap.TryLand(Bounds.Left, Bounds.Right, oldFeet, Feet, out float top))
        {
            Position.Y = top - FOOT_OFFSET * Scale;
            velocity.Y = 0;
            onGround = true;
            if (!wasOnGround) JustLanded = true;
        }

        // keep him in the world
        Position.X = MathHelper.Clamp(Position.X,
                                      -BoxOffsetX,
                                      AshvaleGame.WORLD_WIDTH - BoxOffsetX - BOUNDS_WIDTH * Scale);
    }

    /// <summary>
    /// Draws the assembled knight
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // offsets to each part's top-left corner
        DrawPart(spriteBatch, leftArm, new Vector2(-215, -140));
        DrawPart(spriteBatch, leftLeg, new Vector2(-58, 60));
        DrawPart(spriteBatch, rightLeg, new Vector2(4, 55));
        DrawPart(spriteBatch, body, new Vector2(-52, -46));
        DrawPart(spriteBatch, head, new Vector2(-40, -118));
        DrawPart(spriteBatch, rightArm, SHIELD_ARM_OFFSET);
    }

    /// <summary>
    /// Draws the glow's halo behind the shield
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch, using additive blending</param>
    public void DrawShieldHalo(GameTime gameTime, SpriteBatch spriteBatch)
    {
        float pulse = Pulse(gameTime);
        for (int i = 0; i < HALO_SIZES.Length; i++)
            DrawShieldGlow(spriteBatch, HALO_SIZES[i], GLOW_COLOR * (HALO_BRIGHTNESS[i] * pulse));
    }

    /// <summary>
    /// Draws the glow's shine over the shield
    /// </summary>
    /// <param name="gameTime">The game time</param>
    /// <param name="spriteBatch">The SpriteBatch, using additive blending</param>
    public void DrawShieldShine(GameTime gameTime, SpriteBatch spriteBatch)
    {
        DrawShieldGlow(spriteBatch, 1, GLOW_COLOR * (SHINE_BRIGHTNESS * Pulse(gameTime)));
    }

    /// <summary>
    /// How strong the glow is right now
    /// </summary>
    private static float Pulse(GameTime gameTime)
    {
        return 0.8f + 0.2f * MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * PULSE_SPEED);
    }

    /// <summary>
    /// Draws one layer of the glow
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch</param>
    /// <param name="size">Its size compared to the shield</param>
    /// <param name="color">Its color and brightness</param>
    private void DrawShieldGlow(SpriteBatch spriteBatch, float size, Color color)
    {
        var origin = new Vector2(shieldOutline.Width, shieldOutline.Height) / 2;
        spriteBatch.Draw(shieldOutline, ShieldCenter, null, color, 0f, origin, Scale * size, Flip, 0f);
    }

    /// <summary>
    /// Draws one body part
    /// </summary>
    private void DrawPart(SpriteBatch spriteBatch, Texture2D texture, Vector2 offset)
    {
        spriteBatch.Draw(texture, Position + PartOffset(texture, offset) * Scale, null, Color.White,
                         0f, new Vector2(0,0), Scale, Flip, 0f);
    }

    /// <summary>
    /// Where a body part goes from Position, mirrored when the knight faces right
    /// </summary>
    /// <param name="texture">The body part</param>
    /// <param name="offset">Where it goes when he faces left</param>
    /// <returns>Where it goes now</returns>
    private Vector2 PartOffset(Texture2D texture, Vector2 offset)
    {
        if (FacingRight) offset.X = -(offset.X + texture.Width);
        return offset;
    }
}
