using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Dust kicked up when a rock or the knight lands
/// </summary>
public class DustParticleSystem : ParticleSystem
{
    private static readonly Color DUST = new Color(150, 125, 100);

    public DustParticleSystem(Game game, int maxBursts) : base(game, maxBursts * 10) { }

    protected override void InitializeConstants()
    {
        textureFilename = "smoke";
        minNumParticles = 6;
        maxNumParticles = 10;

        blendState = BlendState.AlphaBlend;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        // spread both ways along the ground
        float side = RandomHelper.Next(2) == 0 ? -1 : 1;
        var velocity = new Vector2(side * RandomHelper.NextFloat(40, 160), RandomHelper.NextFloat(-50, -10));
        var lifetime = RandomHelper.NextFloat(0.6f, 1.1f);
        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
        var angularVelocity = RandomHelper.NextFloat(-1.5f, 1.5f);

        // slow to a stop as it fades
        p.Initialize(where, velocity, -velocity / lifetime, DUST, lifetime: lifetime,
                     rotation: rotation, angularVelocity: angularVelocity);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;
        particle.Scale = 0.4f + 0.9f * normalizedLifetime;
        particle.Color = DUST * (0.6f * (1 - normalizedLifetime));
    }

    /// <summary>
    /// Kicks up a cloud of dust
    /// </summary>
    /// <param name="where">Where on the ground</param>
    public void Burst(Vector2 where) => AddParticles(where);
}
