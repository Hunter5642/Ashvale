using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Clouds of dust thrown up along the ground when a boulder lands, or when the knight lands
/// from a jump. The puffs are translucent and overlap, blending into one soft cloud.
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
        DrawOrder = AlphaBlendDrawOrder;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        // roll out along the ground in both directions, lifting a little
        float side = RandomHelper.Next(2) == 0 ? -1 : 1;
        var velocity = new Vector2(side * RandomHelper.NextFloat(40, 160), RandomHelper.NextFloat(-50, -10));
        var lifetime = RandomHelper.NextFloat(0.6f, 1.1f);
        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
        var angularVelocity = RandomHelper.NextFloat(-1.5f, 1.5f);

        // the dust slows down as it spreads, coming to a stop right as it vanishes
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
    /// <param name="where">The spot on the ground, in world coordinates</param>
    public void Burst(Vector2 where) => AddParticles(where);
}
