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
    /// The background art is 600x300 so the window is an exact 2x of it
    /// </summary>
    public const int WINDOW_WIDTH = 1200;
    public const int WINDOW_HEIGHT = 600;

    /// <summary>
    /// How wide the world the knight can run around in is. It is three screens across, and
    /// the camera scrolls to follow him. Every level's map is 90 tiles of 40 pixels, which
    /// fills it exactly
    /// </summary>
    public const int WORLD_WIDTH = WINDOW_WIDTH * 3;

    /// <summary>
    /// The top of the road, which is the bottom row of tiles in every level's map
    /// </summary>
    public const int GROUND_Y = 560;

    /// <summary>
    /// The map file for each level, in the order they are played
    /// </summary>
    private static readonly string[] LEVEL_FILES = { "level1.txt", "level2.txt", "level3.txt" };

    /// <summary>
    /// How many villagers have to be rescued to clear a level
    /// </summary>
    private const int VILLAGER_GOAL = 5;

    /// <summary>
    /// How many lives the knight starts with, and the most he can build up to. Clearing a level
    /// earns him one more
    /// </summary>
    private const int STARTING_LIVES = 3;
    private const int MAX_LIVES = 5;

    /// <summary>
    /// How many rocks the volcano throws for show at the start, and how they are spaced out
    /// </summary>
    private const int ERUPTION_COUNT = 5;
    private const double ERUPTION_INTERVAL = 0.25;

    /// <summary>
    /// How many rocks fall at once at the start of the first level, how many more each later
    /// level starts with, how many more join for every villager rescued, and the most there
    /// can ever be. Each level starts gentle and gets busier toward the last villager
    /// </summary>
    private const int STARTING_ROCKS = 3;
    private const int ROCKS_PER_LEVEL = 1;
    private const int ROCKS_PER_VILLAGER = 1;
    private const int MAX_ROCKS = 8;

    /// <summary>
    /// How long the knight gets to move before the first rock arrives, and how far apart
    /// the rest follow
    /// </summary>
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
    /// is in it, and where the villager is
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
    private VillagerSprite villager;
    private BoulderSprite[] boulders;
    private EruptionSprite[] eruptions;

    private Camera camera;
    private ParallaxBackground background;

    /// <summary>
    /// The level being played: its road and ledges
    /// </summary>
    private Tilemap tilemap;

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
    private SoundEffect rescueSound;
    private SoundEffect jumpSound;
    private SoundEffect hitSound;
    private SoundEffect eruptionSound;
    private SoundEffect winSound;
    private SoundEffect clickSound;

    /// <summary>
    /// The buttons on the title screen. Continue is greyed out when there is no save file
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
    /// Sits at the bottom of the settings, controls and how to play screens, and goes
    /// back one screen from whichever one is showing
    /// </summary>
    private Button backButton;

    /// <summary>
    /// Starts the level from its story screen
    /// </summary>
    private Button beginButton;

    /// <summary>
    /// The buttons shown when a level ends: Next Level once it is cleared, Try Again after a
    /// hit, Play Again once the game is won or lost, and Main Menu on all of them
    /// </summary>
    private Button nextLevelButton;
    private Button tryAgainButton;
    private Button playAgainButton;
    private Button mainMenuButton;

    /// <summary>
    /// A single white pixel, stretched out to dim or light up the whole screen
    /// </summary>
    private Texture2D whitePixel;

    private string titleText = "Ashvale";
    private string exitText = "Press Esc to exit, M to mute";
    private string goalText = $"Rescue {VILLAGER_GOAL} villagers to clear the level";
    private string gameOverText = "Game Over";
    private string winText = "You Win!";

    /// <summary>
    /// The story shown before each level, as pairs of the level's name and the text under it
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
    /// Shown under the win message once the last level is cleared
    /// </summary>
    private string endingText = "Every villager made it safely behind the castle walls.";

    /// <summary>
    /// Each thing the player can do, and the key or button that does it
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
    /// The how to play screen, as pairs of a heading and the text under it
    /// </summary>
    private string[,] howToPlay =
    {
        { "Objective",
          "Help the knight rescue the villagers stranded at the foot of an erupting volcano." },
        { "What Happens",
          "Each level opens with an eruption, and then rocks rain from the sky.\n" +
          "The villagers wait up on the ledges. Jump up through a ledge to land on it,\n" +
          "press Down to drop back through, and shelter under one when the rocks come." },
        { "Winning and Losing",
          $"Rescue {VILLAGER_GOAL} villagers to clear a level, and clear all {LEVEL_FILES.Length} levels to win.\n" +
          $"A falling rock costs a life. You start with {STARTING_LIVES} and earn 1 more per level.\n" +
          "Your level and lives are saved, so Continue starts your level over." }
    };

    private State gameState = State.Title;

    /// <summary>
    /// Which level is being played, counting from 0, and how many lives the knight has left
    /// </summary>
    private int level;
    private int lives;

    /// <summary>
    /// How many villagers have been rescued so far on this level
    /// </summary>
    private int villagersRescued;

    /// <summary>
    /// Whether clearing the last level earned an extra life, for the level complete screen
    /// </summary>
    private bool gainedLife;

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

        // every rock a level could ever need is made up front, and they join in as villagers are rescued
        boulders = new BoulderSprite[MAX_ROCKS];
        for (int i = 0; i < boulders.Length; i++) boulders[i] = new BoulderSprite(random);

        eruptions = new EruptionSprite[ERUPTION_COUNT];
        for (int i = 0; i < eruptions.Length; i++) eruptions[i] = new EruptionSprite(random);

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

        // the title screen shows the first level's road behind the menu
        LoadLevel(0);
        foreach (var boulder in boulders) boulder.LoadContent(Content);
        foreach (var eruption in eruptions) eruption.LoadContent(Content);

        whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });

        rescueSound = Content.Load<SoundEffect>("coin");
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
                // the knight keeps falling if he was mid-jump when the level ended
                knight.Update(gameTime, tilemap);
                if (Clicked(nextLevelButton))
                {
                    // the next map is only loaded now, so it doesn't swap in behind this screen
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
    /// Goes back to the title screen, with the knight standing where he does there, the
    /// first level's road behind him, and the music back up to full volume
    /// </summary>
    private void ReturnToTitle()
    {
        gameState = State.Title;
        knight.ReturnToTitle(new Vector2(TITLE_KNIGHT_X, TITLE_KNIGHT_Y));
        MediaPlayer.Volume = MUSIC_VOLUME;
        camera.Reset();
        LoadLevel(0);

        // a game that was won or lost on the way here has deleted its save
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
    }

    /// <summary>
    /// Starts a whole new game from the first level, replacing any save file, and shows the
    /// first level's story
    /// </summary>
    private void NewGame()
    {
        level = 0;
        lives = STARTING_LIVES;
        villagersRescued = 0;
        SaveGame();

        LoadLevel(level);
        gameState = State.Story;
    }

    /// <summary>
    /// Picks up a saved game with the lives it had, and shows the story of the level it was on.
    /// The level starts over, so no villagers are rescued yet
    /// </summary>
    private void ContinueGame()
    {
        SaveData save = SaveData.Load();
        if (save == null)
        {
            // the file couldn't be read, so there is nothing to continue
            continueButton.Enabled = false;
            return;
        }

        // keep the numbers in range, in case the file was changed by hand
        level = MathHelper.Clamp(save.Level, 0, LEVEL_FILES.Length - 1);
        lives = MathHelper.Clamp(save.Lives, 1, MAX_LIVES);
        villagersRescued = 0;

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
    /// Puts everything back to its starting state and begins the current level. Trying again
    /// after a hit and continuing a saved game come through here too
    /// </summary>
    private void StartLevel()
    {
        gameState = State.Playing;

        // the music comes back up if the last level ended with it turned down
        MediaPlayer.Volume = MUSIC_VOLUME;

        // the knight stops drifting, shrinks to playing size, and stands at the bottom left
        knight.Bobbing = false;
        knight.Scale = PLAY_SCALE;
        knight.FacingRight = true;
        knight.PlaceOnGround(KNIGHT_START_X);

        // the first villager of a level is always off to the right, on the road, so the opening
        // move is a run for them
        villager.MoveToFirstSpot();

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

            // Down drops him through the ledge he is standing on. Like jumping, it takes a fresh
            // press, so holding it down doesn't drop him through every ledge below in a row
            bool downPressed = currentKeyboardState.IsKeyDown(Keys.Down) || currentKeyboardState.IsKeyDown(Keys.S);
            bool downWasPressed = priorKeyboardState.IsKeyDown(Keys.Down) || priorKeyboardState.IsKeyDown(Keys.S);
            if (downPressed && !downWasPressed)
            {
                knight.DropThrough();
                hasMoved = true;
            }

            camera.Follow(knight.Position.X, gameTime);
        }

        // After a second of free movement, rocks begin falling, one every DROP_INTERVAL, until
        // as many are falling as the villagers rescued so far allow. Each one recycles itself
        // from then on, so this only has to start them off
        if (playTimer > DROP_DELAY && droppedRocks < RocksAllowed())
        {
            dropTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (droppedRocks == 0 || dropTimer > DROP_INTERVAL)
            {
                boulders[droppedRocks].FallFromSky(camera.ViewLeft, camera.ViewRight);

                // a rock that joins because of a rescue gets announced with a rumble from the
                // volcano, so the player can tell the game just got harder
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

        // he can land on a ledge now, so the dust goes wherever his feet are
        if (knight.JustLanded) dust.Burst(new Vector2(knight.Position.X, knight.Bounds.Bottom));

        // A rock that reaches a ledge or the road smashes into chips and dust and gives the
        // screen a jolt, then goes back up to fall again over wherever the camera is now
        foreach (var boulder in boulders)
        {
            if (!boulder.Landed) continue;

            var impact = boulder.Position;
            debris.Burst(impact);
            dust.Burst(impact);
            camera.Shake(1.5f, 0.15f);
            boulder.FallFromSky(camera.ViewLeft, camera.ViewRight);
        }

        // Reaching the villager sends them off and brings the next one out, until enough of
        // them are safe to clear the level
        if (knight.Bounds.CollidesWith(villager.Bounds))
        {
            villagersRescued++;
            sparkles.Burst(villager.Center, Color.Gold);
            if (villagersRescued >= VILLAGER_GOAL)
            {
                // the last villager is rescued rather than moved, so they vanish as the level ends
                villager.Rescued = true;
                CompleteLevel();
                return;
            }
            rescueSound.Play();
            villager.MoveToRandomSpot(knight.Bounds, tilemap);
        }

        // Any falling rock costs a life. The eruption rocks are scenery and never collide
        foreach (var boulder in boulders)
        {
            if (boulder.Active && knight.Bounds.CollidesWith(boulder.Bounds))
            {
                MediaPlayer.Volume = MUSIC_DUCKED_VOLUME;
                hitSound.Play();

                Vector2 knightCenter = KnightCenter();
                explosions.PlaceExplosion(knightCenter);
                debris.Burst(knightCenter);
                camera.Shake(6, 0.4f);
                ZoomInOnKnight();

                // Trying again starts the level over, so its rescues are lost. With no lives
                // left the game is over, and there is nothing to continue
                lives--;
                villagersRescued = 0;
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
    /// Ends the level once its last villager is rescued. Clearing the last level wins the game,
    /// and any other level earns an extra life and saves the game at the start of the next one
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
        villagersRescued = 0;
        SaveGame();
    }

    /// <summary>
    /// How many rocks should be falling, given the level and how many villagers have been
    /// rescued on it
    /// </summary>
    /// <returns>The number of rocks allowed in the air</returns>
    private int RocksAllowed()
    {
        return System.Math.Min(STARTING_ROCKS + level * ROCKS_PER_LEVEL + villagersRescued * ROCKS_PER_VILLAGER,
                               MAX_ROCKS);
    }

    /// <summary>
    /// Finds the volcano closest to the middle of the screen, so the rumble comes from one
    /// the player can see
    /// </summary>
    /// <returns>That volcano's mouth, in background coordinates</returns>
    private Vector2 NearestVolcanoMouth()
    {
        // the background scrolls at half speed, so the middle of the screen is somewhere
        // else in background coordinates than it is in the world
        float middle = camera.Position.X * ParallaxBackground.PARALLAX + WINDOW_WIDTH / 2f;

        Vector2 nearest = ParallaxBackground.VolcanoMouths[0];
        foreach (var mouth in ParallaxBackground.VolcanoMouths)
            if (System.Math.Abs(mouth.X - middle) < System.Math.Abs(nearest.X - middle)) nearest = mouth;
        return nearest;
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
        if (gameState is State.Title or State.Playing or State.GameOver or State.Won
                      or State.LevelComplete or State.Hit)
            DrawScene(gameTime);

        _spriteBatch.Begin();

        switch (gameState)
        {
            case State.Title:
                DrawCenteredText(medievalSharp, titleText, 30, Color.Gold);
                continueButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                newGameButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
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

            case State.Story:
                DrawCenteredText(medievalSharpSmall, $"Level {level + 1} of {LEVEL_FILES.Length}", 70, Color.Gold);
                DrawCenteredText(medievalSharp, story[level, 0], 110, Color.Gold);
                DrawCenteredText(medievalSharpSmall, story[level, 1], 220, Color.White);
                DrawCenteredText(medievalSharpSmall, $"Lives: {lives}", 370, Color.White);
                beginButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
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
                nextLevelButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                mainMenuButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                break;

            case State.Hit:
                DrawOverlay(Color.Black * 0.6f);
                DrawCenteredText(medievalSharp, "You were hit!", 170, Color.OrangeRed);
                DrawCenteredText(medievalSharpSmall, $"Lives left: {lives}", 228, Color.White);
                tryAgainButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
                mainMenuButton.Draw(_spriteBatch, medievalSharp, whitePixel, currentMouseState.Position);
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
                DrawCenteredText(medievalSharpSmall, endingText, 228, Color.White);
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
    /// full speed with the tilemap, villager, rocks and knight, and the effects that happen
    /// around him go over the top
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

        // Pixel art layer: the tilemap and the villager, through the same camera as the world.
        // PointClamp keeps the pixel art sharp, and stops lines showing between tiles when the
        // camera lands between pixels. It gets its own batch because it would make the knight,
        // who is big artwork shrunk down, look jagged
        _spriteBatch.Begin(transformMatrix: camera.GetTransform(), samplerState: SamplerState.PointClamp);
        tilemap.Draw(gameTime, _spriteBatch);
        if (gameState != State.Title) villager.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        // World layer
        _spriteBatch.Begin(transformMatrix: camera.GetTransform());
        if (gameState != State.Title)
            foreach (var boulder in boulders) boulder.Draw(gameTime, _spriteBatch);
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
    /// white mark for the knight and a gold one for the villager
    /// </summary>
    private void DrawMinimap()
    {
        float toMap = (float)MINIMAP.Width / WORLD_WIDTH;

        _spriteBatch.Draw(whitePixel, MINIMAP, Color.Black * 0.5f);

        var view = new Rectangle(MINIMAP.X + (int)(camera.ViewLeft * toMap), MINIMAP.Y,
                                 (int)(WINDOW_WIDTH * toMap), MINIMAP.Height);
        _spriteBatch.Draw(whitePixel, view, Color.White * 0.25f);

        int villagerX = MINIMAP.X + (int)(villager.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(villagerX - 3, MINIMAP.Y - 3, 6, MINIMAP.Height + 6), Color.Gold);

        int knightX = MINIMAP.X + (int)(knight.Position.X * toMap);
        _spriteBatch.Draw(whitePixel, new Rectangle(knightX - 2, MINIMAP.Y - 5, 4, MINIMAP.Height + 10), Color.White);
    }

    /// <summary>
    /// When the villager is off the side of the screen, draws a pulsing arrow at that edge
    /// pointing the way to them
    /// </summary>
    /// <param name="gameTime">The game time, which makes the arrow pulse</param>
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
