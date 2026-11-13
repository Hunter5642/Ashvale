using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Falling ash that drifts over the screen
/// </summary>
public class AshParticleSystem : ParticleSystem
{
    /// <summary>
    /// How far past the left side of the screen ash can start
    /// </summary>
    private const int SPAWN_MARGIN = 250;

    public AshParticleSystem(Game game) : base(game, 700) { }

    protected override void InitializeConstants()
    {
        textureFilename = "shard";
        minNumParticles = 0;
        maxNumParticles = 1;

        blendState = BlendState.AlphaBlend;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = new Vector2(RandomHelper.NextFloat(15, 45), RandomHelper.NextFloat(45, 85));
        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
        var angularVelocity = RandomHelper.NextFloat(-3, 3);
        var scale = RandomHelper.NextFloat(0.25f, 0.6f);

        // grey shade and transparency
        float shade = RandomHelper.NextFloat(0.25f, 0.8f);
        var color = new Color(shade, shade, shade) * RandomHelper.NextFloat(0.45f, 0.8f);

        p.Initialize(where, velocity, Vector2.Zero, color, lifetime: 15, scale: scale,
                     rotation: rotation, angularVelocity: angularVelocity);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        // sway side to side
        particle.Position.X += MathF.Sin(particle.TimeSinceStart * 2 + particle.Rotation) * 20 * dt;

        // done at the ground
        if (particle.Position.Y > AshvaleGame.GROUND_Y) particle.TimeSinceStart = particle.Lifetime;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // spawn above the screen
        AddParticles(new Rectangle((int)camera.ViewLeft - SPAWN_MARGIN, -20,
                                       AshvaleGame.WINDOW_WIDTH + SPAWN_MARGIN, 10));
    }
}
