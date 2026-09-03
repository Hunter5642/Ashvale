using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TitleScreen;

public class TitleScreenProject : Game
{
    /// <summary>The background art is 600x300 so the window is an exact 2x of it</summary>
    private const int WINDOW_WIDTH = 1200;
    private const int WINDOW_HEIGHT = 600;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D background;
    private KnightSprite knight;
    private SpriteFont medievalSharp;

    private string titleText = "Ashvale";
    private string exitText = "Press Esc or the Back button to exit";

    public TitleScreenProject()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Match the window to twice the background's size so it scales evenly
        _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
        _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
        _graphics.ApplyChanges();

        knight = new KnightSprite()
        {
            Position = new Vector2(WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2 + 40)
        };

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        background = Content.Load<Texture2D>("background");
        medievalSharp = Content.Load<SpriteFont>("medievalsharp");
        knight.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        knight.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        // Background is stretched to fill the window
        _spriteBatch.Draw(background, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);

        knight.Draw(gameTime, _spriteBatch);

        // Title is centered horizontally near the top
        Vector2 titleSize = medievalSharp.MeasureString(titleText);
        Vector2 titlePosition = new Vector2((WINDOW_WIDTH - titleSize.X) / 2, 30);
        // A dark copy offset a few pixels acts as a drop shadow so the text stays readable against the busy background
        _spriteBatch.DrawString(medievalSharp, titleText, titlePosition + new Vector2(3, 3), Color.Black * 0.75f);
        _spriteBatch.DrawString(medievalSharp, titleText, titlePosition, Color.Gold);

        // Exit instructions are centered along the bottom
        Vector2 exitSize = medievalSharp.MeasureString(exitText);
        Vector2 exitPosition = new Vector2((WINDOW_WIDTH - exitSize.X) / 2, WINDOW_HEIGHT - exitSize.Y - 20);
        _spriteBatch.DrawString(medievalSharp, exitText, exitPosition + new Vector2(2, 2), Color.Black * 0.6f);
        _spriteBatch.DrawString(medievalSharp, exitText, exitPosition, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
