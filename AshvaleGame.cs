using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Ashvale.Collisions;
using Ashvale.Particles;

namespace Ashvale;

public class AshvaleGame : Game
{
    public enum State
    {
       Title = 0,
       Playing = 1,
       GameOver = 2,
       Won = 3,
       Settings = 4,
       Controls = 5,
       HowToPlay = 6
    };

    /// <summary>
    /// The background art is 600x300 so the window is an exact 2x of it
    /// </summary>
    public const int WINDOW_WIDTH = 1200;
    public const int WINDOW_HEIGHT = 600;

    /// <summary>
    /// How wide the world the knight can run around in is. It is three screens across, and
    /// the camera scrolls to follow him
    /// </summary>
    public const int WORLD_WIDTH = WINDOW_WIDTH * 3;
    private const int COIN_GOAL = 5;
    public const int GROUND_Y = 550;

    /// <summary>
    /// How many rocks the volcano throws for show at the start, and how they are spaced out
    /// </summary>
    private const int ERUPTION_COUNT = 5;
    private const double ERUPTION_INTERVAL = 0.25;

    /// <summary>
    /// How many rocks fall at once, how long the knight gets to move before the first one
    /// arrives, and how far apart the rest follow
    /// </summary>
    private const int ROCK_COUNT = 5;
    private const double DROP_DELAY = 1.0;
    private const double DROP_INTERVAL = 0.75;

    /// <summary>
    /// How fast the knight runs, in pixels per second
    /// </summary>
    private const float MOVE_SPEED = 300;

    /// <summary>
    /// Where the knight starts, and how big he is once the game begins
    /// </summary>
    private const float KNIGHT_START_X = 80;
    private const float PLAY_SCALE = 0.375f;

    /// <summary>
    /// Where the knight stands on the title screen. He sits left of center, facing the
    /// column of buttons on the right
    /// </summary>
    private const float TITLE_KNIGHT_X = 380;
    private const float TITLE_KNIGHT_Y = WINDOW_HEIGHT / 2 + 40;
    private const int TITLE_BUTTONS_X = 860;

    /// <summary>
    /// How loud the music plays, and how far it drops when a run ends so the win or
    /// loss sound stands out over it
    /// </summary>
    private const float MUSIC_VOLUME = 0.5f;
    private const float MUSIC_DUCKED_VOLUME = 0.2f;

    /// <summary>
    /// How far the camera zooms in on the volcano while it erupts at the start of a run,
    /// and on the knight when a run ends
    /// </summary>
    private const float INTRO_ZOOM = 1.4f;
    private const float ENDING_ZOOM = 1.2f;

    /// <summary>
    /// How often a firework goes off once the run is won
    /// </summary>
    private const double FIREWORK_INTERVAL = 0.35;

    /// <summary>
    /// The strip across the top of the screen that shows the whole world, where the knight
    /// is in it, and where the coin is
    /// </summary>
    private static readonly Rectangle MINIMAP = new Rectangle(WINDOW_WIDTH / 2 - 200, 72, 400, 10);

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private SpriteFont medievalSharp;
    /// <summary>
    /// A smaller copy of the same font, for the longer text on the settings screens
    /// </summary>
    private SpriteFont medievalSharpSmall;
    private KnightSprite knight;
    private CoinSprite coin;
    private BoulderSprite[] boulders;
    private EruptionSprite[] eruptions;

    private Camera camera;
    private ParallaxBackground background;
    private Ground ground;

    /// <summary>
    /// The particle effects. The game draws these itself, in among the other layers, rather
    /// than letting them draw on top of everything after the menus
    /// </summary>
    private EmberParticleSystem embers;
    private AshParticleSystem ash;
    private DustParticleSystem dust;
    private DebrisParticleSystem debris;
    private ExplosionParticleSystem explosions;
    private SparkleParticleSystem sparkles;

    private Song music;
    private SoundEffect coinSound;
    private SoundEffect jumpSound;
    private SoundEffect hitSound;
    private SoundEffect eruptionSound;
    private SoundEffect winSound;
    private SoundEffect clickSound;

    /// <summary>
    /// The buttons on the title screen
    /// </summary>
    private Button startButton;
    private Button settingsButton;
    private Button quitButton;

