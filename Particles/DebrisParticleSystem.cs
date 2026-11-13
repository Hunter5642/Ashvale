using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Rock chips that fly out when a rock smashes
/// </summary>
public class DebrisParticleSystem : ParticleSystem
{
    /// <summary>
    /// The chip colors
    /// </summary>
    private static readonly Color[] colors =
    {
        new Color(90, 70, 60),
        new Color(60, 50, 50),
        new Color(120, 95, 80),
        new Color(160, 60, 30)
    };

    private const float GRAVITY = 900;

    /// <summary>
    /// The level, for landing chips
    /// </summary>
    public Tilemap Tilemap { get; set; }

    public DebrisParticleSystem(Game game, int maxBursts) : base(game, maxBursts * 18) { }

    protected override void InitializeConstants()
    {
        textureFilename = "shard";
        minNumParticles = 12;
        maxNumParticles = 18;

        blendState = BlendState.AlphaBlend;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = RandomHelper.RandomDirection(-MathHelper.Pi * 0.95f, -MathHelper.Pi * 0.05f)
                     * RandomHelper.NextFloat(150, 420);
        var lifetime = RandomHelper.NextFloat(0.8f, 1.6f);
        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
        var angularVelocity = RandomHelper.NextFloat(-12, 12);
        var scale = RandomHelper.NextFloat(0.5f, 1.3f);
        var color = colors[RandomHelper.Next(colors.Length)];

        p.Initialize(where, velocity, new Vector2(0, GRAVITY), color, lifetime: lifetime,
                     scale: scale, rotation: rotation, angularVelocity: angularVelocity);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        float oldY = particle.Position.Y;
        base.UpdateParticle(ref particle, dt);

        // land on the road or a ledge
        if (Tilemap.TryLand(particle.Position.X, particle.Position.X, oldY, particle.Position.Y, out float top))
        {
            particle.Position.Y = top;
            particle.Velocity = Vector2.Zero;
            particle.Acceleration = Vector2.Zero;
            particle.AngularVelocity = 0;
        }

        // fade over the last third of its life
        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;
        float alpha = MathHelper.Clamp((1 - normalizedLifetime) * 3, 0, 1);
        particle.Color = particle.StartColor * alpha;
    }

    /// <summary>
    /// Sends out a burst of chips
    /// </summary>
    /// <param name="where">Where the rock hit</param>
    public void Burst(Vector2 where) => AddParticles(where);
}
