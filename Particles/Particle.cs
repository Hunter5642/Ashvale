using Microsoft.Xna.Framework;

namespace Ashvale.Particles;

/// <summary>
/// A single particle in a particle system
/// </summary>
public struct Particle
{
    /// <summary>
    /// The particle's position
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The particle's velocity
    /// </summary>
    public Vector2 Velocity;

    /// <summary>
    /// The particle's acceleration
    /// </summary>
    public Vector2 Acceleration;

    /// <summary>
    /// The particle's rotation
    /// </summary>
    public float Rotation;

    /// <summary>
    /// The particle's angular velocity
    /// </summary>
    public float AngularVelocity;

    /// <summary>
    /// The particle's angular acceleration
    /// </summary>
    public float AngularAcceleration;

    /// <summary>
    /// The particle's scale
    /// </summary>
    public float Scale;

    /// <summary>
    /// How long the particle lives
    /// </summary>
    public float Lifetime;

    /// <summary>
    /// How long this particle has been alive 
    /// </summary>
    public float TimeSinceStart;

    /// <summary>
    /// The particle's color
    /// </summary>
    public Color Color;

    /// <summary>
    /// The color the particle started with, for fading
    /// </summary>
    public Color StartColor;

    /// <summary>
    /// Whether the particle is still alive
    /// </summary>
    public bool Active => TimeSinceStart < Lifetime;



    /// <summary>
    /// Sets the particle up, standing still and white
    /// </summary>
    public void Initialize(Vector2 where, float lifetime = 1, float scale = 1, float rotation = 0, float angularVelocity = 0, float angularAcceleration = 0)
        => Initialize(where, Vector2.Zero, Vector2.Zero, Color.White, lifetime, scale, rotation, angularVelocity, angularAcceleration);

    /// <summary>
    /// Sets the particle up in white
    /// </summary>
    public void Initialize(Vector2 position, Vector2 velocity, Vector2 acceleration, float lifetime = 1, float scale = 1, float rotation = 0, float angularVelocity = 0, float angularAcceleration = 0)
        => Initialize(position, velocity, acceleration, Color.White, lifetime, scale, rotation, angularVelocity, angularAcceleration);

    /// <summary>
    /// Sets the particle up
    /// </summary>
    public void Initialize(Vector2 position, Vector2 velocity, Vector2 acceleration, Color color, float lifetime = 1, float scale = 1, float rotation = 0, float angularVelocity = 0, float angularAcceleration = 0)
    {
        this.Position = position;
        this.Velocity = velocity;
        this.Acceleration = acceleration;
        this.Lifetime = lifetime;
        this.TimeSinceStart = 0f;
        this.Scale = scale;
        this.Rotation = rotation;
        this.AngularVelocity = angularVelocity;
        this.AngularAcceleration = angularAcceleration;
        this.Color = color;
        this.StartColor = this.Color;
    }
}
