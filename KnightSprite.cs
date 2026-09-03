using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TitleScreen;

/// <summary>
/// A class representing the knight on the title screen.  The knight artwork ships
/// as six loose body parts, so this class holds an anchor position and draws every
/// part at a fixed offset from it.  Moving the anchor moves the whole knight.
/// </summary>
public class KnightSprite
{
    /// <summary>How much to shrink the artwork by</summary>
    private const float SCALE = 0.75f;

    /// <summary>How fast the knight drifts, in pixels per second</summary>
    private const float BOB_SPEED = 20;

    /// <summary>How many seconds before the knight reverses direction</summary>
    private const double BOB_INTERVAL = 1.2;

    private Texture2D body;
    private Texture2D head;
    private Texture2D leftArm;
    private Texture2D rightArm;
    private Texture2D leftLeg;
    private Texture2D rightLeg;

    private double bobTimer;

    private bool movingDown = false;

    /// <summary>
    /// The point the knight is built around
    /// Every body part is positioned relative to this
    /// </summary>
    public Vector2 Position;

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
    /// Bobs the knight gently up and down
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
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
            Position += new Vector2(0, 1) * BOB_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;
        else
            Position += new Vector2(0, -1) * BOB_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;
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
    /// Draws a single body part at a scaled offset from the knight's position
    /// </summary>
    private void DrawPart(SpriteBatch spriteBatch, Texture2D texture, Vector2 offset)
    {
        spriteBatch.Draw(texture, Position + offset * SCALE, null, Color.White,
                         0f, new Vector2(0,0), SCALE, SpriteEffects.None, 0f);
    }
}