    /// <summary>
    /// The buttons on the settings screen
    /// </summary>
    private Button controlsButton;
    private Button howToPlayButton;

    /// <summary>
    /// Sits at the bottom of the settings, controls and how to play screens, and goes
    /// back one screen from whichever one is showing
    /// </summary>
    private Button backButton;

    /// <summary>
    /// The buttons shown once a run is won or lost
    /// </summary>
    private Button playAgainButton;
    private Button mainMenuButton;

    /// <summary>
    /// A single white pixel, stretched out to dim or light up the whole screen
    /// </summary>
    private Texture2D whitePixel;

    private string titleText = "Ashvale";
    private string exitText = "Press Esc to exit, M to mute";
    private string goalText = $"Collect {COIN_GOAL} coins to win";
    private string gameOverText = "Game Over";
    private string winText = "You Win!";

    /// <summary>
    /// Each thing the player can do, and the key or button that does it
    /// </summary>
    private string[,] controls =
    {
        { "Run left", "Left Arrow or A" },
        { "Run right", "Right Arrow or D" },
        { "Jump", "Space" },
        { "Press a button", "Left Mouse Button" },
        { "Mute the sound", "M" },
        { "Exit the game", "Esc" }
    };

    /// <summary>
    /// The how to play screen, as pairs of a heading and the text under it
    /// </summary>
    private string[,] howToPlay =
    {
        { "Objective",
          $"Help the knight collect {COIN_GOAL} coins at the foot of an erupting volcano." },
        { "What Happens",
          "Each run opens with the volcano throwing up a burst of rocks.\n" +
          "Once it settles you can move, and soon rocks rain from the sky.\n" +
          "Only one coin is out at a time, and the land stretches well past the\n" +
          "screen. Follow the map at the top to find it, and jump for the high ones." },
        { "Winning and Losing",
          $"Collect all {COIN_GOAL} coins to win. A single hit from a falling rock ends the run." }
    };

    private State gameState = State.Title;

    private int coinsCollected;

    /// <summary>
    /// How much of the opening eruption has been thrown, and the timer between throws
    /// </summary>
    private int eruptedRocks;
    private double eruptionTimer;

    /// <summary>
    /// How long the knight has been free to move, how many rocks have been dropped on him
    /// so far, and the timer between drops
    /// </summary>
    private double playTimer;
    private int droppedRocks;
    private double dropTimer;

    /// <summary>
    /// Whether the knight has moved yet this run, which hides the reminder of the goal
    /// </summary>
    private bool hasMoved;

    /// <summary>
    /// Counts down to the next victory firework
    /// </summary>
    private double fireworkTimer;

    private MathHelper.Random random {get; init;} = new();

    protected KeyboardState currentKeyboardState;
    protected KeyboardState priorKeyboardState;
    protected MouseState currentMouseState;
    protected MouseState priorMouseState;

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

        camera = new Camera(random);
        Services.AddService(camera);

        background = new ParallaxBackground();
        ground = new Ground();

        embers = new EmberParticleSystem(this, ParallaxBackground.VolcanoMouths);
        ash = new AshParticleSystem(this);
        dust = new DustParticleSystem(this, 20);
        debris = new DebrisParticleSystem(this, 20);
        explosions = new ExplosionParticleSystem(this, 4);
        sparkles = new SparkleParticleSystem(this, 20);

        foreach (ParticleSystem system in new ParticleSystem[] { embers, ash, dust, debris, explosions, sparkles })
        {
            system.Visible = false;
            Components.Add(system);
        }

        knight = new KnightSprite();
        knight.ReturnToTitle(new Vector2(TITLE_KNIGHT_X, TITLE_KNIGHT_Y));

        startButton = new Button("Start Game", TITLE_BUTTONS_X, 230);
        settingsButton = new Button("Settings", TITLE_BUTTONS_X, 320);
        quitButton = new Button("Quit", TITLE_BUTTONS_X, 410);

        controlsButton = new Button("Controls", WINDOW_WIDTH / 2, 230);
        howToPlayButton = new Button("How to Play", WINDOW_WIDTH / 2, 320);
        backButton = new Button("Back", WINDOW_WIDTH / 2, 490);

