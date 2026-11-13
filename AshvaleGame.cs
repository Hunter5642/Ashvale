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
       HowToPlay = 6,
       Story = 7,
       LevelComplete = 8,
       Hit = 9
    };

    /// <summary>
    /// The window size, twice the size of the background art
    /// </summary>
    public const int WINDOW_WIDTH = 1200;
    public const int WINDOW_HEIGHT = 600;

    /// <summary>
    /// How wide the world is: three screens
    /// </summary>
    public const int WORLD_WIDTH = WINDOW_WIDTH * 3;

    /// <summary>
    /// The top of the road
    /// </summary>
    public const int GROUND_Y = 560;

    /// <summary>
    /// The map file for each level, in order
    /// </summary>
    private static readonly string[] LEVEL_FILES = { "level1.txt", "level2.txt", "level3.txt" };

    /// <summary>
    /// How many villagers have to be rescued to clear a level
    /// </summary>
    private const int VILLAGER_GOAL = 5;

    /// <summary>
    /// Lives at the start, and the most the knight can have
    /// </summary>
    private const int STARTING_LIVES = 3;
    private const int MAX_LIVES = 5;

    /// <summary>
    /// How many rocks the opening eruption throws, and the time between them
    /// </summary>
    private const int ERUPTION_COUNT = 5;
    private const double ERUPTION_INTERVAL = 0.25;

    /// <summary>
    /// Rocks falling at the start, extra rocks per level and per rescue, and the most at once
    /// </summary>
    private const int STARTING_ROCKS = 3;
    private const int ROCKS_PER_LEVEL = 1;
    private const int ROCKS_PER_VILLAGER = 1;
    private const int MAX_ROCKS = 8;

    /// <summary>
    /// Rescues needed before the shield power-up appears
    /// </summary>
    private const int SHIELD_AFTER_RESCUES = 2;

    /// <summary>
    /// Time before the first rock falls, and between the rocks after it
    /// </summary>
    private const double DROP_DELAY = 1.0;
    private const double DROP_INTERVAL = 0.75;

    /// <summary>
    /// How fast the knight runs, in pixels per second
    /// </summary>
    private const float MOVE_SPEED = 300;

    /// <summary>
    /// Where the knight starts, and his size in play
    /// </summary>
    private const float KNIGHT_START_X = 80;
    private const float PLAY_SCALE = 0.375f;

    /// <summary>
    /// Where the knight and the buttons are on the title screen
    /// </summary>
    private const float TITLE_KNIGHT_X = 380;
    private const float TITLE_KNIGHT_Y = WINDOW_HEIGHT / 2 + 40;
    private const int TITLE_BUTTONS_X = 860;

    /// <summary>
    /// Music volume, normally and while the win or loss sound plays
    /// </summary>
    private const float MUSIC_VOLUME = 0.5f;
    private const float MUSIC_DUCKED_VOLUME = 0.2f;

    /// <summary>
    /// Camera zoom for the opening eruption and the end of a run
    /// </summary>
    private const float INTRO_ZOOM = 1.4f;
    private const float ENDING_ZOOM = 1.2f;

    /// <summary>
    /// Time between victory fireworks
    /// </summary>
    private const double FIREWORK_INTERVAL = 0.35;

    /// <summary>
    /// The minimap across the top of the screen
    /// </summary>
    private static readonly Rectangle MINIMAP = new Rectangle(WINDOW_WIDTH / 2 - 200, 72, 400, 10);

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private SpriteFont medievalSharp;
    /// <summary>
    /// A smaller copy of the font, for longer text
    /// </summary>
    private SpriteFont medievalSharpSmall;
    private KnightSprite knight;
    private VillagerSprite villager;
    private BoulderSprite[] boulders;
    private EruptionSprite[] eruptions;

    /// <summary>
    /// The shield power-up the knight can pick up partway through a level
    /// </summary>
    private ShieldPickup shield;

    private Camera camera;
    private ParallaxBackground background;

    /// <summary>
    /// The level being played
    /// </summary>
    private Tilemap tilemap;

    /// <summary>
    /// The particle effects, drawn by the game between its layers
    /// </summary>
    private EmberParticleSystem embers;
    private AshParticleSystem ash;
    private DustParticleSystem dust;
    private DebrisParticleSystem debris;
    private ExplosionParticleSystem explosions;
    private SparkleParticleSystem sparkles;

    private Song music;
    private SoundEffect rescueSound;
    private SoundEffect jumpSound;
    private SoundEffect hitSound;
    private SoundEffect eruptionSound;
    private SoundEffect winSound;
    private SoundEffect clickSound;

    /// <summary>
    /// The title screen buttons
    /// </summary>
    private Button continueButton;
    private Button newGameButton;
    private Button settingsButton;
    private Button quitButton;

    /// <summary>
    /// The buttons on the settings screen
    /// </summary>
    private Button controlsButton;
    private Button howToPlayButton;

    /// <summary>
    /// The back button on the settings, controls and how to play screens
    /// </summary>
    private Button backButton;

    /// <summary>
    /// Starts the level from its story screen
    /// </summary>
    private Button beginButton;

    /// <summary>
    /// The buttons shown when a level ends
    /// </summary>
    private Button nextLevelButton;
    private Button tryAgainButton;
    private Button playAgainButton;
    private Button mainMenuButton;

    /// <summary>
    /// A single white pixel, stretched for boxes and overlays
    /// </summary>
    private Texture2D whitePixel;

    private string titleText = "Ashvale";
    private string exitText = "Press Esc to exit, M to mute";
    private string goalText = $"Rescue {VILLAGER_GOAL} villagers to clear the level";
    private string gameOverText = "Game Over";
    private string winText = "You Win!";

    /// <summary>
    /// Each level's name and story
    /// </summary>
    private string[,] story =
    {
        { "The Road to the Castle",
          "The volcano above Ashvale has woken, and rocks are raining down on the valley.\n" +
          "The villagers were caught out on the road, far from the safety of the castle.\n" +
          "Climb up to them on the ledges and send them running for the castle gates." },
        { "The Foothills",
          "The road climbs into the foothills, closer to the volcano.\n" +
          "More villagers are stranded up on the rocks, and the volcano is getting angrier.\n" +
          "More rocks will be falling from the very start." },
        { "The Foot of the Volcano",
          "The last villagers are trapped right under the volcano.\n" +
          "The ledges here are small, so cover is hard to find.\n" +
          "Bring everyone home." }
    };

    /// <summary>
    /// Shown under the win message
    /// </summary>
    private string endingText = "Every villager made it safely behind the castle walls.";

    /// <summary>
    /// Each action and the key that does it
    /// </summary>
    private string[,] controls =
    {
        { "Run left", "Left Arrow or A" },
        { "Run right", "Right Arrow or D" },
        { "Jump", "Space" },
        { "Drop through a ledge", "Down Arrow or S" },
        { "Press a button", "Left Mouse Button" },
        { "Mute the sound", "M" },
        { "Exit the game", "Esc" }
    };

    /// <summary>
    /// Each how to play heading and its text
    /// </summary>
    private string[,] howToPlay =
    {
        { "Objective",
          "Help the knight rescue the villagers stranded at the foot of an erupting volcano." },
        { "What Happens",
          "Each level opens with an eruption, and then rocks rain from the sky.\n" +
          "The villagers wait up on the ledges. Jump up through a ledge to land on it,\n" +
          "press Down to drop back through, and shelter under one when the rocks come.\n" +
          $"After {SHIELD_AFTER_RESCUES} rescues a shield appears. Grab it and your shield blocks one rock." },
        { "Winning and Losing",
          $"Rescue {VILLAGER_GOAL} villagers to clear a level, and clear all {LEVEL_FILES.Length} levels to win.\n" +
          $"A falling rock costs a life. You start with {STARTING_LIVES} and earn 1 more per level.\n" +
          "Your level and lives are saved, so Continue starts your level over." }
    };

    private State gameState = State.Title;

    /// <summary>
    /// The current level, counting from 0, and lives left
    /// </summary>
    private int level;
    private int lives;

    /// <summary>
    /// Villagers rescued on this level
    /// </summary>
    private int villagersRescued;

    /// <summary>
    /// Whether clearing the level earned an extra life
    /// </summary>
    private bool gainedLife;

    /// <summary>
    /// Rocks thrown in the opening eruption so far, and the timer between throws
    /// </summary>
    private int eruptedRocks;
    private double eruptionTimer;

    /// <summary>
    /// Time the knight has been free to move, rocks dropped so far, and the timer between drops
    /// </summary>
    private double playTimer;
    private int droppedRocks;
    private double dropTimer;

    /// <summary>
    /// Whether the knight has moved yet this run
    /// </summary>
    private bool hasMoved;

    /// <summary>
    /// Countdown to the next firework
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
        // set the window size
        _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
        _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;

        // depth buffer for the 3D model
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
        _graphics.ApplyChanges();

        camera = new Camera(random);
        Services.AddService(camera);

        background = new ParallaxBackground();

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

        continueButton = new Button("Continue", TITLE_BUTTONS_X, 200);
        newGameButton = new Button("New Game", TITLE_BUTTONS_X, 285);
        settingsButton = new Button("Settings", TITLE_BUTTONS_X, 370);
        quitButton = new Button("Quit", TITLE_BUTTONS_X, 455);
        continueButton.Enabled = SaveData.Exists;

        controlsButton = new Button("Controls", WINDOW_WIDTH / 2, 230);
        howToPlayButton = new Button("How to Play", WINDOW_WIDTH / 2, 320);
        backButton = new Button("Back", WINDOW_WIDTH / 2, 515);

        beginButton = new Button("Begin", WINDOW_WIDTH / 2, 450);

        nextLevelButton = new Button("Next Level", WINDOW_WIDTH / 2, 300);
        tryAgainButton = new Button("Try Again", WINDOW_WIDTH / 2, 300);
        playAgainButton = new Button("Play Again", WINDOW_WIDTH / 2, 300);
        mainMenuButton = new Button("Main Menu", WINDOW_WIDTH / 2, 390);

        villager = new VillagerSprite(random);

        // every rock a level could need
        boulders = new BoulderSprite[MAX_ROCKS];
        for (int i = 0; i < boulders.Length; i++) boulders[i] = new BoulderSprite(random);

        eruptions = new EruptionSprite[ERUPTION_COUNT];
        for (int i = 0; i < eruptions.Length; i++) eruptions[i] = new EruptionSprite(random);

        shield = new ShieldPickup(random);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        background.LoadContent(Content);
        medievalSharp = Content.Load<SpriteFont>("medievalsharp");
        medievalSharpSmall = Content.Load<SpriteFont>("medievalsharp_small");
        knight.LoadContent(Content);
        villager.LoadContent(Content);

        // first level's road behind the title screen
        LoadLevel(0);
        foreach (var boulder in boulders) boulder.LoadContent(Content);
        foreach (var eruption in eruptions) eruption.LoadContent(Content);

        shield.SetModel(new ShieldModel(GraphicsDevice));

        whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });

        rescueSound = Content.Load<SoundEffect>("coin");
        jumpSound = Content.Load<SoundEffect>("jump");
        hitSound = Content.Load<SoundEffect>("hit");
        eruptionSound = Content.Load<SoundEffect>("eruption");
        winSound = Content.Load<SoundEffect>("win");
        clickSound = Content.Load<SoundEffect>("click");

        // music loops the whole game
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

        // M mutes and unmutes everything
        if (currentKeyboardState.IsKeyDown(Keys.M) && priorKeyboardState.IsKeyUp(Keys.M))
        {
            MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
            SoundEffect.MasterVolume = MediaPlayer.IsMuted ? 0 : 1;
        }

        camera.Update(gameTime);

        // each screen only checks its own buttons
        switch (gameState)
        {
            case State.Title:
                knight.Update(gameTime, tilemap);
                if (Clicked(continueButton)) ContinueGame();
                else if (Clicked(newGameButton)) NewGame();
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

            case State.Story:
                if (Clicked(beginButton)) StartLevel();
                break;

            case State.Playing:
                UpdatePlaying(gameTime);
                break;

            case State.LevelComplete:
                // the knight finishes any jump
                knight.Update(gameTime, tilemap);
                if (Clicked(nextLevelButton))
                {
                    // load the next map
                    LoadLevel(level);
                    gameState = State.Story;
                }
                else if (Clicked(mainMenuButton)) ReturnToTitle();
                break;

            case State.Hit:
                knight.Update(gameTime, tilemap);
                if (Clicked(tryAgainButton)) StartLevel();
                else if (Clicked(mainMenuButton)) ReturnToTitle();
                break;

            case State.GameOver:
            case State.Won:
                knight.Update(gameTime, tilemap);
                if (gameState == State.Won) UpdateFireworks(gameTime);
                if (Clicked(playAgainButton)) NewGame();
                else if (Clicked(mainMenuButton)) ReturnToTitle();
                break;
        }

        priorKeyboardState = currentKeyboardState;
        priorMouseState = currentMouseState;

        base.Update(gameTime);
    }

    /// <summary>
    /// Whether a button was clicked this frame, playing the click sound if so
    /// </summary>
    /// <param name="button">The button to check</param>
    private bool Clicked(Button button)
    {
        if (!button.IsClicked(currentMouseState, priorMouseState)) return false;

        clickSound.Play();
        return true;
    }

    /// <summary>
    /// Goes back to the title screen
    /// </summary>
    private void ReturnToTitle()
    {
        gameState = State.Title;
        knight.ReturnToTitle(new Vector2(TITLE_KNIGHT_X, TITLE_KNIGHT_Y));
        MediaPlayer.Volume = MUSIC_VOLUME;
        camera.Reset();
        LoadLevel(0);

        continueButton.Enabled = SaveData.Exists;
    }

    /// <summary>
    /// Reads in the map for a level
    /// </summary>
    /// <param name="index">Which level, counting from 0</param>
    private void LoadLevel(int index)
    {
        tilemap = new Tilemap(LEVEL_FILES[index]);
        tilemap.LoadContent(Content);
        debris.Tilemap = tilemap;
    }

    /// <summary>
    /// Starts a new game from the first level
    /// </summary>
    private void NewGame()
    {
        level = 0;
        lives = STARTING_LIVES;
        SaveGame();

        LoadLevel(level);
        gameState = State.Story;
    }

    /// <summary>
    /// Continues the saved game from the start of its level
    /// </summary>
    private void ContinueGame()
    {
        SaveData save = SaveData.Load();
        if (save == null)
        {
            // the save couldn't be read
            continueButton.Enabled = false;
            return;
        }

        // keep the saved numbers in range
        level = MathHelper.Clamp(save.Level, 0, LEVEL_FILES.Length - 1);
        lives = MathHelper.Clamp(save.Lives, 1, MAX_LIVES);

        LoadLevel(level);
        gameState = State.Story;
    }

    /// <summary>
    /// Writes the level and lives out to the save file
    /// </summary>
    private void SaveGame()
    {
        var save = new SaveData
        {
            Level = level,
            Lives = lives
        };
        save.Save();
    }

    /// <summary>
    /// Starts the current level from the beginning
    /// </summary>
    private void StartLevel()
    {
        gameState = State.Playing;

        MediaPlayer.Volume = MUSIC_VOLUME;

        // knight to playing size, on the ground at the start
        knight.Bobbing = false;
        knight.Scale = PLAY_SCALE;
        knight.FacingRight = true;
        knight.PlaceOnGround(KNIGHT_START_X);

        villager.MoveToFirstSpot();

        // back to the start, zoomed in on the volcano
        camera.Reset();
        camera.SnapZoom(INTRO_ZOOM, ParallaxBackground.VolcanoMouths[0]);

        foreach (var boulder in boulders) boulder.Reset();
        foreach (var eruption in eruptions) eruption.Reset();
        shield.Reset();
        knight.ShieldGlowing = false;

        villagersRescued = 0;
        hasMoved = false;
        eruptedRocks = 0;
        eruptionTimer = 0;
        playTimer = 0;
        droppedRocks = 0;
        dropTimer = 0;
    }

    /// <summary>
    /// Updates a level in progress
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void UpdatePlaying(GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // opening eruption, one rock at a time
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

                // zoom back out after the last rock
                if (eruptedRocks == eruptions.Length) camera.TargetZoom = 1;
            }
        }
        else
        {
            playTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // run left or right
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
                if (knight.Jump()) jumpSound.Play();
                hasMoved = true;
            }

            // drop through a ledge
            bool downPressed = currentKeyboardState.IsKeyDown(Keys.Down) || currentKeyboardState.IsKeyDown(Keys.S);
            bool downWasPressed = priorKeyboardState.IsKeyDown(Keys.Down) || priorKeyboardState.IsKeyDown(Keys.S);
            if (downPressed && !downWasPressed)
            {
                knight.DropThrough();
                hasMoved = true;
            }

            camera.Follow(knight.Position.X, gameTime);
        }

        // drop rocks until enough are falling
        if (playTimer > DROP_DELAY && droppedRocks < RocksAllowed())
        {
            dropTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (droppedRocks == 0 || dropTimer > DROP_INTERVAL)
            {
                boulders[droppedRocks].FallFromSky(camera.ViewLeft, camera.ViewRight);

                // rocks added by rescues come with a rumble
                if (droppedRocks >= STARTING_ROCKS + level * ROCKS_PER_LEVEL)
                {
                    embers.Burst(NearestVolcanoMouth());
                    eruptionSound.Play(0.5f, 0, 0);
                    camera.Shake(3, 0.25f);
                }

                droppedRocks++;
                dropTimer = 0;
            }
        }

        knight.Update(gameTime, tilemap);
        villager.Update(gameTime);
        foreach (var boulder in boulders) boulder.Update(gameTime, tilemap);
        foreach (var eruption in eruptions) eruption.Update(gameTime);
        shield.Update(gameTime);

        // dust where he lands
        if (knight.JustLanded) dust.Burst(new Vector2(knight.Position.X, knight.Bounds.Bottom));

        // smash rocks that landed
        foreach (var boulder in boulders)
            if (boulder.Landed) SmashRock(boulder);

        // rescue the villager
        if (knight.Bounds.CollidesWith(villager.Bounds))
        {
            villagersRescued++;
            sparkles.Burst(villager.Center, Color.Gold);
            if (villagersRescued >= VILLAGER_GOAL)
            {
                villager.Rescued = true;
                CompleteLevel();
                return;
            }
            rescueSound.Play();
            villager.MoveToRandomSpot(knight.Bounds, tilemap);

            // shield power-up appears
            if (villagersRescued == SHIELD_AFTER_RESCUES)
            {
                shield.Appear(knight.Bounds, villager.Position, tilemap);
                sparkles.Burst(shield.Position, Color.LightSkyBlue);
            }
        }

        // pick up the shield power-up
        if (shield.Waiting && knight.Bounds.CollidesWith(shield.Bounds))
        {
            shield.PickUp();
            knight.ShieldGlowing = true;
            sparkles.Burst(shield.Position, Color.Gold);
            sparkles.Burst(knight.ShieldCenter, Color.LightSkyBlue);
            rescueSound.Play(0.7f, 0.5f, 0);
        }

        // a falling rock costs a life
        bool blocked = false;
        foreach (var boulder in boulders)
        {
            if (boulder.Active && knight.Bounds.CollidesWith(boulder.Bounds))
            {
                // the glowing shield blocks the hit
                if (knight.ShieldGlowing || blocked)
                {
                    if (!blocked) BreakShield();
                    blocked = true;
                    SmashRock(boulder);
                    continue;
                }

                MediaPlayer.Volume = MUSIC_DUCKED_VOLUME;
                hitSound.Play();

                Vector2 knightCenter = knight.Bounds.Center;
                explosions.PlaceExplosion(knightCenter);
                debris.Burst(knightCenter);
                camera.Shake(6, 0.4f);
                ZoomInOnKnight();

                // game over with no lives left
                lives--;
                if (lives > 0)
                {
                    gameState = State.Hit;
                    SaveGame();
                }
                else
                {
                    gameState = State.GameOver;
                    SaveData.Delete();
                }
                return;
            }
        }
    }

    /// <summary>
    /// Uses up the shield's glow after it blocks a rock
    /// </summary>
    private void BreakShield()
    {
        sparkles.Burst(knight.ShieldCenter, Color.LightSkyBlue);
        camera.Shake(4, 0.3f);
        hitSound.Play(0.6f, 0.6f, 0);
        knight.ShieldGlowing = false;
    }

    /// <summary>
    /// Smashes a rock into chips and dust and drops it again
    /// </summary>
    /// <param name="boulder">The rock to smash</param>
    private void SmashRock(BoulderSprite boulder)
    {
        debris.Burst(boulder.Position);
        dust.Burst(boulder.Position);
        camera.Shake(1.5f, 0.15f);
        boulder.FallFromSky(camera.ViewLeft, camera.ViewRight);
    }

    /// <summary>
    /// Ends the level, or wins the game after the last one
    /// </summary>
    private void CompleteLevel()
    {
        ZoomInOnKnight();
        MediaPlayer.Volume = MUSIC_DUCKED_VOLUME;
        winSound.Play();

        if (level == LEVEL_FILES.Length - 1)
        {
            gameState = State.Won;
            fireworkTimer = 0;
            SaveData.Delete();
            return;
        }

        gameState = State.LevelComplete;
        level++;
        gainedLife = lives < MAX_LIVES;
        lives = System.Math.Min(lives + 1, MAX_LIVES);
        SaveGame();
    }

    /// <summary>
    /// How many rocks should be falling right now
    /// </summary>
    /// <returns>The number of rocks</returns>
    private int RocksAllowed()
    {
        return System.Math.Min(STARTING_ROCKS + level * ROCKS_PER_LEVEL + villagersRescued * ROCKS_PER_VILLAGER,
                               MAX_ROCKS);
    }

    /// <summary>
    /// Finds the volcano nearest the middle of the screen
    /// </summary>
    /// <returns>Its mouth, in background coordinates</returns>
    private Vector2 NearestVolcanoMouth()
    {
        // middle of the screen in background coordinates
        float middle = camera.Position.X * ParallaxBackground.PARALLAX + WINDOW_WIDTH / 2f;

        Vector2 nearest = ParallaxBackground.VolcanoMouths[0];
        foreach (var mouth in ParallaxBackground.VolcanoMouths)
            if (System.Math.Abs(mouth.X - middle) < System.Math.Abs(nearest.X - middle)) nearest = mouth;
        return nearest;
    }

    /// <summary>
    /// Zooms the camera in on the knight
    /// </summary>
    private void ZoomInOnKnight()
    {
        camera.ZoomFocus = knight.Bounds.Center - camera.Position;
        camera.TargetZoom = ENDING_ZOOM;
    }

    /// <summary>
    /// Sets off fireworks while the win is showing
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
        sparkles.Burst(where, colors[random.Next(colors.Length)]);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // world first, then the menus and text on top
        if (gameState is State.Title or State.Playing or State.GameOver or State.Won
                      or State.LevelComplete or State.Hit)
            DrawScene(gameTime);

        _spriteBatch.Begin();

        switch (gameState)
        {
            case State.Title:
                DrawCenteredText(medievalSharp, titleText, 30, Color.Gold);
                DrawButton(continueButton);
                DrawButton(newGameButton);
                DrawButton(settingsButton);
                DrawButton(quitButton);
                break;

            case State.Settings:
                DrawCenteredText(medievalSharp, "Settings", 60, Color.Gold);
                DrawButton(controlsButton);
                DrawButton(howToPlayButton);
                DrawButton(backButton);
                break;

            case State.Controls:
                DrawCenteredText(medievalSharp, "Controls", 30, Color.Gold);
                DrawControls();
                DrawButton(backButton);
                break;

            case State.HowToPlay:
                DrawCenteredText(medievalSharp, "How to Play", 30, Color.Gold);
                DrawHowToPlay();
                DrawButton(backButton);
                break;

            case State.Story:
                DrawCenteredText(medievalSharpSmall, $"Level {level + 1} of {LEVEL_FILES.Length}", 70, Color.Gold);
                DrawCenteredText(medievalSharp, story[level, 0], 110, Color.Gold);
                DrawCenteredText(medievalSharpSmall, story[level, 1], 220, Color.White);
                DrawCenteredText(medievalSharpSmall, $"Lives: {lives}", 370, Color.White);
                DrawButton(beginButton);
                break;

            case State.Playing:
                if (hasMoved)
                    DrawCenteredText(medievalSharp,
                        $"Level {level + 1}   Villagers: {villagersRescued} / {VILLAGER_GOAL}   Lives: {lives}",
                        20, Color.Gold);
                else
                    DrawCenteredText(medievalSharp, goalText, 20, Color.White);
                DrawMinimap();
                DrawVillagerPointer(gameTime);
                break;

            case State.LevelComplete:
                DrawOverlay(Color.White * 0.5f);
                DrawCenteredText(medievalSharp, "Level Complete!", 170, Color.Gold);
                if (gainedLife)
                    DrawCenteredText(medievalSharpSmall, "You earned an extra life!", 228, Color.White);
                DrawButton(nextLevelButton);
                DrawButton(mainMenuButton);
                break;

            case State.Hit:
                DrawOverlay(Color.Black * 0.6f);
                DrawCenteredText(medievalSharp, "You were hit!", 170, Color.OrangeRed);
                DrawCenteredText(medievalSharpSmall, $"Lives left: {lives}", 228, Color.White);
                DrawButton(tryAgainButton);
                DrawButton(mainMenuButton);
                break;

            case State.GameOver:
                DrawOverlay(Color.Black * 0.6f);
                DrawCenteredText(medievalSharp, gameOverText, 170, Color.OrangeRed);
                DrawButton(playAgainButton);
                DrawButton(mainMenuButton);
                break;

            case State.Won:
                DrawOverlay(Color.White * 0.5f);
                DrawCenteredText(medievalSharp, winText, 170, Color.Gold);
                DrawCenteredText(medievalSharpSmall, endingText, 228, Color.White);
                DrawButton(playAgainButton);
                DrawButton(mainMenuButton);
                break;
        }

        DrawCenteredText(medievalSharpSmall, exitText, WINDOW_HEIGHT - medievalSharpSmall.LineSpacing - 12, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a button
    /// </summary>
    /// <param name="button">The button to draw</param>
    private void DrawButton(Button button)
    {
        button.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
    }

    /// <summary>
    /// Draws the world back to front through the camera
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void DrawScene(GameTime gameTime)
    {
        bool inPlay = gameState != State.Title;

        // Background layer
        _spriteBatch.Begin(transformMatrix: camera.GetTransform(ParallaxBackground.PARALLAX));
        background.Draw(_spriteBatch);
        if (inPlay)
            foreach (var eruption in eruptions) eruption.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        embers.Draw(gameTime);

        // pixel art layer: tilemap and villager
        _spriteBatch.Begin(transformMatrix: camera.GetTransform(), samplerState: SamplerState.PointClamp);
        tilemap.Draw(gameTime, _spriteBatch);
        if (inPlay) villager.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        // 3D shield power-up, behind the knight
        if (inPlay && shield.Waiting)
        {
            Graphics3D.Begin(GraphicsDevice);
            shield.Draw(camera.GetTransform(), Graphics3D.Projection(GraphicsDevice.Viewport));
        }

        // glow halo behind the knight
        bool glowing = inPlay && knight.ShieldGlowing;
        if (glowing)
        {
            _spriteBatch.Begin(transformMatrix: camera.GetTransform(), blendState: BlendState.Additive);
            knight.DrawShieldHalo(gameTime, _spriteBatch);
            _spriteBatch.End();
        }

        // World layer
        _spriteBatch.Begin(transformMatrix: camera.GetTransform());
        if (inPlay)
            foreach (var boulder in boulders) boulder.Draw(gameTime, _spriteBatch);
        knight.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        // glow shine over the shield
        if (glowing)
        {
            _spriteBatch.Begin(transformMatrix: camera.GetTransform(), blendState: BlendState.Additive);
            knight.DrawShieldShine(gameTime, _spriteBatch);
            _spriteBatch.End();
        }

        dust.Draw(gameTime);
        debris.Draw(gameTime);
        explosions.Draw(gameTime);
        sparkles.Draw(gameTime);
        ash.Draw(gameTime);
    }

    /// <summary>
    /// Draws the minimap, with marks for the knight, villager and shield power-up
    /// </summary>
    private void DrawMinimap()
    {
        float toMap = (float)MINIMAP.Width / WORLD_WIDTH;

        _spriteBatch.Draw(whitePixel, MINIMAP, Color.Black * 0.5f);

        var view = new Rectangle(MINIMAP.X + (int)(camera.ViewLeft * toMap), MINIMAP.Y,
                                 (int)(WINDOW_WIDTH * toMap), MINIMAP.Height);
        _spriteBatch.Draw(whitePixel, view, Color.White * 0.25f);

        if (shield.Waiting)
        {
            int shieldX = MINIMAP.X + (int)(shield.Position.X * toMap);
            _spriteBatch.Draw(whitePixel, new Rectangle(shieldX - 3, MINIMAP.Y - 3, 6, MINIMAP.Height + 6), Color.LightSkyBlue);
        }

        int villagerX = MINIMAP.X + (int)(villager.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(villagerX - 3, MINIMAP.Y - 3, 6, MINIMAP.Height + 6), Color.Gold);

        int knightX = MINIMAP.X + (int)(knight.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(knightX - 2, MINIMAP.Y - 5, 4, MINIMAP.Height + 10), Color.White);
    }

    /// <summary>
    /// Draws an arrow at the edge of the screen pointing to an off-screen villager
    /// </summary>
    /// <param name="gameTime">The game time</param>
    private void DrawVillagerPointer(GameTime gameTime)
    {
        if (villager.Rescued) return;

        bool left = villager.Position.X < camera.ViewLeft;
        bool right = villager.Position.X > camera.ViewRight;
        if (!left && !right) return;

        string arrow = left ? "<" : ">";
        Vector2 size = medievalSharp.MeasureString(arrow);
        float nudge = 6 * (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * 6);
        float x = left ? 20 + nudge : WINDOW_WIDTH - 20 - size.X - nudge;
        var position = new Vector2(x, villager.Center.Y - size.Y / 2);

        _spriteBatch.DrawString(medievalSharp, arrow, position + new Vector2(3, 3), Color.Black * 0.75f);
        _spriteBatch.DrawString(medievalSharp, arrow, position, Color.Gold);
    }

    /// <summary>
    /// Draws the controls table
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
    /// Draws the how to play screen
    /// </summary>
    private void DrawHowToPlay()
    {
        float y = 100;

        for (int i = 0; i < howToPlay.GetLength(0); i++)
        {
            DrawCenteredText(medievalSharpSmall, howToPlay[i, 0], y, Color.Gold);
            y += medievalSharpSmall.LineSpacing;

            DrawCenteredText(medievalSharpSmall, howToPlay[i, 1], y, Color.White);
            y += howToPlay[i, 1].Split('\n').Length * medievalSharpSmall.LineSpacing + 10;
        }
    }

    /// <summary>
    /// Tints the whole screen
    /// </summary>
    /// <param name="color">The tint color</param>
    private void DrawOverlay(Color color)
    {
        _spriteBatch.Draw(whitePixel, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), color);
    }

    /// <summary>
    /// Draws text centered across the window, with a drop shadow
    /// </summary>
    /// <param name="font">The font to draw the text in</param>
    /// <param name="text">The text, which may be several lines</param>
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
