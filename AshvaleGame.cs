using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ashvale.Collisions;

namespace Ashvale;

public class AshvaleGame : Game
{
    public enum State
    {
       Title = 0,
       Playing = 1,
       GameOver = 2,
       Won = 3
    };

    /// <summary>The background art is 600x300 so the window is an exact 2x of it</summary>
    public const int WINDOW_WIDTH = 1200;
    public const int WINDOW_HEIGHT = 600;
    private const int COIN_GOAL = 5;
    public const int GROUND_Y = 550;

    /// <summary>How many rocks the volcano throws for show at the start, and how they are spaced out</summary>
    private const int ERUPTION_COUNT = 5;
    private const double ERUPTION_INTERVAL = 0.25;

    /// <summary>
    /// How many rocks fall at once, how long the knight gets to move before the first one
    /// arrives, and how far apart the rest follow
    /// </summary>
    private const int ROCK_COUNT = 5;
    private const double DROP_DELAY = 1.0;
    private const double DROP_INTERVAL = 0.75;

    /// <summary>How fast the knight runs, in pixels per second</summary>
    private const float MOVE_SPEED = 300;

    /// <summary>Where the knight starts, and how big he is once the game begins</summary>
    private const float KNIGHT_START_X = 80;
    private const float PLAY_SCALE = 0.375f;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Texture2D background;
    private SpriteFont medievalSharp;
    private KnightSprite knight;
    private CoinSprite coin;
    private BoulderSprite[] boulders;
    private EruptionSprite[] eruptions;

    /// <summary>A single white pixel, stretched out to dim or light up the whole screen</summary>
    private Texture2D whitePixel;

    private string titleText = "Ashvale";
    private string exitText = "Press Esc to exit";
    private string startText = "Press Enter to start playing";
    private string goalText = $"Collect {COIN_GOAL} coins to win";
    private string gameOverText = "Game Over\nPress Enter to Play again";
    private string winText = "You Win!\nPress Enter to Play again";

    private State gameState = State.Title;

    private int coinsCollected;

    /// <summary>How much of the opening eruption has been thrown, and the timer between throws</summary>
    private int eruptedRocks;
    private double eruptionTimer;

    /// <summary>
    /// How long the knight has been free to move, how many rocks have been dropped on him
    /// so far, and the timer between drops
    /// </summary>
    private double playTimer;
    private int droppedRocks;
    private double dropTimer;

    /// <summary>Whether the knight has moved yet this run, which hides the reminder of the goal</summary>
    private bool hasMoved;

    private MathHelper.Random random {get; init;} = new();

    protected KeyboardState currentKeyboardState;
    protected KeyboardState priorKeyboardState;

    public AshvaleGame()
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

        coin = new CoinSprite(random);

        boulders = new BoulderSprite[ROCK_COUNT];
        for (int i = 0; i < boulders.Length; i++) boulders[i] = new BoulderSprite(random);

        eruptions = new EruptionSprite[ERUPTION_COUNT];
        for (int i = 0; i < eruptions.Length; i++) eruptions[i] = new EruptionSprite(random);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        background = Content.Load<Texture2D>("background");
        medievalSharp = Content.Load<SpriteFont>("medievalsharp");
        knight.LoadContent(Content);
        coin.LoadContent(Content);
        foreach (var boulder in boulders) boulder.LoadContent(Content);
        foreach (var eruption in eruptions) eruption.LoadContent(Content);

        whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        currentKeyboardState = Keyboard.GetState();

        if (currentKeyboardState.IsKeyDown(Keys.Escape))
            Exit();

        switch (gameState)
        {
            case State.Playing:
                UpdatePlaying(gameTime);
                break;

            default:
                knight.Update(gameTime);
                if (currentKeyboardState.IsKeyDown(Keys.Enter) && priorKeyboardState.IsKeyUp(Keys.Enter))
                    StartGame();
                break;
        }

        priorKeyboardState = currentKeyboardState;

