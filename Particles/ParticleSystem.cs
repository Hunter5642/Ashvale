using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Ashvale.Particles;

/// <summary>
/// A generic particle system
/// </summary>
public abstract class ParticleSystem : DrawableGameComponent
{
    #region static fields

    /// <summary>
    /// A SpriteBatch shared by the particle systems
    /// </summary>
    protected static SpriteBatch spriteBatch;

    /// <summary>
    /// A ContentManager shared by the particle systems
    /// </summary>
    protected static ContentManager contentManager;

    #endregion

    #region private fields

    /// <summary>
    /// The collection of particles 
    /// </summary>
    Particle[] particles;

    /// <summary>
    /// Indices of unused particles
    /// </summary>
    Queue<int> freeParticles;

    /// <summary>
    /// The particle texture
    /// </summary>
    Texture2D texture;

    /// <summary>
    /// The middle of the texture
    /// </summary>
    Vector2 origin;

    #endregion

    #region protected fields

    /// <summary>
    /// The blend state
    /// </summary>
    protected BlendState blendState = BlendState.AlphaBlend;

    /// <summary>
    /// The particle texture's filename
    /// </summary>
    protected string textureFilename;

    /// <summary>
    /// The fewest particles AddParticles() adds
    /// </summary>
    protected int minNumParticles;

    /// <summary>
    /// The most particles AddParticles() adds
    /// </summary>
    protected int maxNumParticles;

    /// <summary>
    /// How fast this system scrolls compared to the world
    /// </summary>
    protected float parallax = 1;

    /// <summary>
    /// The game's camera
    /// </summary>
    protected Camera camera;

    #endregion

    /// <summary>
    /// Constructs a particle system
    /// </summary>
    /// <param name="game">The game</param>
    public ParticleSystem(Game game, int maxParticles) : base(game) 
    {
        particles = new Particle[maxParticles];
        freeParticles = new Queue<int>(maxParticles);
        for (int i = 0; i < particles.Length; i++)
        {
            freeParticles.Enqueue(i);
        }
        InitializeConstants();
    }

    #region virtual hook methods 

    /// <summary>
    /// Sets textureFilename, minNumParticles and maxNumParticles
    /// </summary>
    protected abstract void InitializeConstants();

    /// <summary>
    /// Sets up a new particle. Overridden by each system
    /// </summary>
    /// <param name="p">The particle</param>
    /// <param name="where">Where it starts</param>
    protected virtual void InitializeParticle(ref Particle p, Vector2 where)
    {
        p.Initialize(where);
    }

    /// <summary>
    /// Moves a particle. Can be overridden
    /// </summary>
    /// <param name="particle">The particle to update</param>
    /// <param name="dt">The elapsed time</param>
    protected virtual void UpdateParticle(ref Particle particle, float dt)
    {
        // linear motion
        particle.Velocity += particle.Acceleration * dt;
        particle.Position += particle.Velocity * dt;

        // angular motion
        particle.AngularVelocity += particle.AngularAcceleration * dt;
        particle.Rotation += particle.AngularVelocity * dt;

        // age
        particle.TimeSinceStart += dt;
    }

    #endregion

    #region override methods 

    /// <summary>
    /// Loads the particle texture
    /// </summary>
    /// <throws>A InvalidOperationException if the texture filename is not provided</throws>
    protected override void LoadContent()
    {
        // shared ContentManager and SpriteBatch
        if (contentManager == null) contentManager = new ContentManager(Game.Services, "Content");
        if (spriteBatch == null) spriteBatch = new SpriteBatch(Game.GraphicsDevice);

        camera = Game.Services.GetService<Camera>();

        // textureFilename has to be set
        if (string.IsNullOrEmpty(textureFilename))
        {
            string message = "textureFilename wasn't set properly, so the " +
                "particle system doesn't know what texture to load. Make " +
                "sure your particle system's InitializeConstants function " +
                "properly sets textureFilename.";
            throw new InvalidOperationException(message);
        }
        texture = contentManager.Load<Texture2D>(textureFilename);

        // rotate and scale around the middle
        origin.X = texture.Width / 2;
        origin.Y = texture.Height / 2;

        base.LoadContent();
    }

    /// <summary>
    /// Updates the active particles
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for(int i = 0; i < particles.Length; i++)
        {

            if (particles[i].Active)
            {
                UpdateParticle(ref particles[i], dt);
                // free finished particles
                if (!particles[i].Active)
                {
                    freeParticles.Enqueue(i);
                }
            }
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the active particles
    /// </summary>
    public override void Draw(GameTime gameTime)
    {
        spriteBatch.Begin(blendState: blendState, transformMatrix: camera.GetTransform(parallax));

        foreach (Particle p in particles)
        {
            if (!p.Active)
                continue;
            
            spriteBatch.Draw(texture, p.Position, null, p.Color,
                p.Rotation, origin, p.Scale, SpriteEffects.None, 0.0f);
        }

        spriteBatch.End();

        base.Draw(gameTime);
    }

    #endregion

    #region AddParticles methods 

    /// <summary>
    /// Adds a burst of particles at a point, as many as are free
    /// </summary>
    /// <param name="where">Where the burst starts</param>
    protected void AddParticles(Vector2 where)
    {
        int numParticles =
            RandomHelper.Next(minNumParticles, maxNumParticles + 1);

        for (int i = 0; i < numParticles && freeParticles.Count > 0; i++)
        {
            int index = freeParticles.Dequeue();
            InitializeParticle(ref particles[index], where);
        }
    }

    /// <summary>
    /// Adds particles at random points in a rectangle, as many as are free
    /// </summary>
    /// <param name="where">The area to add them in</param>
    protected void AddParticles(Rectangle where)
    {
        int numParticles =
            RandomHelper.Next(minNumParticles, maxNumParticles + 1);

        for (int i = 0; i < numParticles && freeParticles.Count > 0; i++)
        {
            int index = freeParticles.Dequeue();
            InitializeParticle(ref particles[index], RandomHelper.RandomPosition(where));
        }
    }

    #endregion

}