        playAgainButton = new Button("Play Again", WINDOW_WIDTH / 2, 300);
        mainMenuButton = new Button("Main Menu", WINDOW_WIDTH / 2, 390);

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

        background.LoadContent(Content);
        ground.LoadContent(Content);
        medievalSharp = Content.Load<SpriteFont>("medievalsharp");
        medievalSharpSmall = Content.Load<SpriteFont>("medievalsharp_small");
        knight.LoadContent(Content);
        coin.LoadContent(Content);
        foreach (var boulder in boulders) boulder.LoadContent(Content);
        foreach (var eruption in eruptions) eruption.LoadContent(Content);

        whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });

        coinSound = Content.Load<SoundEffect>("coin");
        jumpSound = Content.Load<SoundEffect>("jump");
        hitSound = Content.Load<SoundEffect>("hit");
        eruptionSound = Content.Load<SoundEffect>("eruption");
        winSound = Content.Load<SoundEffect>("win");
        clickSound = Content.Load<SoundEffect>("click");

        // The music starts on the title screen and loops for as long as the game is open
        music = Content.Load<Song>("music");
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = MUSIC_VOLUME;
        MediaPlayer.Play(music);
    }

    protected override void Update(GameTime gameTime)
    {
        currentKeyboardState = Keyboard.GetState();
        currentMouseState = Mouse.GetState();

        if (currentKeyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // M silences the music and sound effects together, and brings them back
        if (currentKeyboardState.IsKeyDown(Keys.M) && priorKeyboardState.IsKeyUp(Keys.M))
        {
            MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
            SoundEffect.MasterVolume = MediaPlayer.IsMuted ? 0 : 1;
        }

        camera.Update(gameTime);

        // Each screen only listens to its own buttons. A click that changes the screen is
        // over by the next frame, so it never presses a button on the new screen too
        switch (gameState)
        {
            case State.Title:
                knight.Update(gameTime);
                if (Clicked(startButton)) StartGame();
                else if (Clicked(settingsButton)) gameState = State.Settings;
                else if (Clicked(quitButton)) Exit();
                break;

            case State.Settings:
                if (Clicked(controlsButton)) gameState = State.Controls;
                else if (Clicked(howToPlayButton)) gameState = State.HowToPlay;
                else if (Clicked(backButton)) gameState = State.Title;
                break;

            case State.Controls:
            case State.HowToPlay:
                if (Clicked(backButton)) gameState = State.Settings;
                break;

            case State.Playing:
                UpdatePlaying(gameTime);
                break;

            case State.GameOver:
            case State.Won:
                // the knight keeps falling if he was mid-jump when the run ended
                knight.Update(gameTime);
                if (gameState == State.Won) UpdateFireworks(gameTime);
                if (Clicked(playAgainButton)) StartGame();
                else if (Clicked(mainMenuButton)) ReturnToTitle();
                break;
        }

        priorKeyboardState = currentKeyboardState;
        priorMouseState = currentMouseState;

        base.Update(gameTime);
    }

    /// <summary>
    /// Whether the given button was clicked this frame, playing the click sound if it was
    /// </summary>
    /// <param name="button">The button to check</param>
    private bool Clicked(Button button)
    {
        if (!button.IsClicked(currentMouseState, priorMouseState)) return false;

        clickSound.Play();
        return true;
    }

    /// <summary>
    /// Goes back to the title screen after a run, with the knight standing where he
    /// does there and the music back up to full volume
    /// </summary>
    private void ReturnToTitle()
    {
        gameState = State.Title;
        knight.ReturnToTitle(new Vector2(TITLE_KNIGHT_X, TITLE_KNIGHT_Y));
        MediaPlayer.Volume = MUSIC_VOLUME;
        camera.Reset();
    }

    /// <summary>
    /// Puts everything back to its starting state and begins a run. Restarting after a win
    /// or a loss comes through here too
    /// </summary>
    private void StartGame()
    {
        gameState = State.Playing;

        // the music comes back up if the last run ended with it turned down
        MediaPlayer.Volume = MUSIC_VOLUME;

        // the knight stops drifting, shrinks to playing size, and stands at the bottom left
        knight.Bobbing = false;
        knight.Scale = PLAY_SCALE;
        knight.FacingRight = true;
        knight.PlaceOnGround(KNIGHT_START_X);

        coinsCollected = 0;
        // the first coin of a run is always off to the right, so the opening move is a run for it
        coin.MoveToFirstSpot();

        // Back to the start of the world, zoomed in on the volcano for the eruption. The
        // background scrolls at half speed but the camera starts at 0, so the volcano's
        // background position is also where it is on screen
        camera.Reset();
        camera.SnapZoom(INTRO_ZOOM, ParallaxBackground.VolcanoMouths[0]);

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
                embers.Burst(ParallaxBackground.VolcanoMouths[0]);
                camera.Shake(3, 0.25f);
                eruptionSound.Play();
                eruptedRocks++;
                eruptionTimer -= ERUPTION_INTERVAL;

                // with the last rock thrown, pull back out to the whole scene
                if (eruptedRocks == eruptions.Length) camera.TargetZoom = 1;
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
                // pressing jump in mid-air does nothing, so it shouldn't make a sound either
                if (knight.Jump()) jumpSound.Play();
                hasMoved = true;
            }

            camera.Follow(knight.Position.X, gameTime);
        }

        // After a second of free movement, rocks begin falling, one every DROP_INTERVAL.
        // Each one recycles itself from then on, so this only has to start them off
        if (playTimer > DROP_DELAY && droppedRocks < boulders.Length)
        {
            dropTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (droppedRocks == 0 || dropTimer > DROP_INTERVAL)
            {
                boulders[droppedRocks].FallFromSky(camera.ViewLeft, camera.ViewRight);
                droppedRocks++;
                dropTimer = 0;
            }
        }

        knight.Update(gameTime);
        coin.Update(gameTime);
        foreach (var boulder in boulders) boulder.Update(gameTime);
        foreach (var eruption in eruptions) eruption.Update(gameTime);

        if (knight.JustLanded) dust.Burst(new Vector2(knight.Position.X, GROUND_Y));

        // A rock that reaches the ground smashes into chips and dust and gives the screen a
        // jolt, then goes back up to fall again over wherever the camera is now
        foreach (var boulder in boulders)
        {
            if (!boulder.Landed) continue;

            var impact = new Vector2(boulder.Position.X, GROUND_Y);
            debris.Burst(impact);
            dust.Burst(impact);
            camera.Shake(1.5f, 0.15f);
            boulder.FallFromSky(camera.ViewLeft, camera.ViewRight);
        }

        // Grabbing the coin moves it somewhere new, until there are enough of them to win
        if (knight.Bounds.CollidesWith(coin.Bounds))
        {
            coinsCollected++;
            sparkles.Burst(coin.Position, Color.Gold);
            if (coinsCollected >= COIN_GOAL)
            {
                // the last coin is picked up rather than moved, so it vanishes with the win
                coin.Collected = true;
                gameState = State.Won;
                ZoomInOnKnight();
                fireworkTimer = 0;
                MediaPlayer.Volume = MUSIC_DUCKED_VOLUME;
                winSound.Play();
                return;
            }
            coinSound.Play();
            coin.MoveToRandomSpot(knight.Bounds);
        }

        // Any falling rock ends the run. The eruption rocks are scenery and never collide
        foreach (var boulder in boulders)
        {
            if (boulder.Active && knight.Bounds.CollidesWith(boulder.Bounds))
            {
                gameState = State.GameOver;
                MediaPlayer.Volume = MUSIC_DUCKED_VOLUME;
                hitSound.Play();

                Vector2 knightCenter = KnightCenter();
                explosions.PlaceExplosion(knightCenter);
                debris.Burst(knightCenter);
                camera.Shake(6, 0.4f);
                ZoomInOnKnight();
                return;
            }
        }
    }

    /// <summary>
    /// The middle of the knight's collision box, in world coordinates
    /// </summary>
    private Vector2 KnightCenter()
    {
        var bounds = knight.Bounds;
        return new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    /// <summary>
    /// Slowly closes in on the knight at the end of a run. The zoom works in screen
    /// coordinates, so the knight's world position is turned into where he is on screen
    /// </summary>
    private void ZoomInOnKnight()
    {
        camera.ZoomFocus = KnightCenter() - camera.Position;
        camera.TargetZoom = ENDING_ZOOM;
    }

    /// <summary>
    /// Sets off a firework every so often over the screen while the win is showing
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void UpdateFireworks(GameTime gameTime)
    {
        fireworkTimer -= gameTime.ElapsedGameTime.TotalSeconds;
        if (fireworkTimer > 0) return;

        fireworkTimer += FIREWORK_INTERVAL;
        var where = new Vector2(random.NextFloat(camera.ViewLeft + 150, camera.ViewRight - 150),
                                random.NextFloat(80, 300));
        var colors = SparkleParticleSystem.FireworkColors;
        sparkles.Burst(where, colors[(int)random.NextFloat(0, colors.Length - 0.01f)]);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // The world is drawn first, through the camera, and the menus and text go on top of
        // it in a batch of their own that the camera doesn't move
        if (gameState is State.Title or State.Playing or State.GameOver or State.Won)
            DrawScene(gameTime);

        _spriteBatch.Begin();

        switch (gameState)
        {
            case State.Title:
                DrawCenteredText(medievalSharp, titleText, 30, Color.Gold);
                startButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                settingsButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                quitButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.Settings:
                DrawCenteredText(medievalSharp, "Settings", 60, Color.Gold);
                controlsButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                howToPlayButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                backButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.Controls:
                DrawCenteredText(medievalSharp, "Controls", 30, Color.Gold);
                DrawControls();
                backButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.HowToPlay:
                DrawCenteredText(medievalSharp, "How to Play", 30, Color.Gold);
                DrawHowToPlay();
                backButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.Playing:
                if (hasMoved)
                    DrawCenteredText(medievalSharp, $"Coins: {coinsCollected} / {COIN_GOAL}", 20, Color.Gold);
                else
                    DrawCenteredText(medievalSharp, goalText, 20, Color.White);
                DrawMinimap();
                DrawCoinPointer(gameTime);
                break;

            case State.GameOver:
                DrawOverlay(Color.Black * 0.6f);
                DrawCenteredText(medievalSharp, gameOverText, 170, Color.OrangeRed);
                playAgainButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                mainMenuButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.Won:
                DrawOverlay(Color.White * 0.5f);
                DrawCenteredText(medievalSharp, winText, 170, Color.Gold);
                playAgainButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                mainMenuButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;
        }

        DrawCenteredText(medievalSharpSmall, exitText, WINDOW_HEIGHT - medievalSharpSmall.LineSpacing - 12, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws the world back to front through the camera. The background layer scrolls at
    /// half speed with the volcano's embers on it, then the world layer scrolls at
    /// full speed with the road, coin, rocks and knight, and the effects that happen around
    /// him go over the top
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void DrawScene(GameTime gameTime)
    {
        // Background layer
        _spriteBatch.Begin(transformMatrix: camera.GetTransform(ParallaxBackground.PARALLAX));
        background.Draw(_spriteBatch);
        if (gameState != State.Title)
            foreach (var eruption in eruptions) eruption.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        embers.Draw(gameTime);

        // World layer
        _spriteBatch.Begin(transformMatrix: camera.GetTransform());
        ground.Draw(gameTime, _spriteBatch, whitePixel);
        if (gameState != State.Title)
        {
            coin.Draw(gameTime, _spriteBatch);
            foreach (var boulder in boulders) boulder.Draw(gameTime, _spriteBatch);
        }
        knight.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        dust.Draw(gameTime);
        debris.Draw(gameTime);
        explosions.Draw(gameTime);
        sparkles.Draw(gameTime);
        ash.Draw(gameTime);
    }

    /// <summary>
    /// Draws the whole world shrunk down to a strip, with the part on screen lit up, a
    /// white mark for the knight and a gold one for the coin
    /// </summary>
    private void DrawMinimap()
    {
        float toMap = (float)MINIMAP.Width / WORLD_WIDTH;

        _spriteBatch.Draw(whitePixel, MINIMAP, Color.Black * 0.5f);

        var view = new Rectangle(MINIMAP.X + (int)(camera.ViewLeft * toMap), MINIMAP.Y,
                                 (int)(WINDOW_WIDTH * toMap), MINIMAP.Height);
        _spriteBatch.Draw(whitePixel, view, Color.White * 0.25f);

        int coinX = MINIMAP.X + (int)(coin.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(coinX - 3, MINIMAP.Y - 3, 6, MINIMAP.Height + 6), Color.Gold);

        int knightX = MINIMAP.X + (int)(knight.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(knightX - 2, MINIMAP.Y - 5, 4, MINIMAP.Height + 10), Color.White);
    }

    /// <summary>
    /// When the coin is off the side of the screen, draws a pulsing arrow at that edge
    /// pointing the way to it
    /// </summary>
    /// <param name="gameTime">The game time, which makes the arrow pulse</param>
    private void DrawCoinPointer(GameTime gameTime)
    {
        if (coin.Collected) return;

        bool left = coin.Position.X < camera.ViewLeft;
        bool right = coin.Position.X > camera.ViewRight;
        if (!left && !right) return;

        string arrow = left ? "<" : ">";
        Vector2 size = medievalSharp.MeasureString(arrow);
        float nudge = 6 * (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * 6);
        float x = left ? 20 + nudge : WINDOW_WIDTH - 20 - size.X - nudge;
        var position = new Vector2(x, coin.Position.Y - size.Y / 2);

        _spriteBatch.DrawString(medievalSharp, arrow, position + new Vector2(3, 3), Color.Black * 0.75f);
        _spriteBatch.DrawString(medievalSharp, arrow, position, Color.Gold);
    }

    /// <summary>
    /// Draws the controls as a table, with what each one does on the left and the key
    /// that does it on the right
    /// </summary>
    private void DrawControls()
    {
        float actionX = 330;
        float keyX = 650;
        float y = 130;
        float rowHeight = medievalSharpSmall.LineSpacing + 12;

        _spriteBatch.DrawString(medievalSharpSmall, "Action", new Vector2(actionX, y), Color.Gold);
        _spriteBatch.DrawString(medievalSharpSmall, "Key", new Vector2(keyX, y), Color.Gold);
        y += rowHeight;

        for (int i = 0; i < controls.GetLength(0); i++)
        {
            _spriteBatch.DrawString(medievalSharpSmall, controls[i, 0], new Vector2(actionX, y), Color.White);
            _spriteBatch.DrawString(medievalSharpSmall, controls[i, 1], new Vector2(keyX, y), Color.White);
            y += rowHeight;
        }
    }

    /// <summary>
    /// Draws each section of the how to play screen, a gold heading with its text below
    /// </summary>
    private void DrawHowToPlay()
    {
        float y = 110;

        for (int i = 0; i < howToPlay.GetLength(0); i++)
        {
            DrawCenteredText(medievalSharpSmall, howToPlay[i, 0], y, Color.Gold);
            y += medievalSharpSmall.LineSpacing;

            DrawCenteredText(medievalSharpSmall, howToPlay[i, 1], y, Color.White);
            y += howToPlay[i, 1].Split('\n').Length * medievalSharpSmall.LineSpacing + 14;
        }
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
    /// Draws text centered across the window, one line at a time so each line is centered
    /// on its own. A dark copy offset a few pixels acts as a drop shadow so the text stays
    /// readable against the busy background
    /// </summary>
    /// <param name="font">The font to draw the text in</param>
    /// <param name="text">The text to draw, which may run to several lines</param>
    /// <param name="y">The height to start drawing at</param>
    /// <param name="color">The color to draw the text in</param>
    private void DrawCenteredText(SpriteFont font, string text, float y, Color color)
    {
        foreach (string line in text.Split('\n'))
        {
            Vector2 size = font.MeasureString(line);
            Vector2 position = new Vector2((WINDOW_WIDTH - size.X) / 2, y);
            _spriteBatch.DrawString(font, line, position + new Vector2(3, 3), Color.Black * 0.75f);
            _spriteBatch.DrawString(font, line, position, color);
            y += font.LineSpacing;
        }
    }
}