        base.Update(gameTime);
    }

    /// <summary>
    /// Puts everything back to its starting state and begins a run. Restarting after a win
    /// or a loss comes through here too
    /// </summary>
    private void StartGame()
    {
        gameState = State.Playing;

        // the knight stops drifting, shrinks to playing size, and stands at the bottom left
        knight.Bobbing = false;
        knight.Scale = PLAY_SCALE;
        knight.FacingRight = true;
        knight.PlaceOnGround(KNIGHT_START_X);

        coinsCollected = 0;
        // the first coin of a run is always at the far right, so the opening move is a run for it
        coin.MoveToRightEdge();

        foreach (var boulder in boulders) boulder.Reset();
        foreach (var eruption in eruptions) eruption.Reset();

        hasMoved = false;
        eruptedRocks = 0;
        eruptionTimer = 0;
        playTimer = 0;
        droppedRocks = 0;
        dropTimer = 0;
    }

    /// <summary>
    /// Updates a run in progress. A run opens with the volcano erupting while the knight
    /// watches, then he is free to move, and a second later rocks start falling on him
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void UpdatePlaying(GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // The volcano throws its rocks one at a time. The knight is held still until it's done
        if (eruptedRocks < eruptions.Length)
        {
            eruptionTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (eruptionTimer > ERUPTION_INTERVAL)
            {
                eruptions[eruptedRocks].Erupt();
                eruptedRocks++;
                eruptionTimer -= ERUPTION_INTERVAL;
            }
        }
        else
        {
            playTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // the knight turns to face the way he is running, and keeps facing that way when he stops
            if (currentKeyboardState.IsKeyDown(Keys.Left) || currentKeyboardState.IsKeyDown(Keys.A))
            {
                knight.Position += new Vector2(-MOVE_SPEED * t, 0);
                knight.FacingRight = false;
                hasMoved = true;
            }
            if (currentKeyboardState.IsKeyDown(Keys.Right) || currentKeyboardState.IsKeyDown(Keys.D))
            {
                knight.Position += new Vector2(MOVE_SPEED * t, 0);
                knight.FacingRight = true;
                hasMoved = true;
            }
            if (currentKeyboardState.IsKeyDown(Keys.Space) && priorKeyboardState.IsKeyUp(Keys.Space))
            {
                knight.Jump();
                hasMoved = true;
            }
        }

        // After a second of free movement, rocks begin falling, one every DROP_INTERVAL.
        // Each one recycles itself from then on, so this only has to start them off
        if (playTimer > DROP_DELAY && droppedRocks < boulders.Length)
        {
            dropTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (droppedRocks == 0 || dropTimer > DROP_INTERVAL)
            {
                boulders[droppedRocks].FallFromSky();
                droppedRocks++;
                dropTimer = 0;
            }
        }

        knight.Update(gameTime);
        coin.Update(gameTime);
        foreach (var boulder in boulders) boulder.Update(gameTime);
        foreach (var eruption in eruptions) eruption.Update(gameTime);

        // Grabbing the coin moves it somewhere new, until there are enough of them to win
        if (knight.Bounds.CollidesWith(coin.Bounds))
        {
            coinsCollected++;
            if (coinsCollected >= COIN_GOAL)
            {
                // the last coin is picked up rather than moved, so it vanishes with the win
                coin.Collected = true;
                gameState = State.Won;
                return;
            }
            coin.MoveToRandomSpot(knight.Bounds);
        }

        // Any falling rock ends the run. The eruption rocks are scenery and never collide
        foreach (var boulder in boulders)
        {
            if (boulder.Active && knight.Bounds.CollidesWith(boulder.Bounds))
            {
                gameState = State.GameOver;
                return;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        // Background is stretched to fill the window
        _spriteBatch.Draw(background, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);

        // The coin and rocks only exist once a game has started
        if (gameState != State.Title)
        {
            coin.Draw(gameTime, _spriteBatch);
            foreach (var boulder in boulders) boulder.Draw(gameTime, _spriteBatch);
            foreach (var eruption in eruptions) eruption.Draw(gameTime, _spriteBatch);
        }

        knight.Draw(gameTime, _spriteBatch);

        switch (gameState)
        {
            case State.Title:
                DrawCenteredText(titleText, 30, Color.Gold);
                DrawCenteredText(startText, 30 + medievalSharp.LineSpacing, Color.White);
                break;

            case State.Playing:
                // the reminder of the goal gives way to the coin counter once the knight moves
                if (hasMoved)
                    DrawCenteredText($"Coins: {coinsCollected} / {COIN_GOAL}", 20, Color.Gold);
                else
                    DrawCenteredText(goalText, 20, Color.White);
                break;

            case State.GameOver:
                // a wash of black dims the whole scene behind the message
                DrawOverlay(Color.Black * 0.6f);
                DrawCenteredText(gameOverText, CenterHeight(gameOverText), Color.OrangeRed);
                break;

            case State.Won:
                // a wash of white lights the scene up instead
                DrawOverlay(Color.White * 0.5f);
                DrawCenteredText(winText, CenterHeight(winText), Color.Gold);
                break;
        }

        // Exit instructions are centered along the bottom of every screen
        DrawCenteredText(exitText, WINDOW_HEIGHT - medievalSharp.LineSpacing - 20, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Stretches the single white pixel over the whole window in the given color, which
    /// dims or lights up everything drawn underneath it
    /// </summary>
    /// <param name="color">The color to wash the screen with</param>
    private void DrawOverlay(Color color)
    {
        _spriteBatch.Draw(whitePixel, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), color);
    }

    /// <summary>
    /// The height that centers a block of text in the window
    /// </summary>
    /// <param name="text">The text, which may run to several lines</param>
    private float CenterHeight(string text)
    {
        return (WINDOW_HEIGHT - text.Split('\n').Length * medievalSharp.LineSpacing) / 2;
    }

    /// <summary>
    /// Draws text centered across the window, one line at a time so each line is centered
    /// on its own. A dark copy offset a few pixels acts as a drop shadow so the text stays
    /// readable against the busy background
    /// </summary>
    /// <param name="text">The text to draw, which may run to several lines</param>
    /// <param name="y">The height to start drawing at</param>
    /// <param name="color">The color to draw the text in</param>
    private void DrawCenteredText(string text, float y, Color color)
    {
        foreach (string line in text.Split('\n'))
        {
            Vector2 size = medievalSharp.MeasureString(line);
            Vector2 position = new Vector2((WINDOW_WIDTH - size.X) / 2, y);
            _spriteBatch.DrawString(medievalSharp, line, position + new Vector2(3, 3), Color.Black * 0.75f);
            _spriteBatch.DrawString(medievalSharp, line, position, color);
            y += medievalSharp.LineSpacing;
        }
    }
}
