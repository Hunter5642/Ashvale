using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// A rock thrown clear of the volcano at the start of a run, for show.  Eruption rocks have no
/// gravity and no collisions: they fly out in a straight line and off the screen, so the player
/// can see where the rocks that fall on them later are coming from.
/// </summary>
public class EruptionSprite
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
    private const double ANIMATION_SPEED = 0.05;

    /// <summary>
    /// How much to blow the 32x32 artwork up by. Smaller than the falling rocks,
    /// since these are away in the distance over the volcano
    /// </summary>
    private const float SCALE = 1.5f;

    /// <summary>
    /// Half the drawn size, used to tell when a rock is completely off the screen
    /// </summary>
    private const float HALF_SIZE = FRAME_SIZE / 2f * SCALE;


    /// <summary>
    /// The game's random number generator, for throwing each rock a little differently
    /// </summary>
    private readonly MathHelper.Random random;

    private Texture2D texture;

    private double animationTimer;

    private int animationFrame;

    private Vector2 position;

    private Vector2 velocity;

    /// <summary>
    /// Whether this rock is still on screen. Eruption rocks are gone for good once they leave
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
    /// Loads the rock sprite sheet using the provided ContentManager
    /// </summary>
    /// <param name="content">The ContentManager to load with</param>
    public void LoadContent(ContentManager content)
    {
        texture = content.Load<Texture2D>("rock_round");
    }

    /// <summary>
    /// Throws this rock up out of the volcano
    /// </summary>
    public void Erupt()
    {
        // the first volcano, the one on screen at the start of a run. Eruption rocks are drawn
        // in the background layer, so this is a background position
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
    /// Flies the rock in a straight line, spinning, until it leaves the screen
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        if (!Active) return;

        // no gravity here, so the rock keeps the speed and direction it was thrown with
        position += velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // update the animation frame
        animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (animationTimer > ANIMATION_SPEED)
        {
            animationFrame++;
            if (animationFrame >= FRAME_COUNT) animationFrame = 0;
            animationTimer -= ANIMATION_SPEED;
        }

        // once it is off the screen it is finished
        if (position.Y + HALF_SIZE < 0
            || position.X + HALF_SIZE < 0
            || position.X - HALF_SIZE > AshvaleGame.WINDOW_WIDTH
            || position.Y - HALF_SIZE > AshvaleGame.WINDOW_HEIGHT)
        {
            Active = false;
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

        spriteBatch.Draw(texture, position, source, Color.White, 0f,
                         new Vector2(FRAME_SIZE / 2f, FRAME_SIZE / 2f), SCALE, SpriteEffects.None, 0f);
    }
}
