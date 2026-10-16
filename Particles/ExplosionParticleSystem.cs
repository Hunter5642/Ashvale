using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// A fiery blast, from the particle system tutorial, for the moment a rock hits the knight.
/// The blobs are recolored from a white-hot core out to orange, and use additive blending
/// so the middle of the blast, where they overlap most, burns brightest.
/// </summary>
public class ExplosionParticleSystem : ParticleSystem
{
    public ExplosionParticleSystem(Game game, int maxExplosions) : base(game, maxExplosions * 25) { }

    protected override void InitializeConstants()
    {
        textureFilename = "explosion";
        minNumParticles = 20;
        maxNumParticles = 25;

        blendState = BlendState.Additive;
        DrawOrder = AdditiveBlendDrawOrder;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = RandomHelper.NextDirection() * RandomHelper.NextFloat(40, 200);

        var lifetime = RandomHelper.NextFloat(0.5f, 1.0f);

        var acceleration = -velocity / lifetime;

        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);

        var angularVelocity = RandomHelper.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);

        p.Initialize(where, velocity, acceleration, lifetime: lifetime, rotation: rotation, angularVelocity: angularVelocity);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;

        float alpha = 4 * normalizedLifetime * (1 - normalizedLifetime);
        particle.Color = Color.Lerp(Color.White, Color.Orange, normalizedLifetime) * alpha;

        particle.Scale = .1f + .25f * normalizedLifetime;
    }

    public void PlaceExplosion(Vector2 where) => AddParticles(where);
}
