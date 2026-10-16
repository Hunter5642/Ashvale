using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Flakes of ash that drift down over wherever the camera is looking, spinning and swaying
/// as they fall. Each flake is tinted its own shade of grey and is partly see-through, so
/// they blend over each other and over the world behind them.
/// </summary>
public class AshParticleSystem : ParticleSystem
{
    /// <summary>
    /// How far past each side of the screen flakes can start, so the wind doesn't leave a
    /// bare strip down the left edge
    /// </summary>
    private const int SPAWN_MARGIN = 250;

    public AshParticleSystem(Game game) : base(game, 700) { }

    protected override void InitializeConstants()
    {
        textureFilename = "shard";
        minNumParticles = 0;
        maxNumParticles = 2;

        blendState = BlendState.AlphaBlend;
        DrawOrder = AlphaBlendDrawOrder;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = new Vector2(RandomHelper.NextFloat(15, 45), RandomHelper.NextFloat(45, 85));
        var rotation = RandomHelper.NextFloat(0, MathHelper.TwoPi);
        var angularVelocity = RandomHelper.NextFloat(-3, 3);
        var scale = RandomHelper.NextFloat(0.25f, 0.6f);

        // anything from pale grey to near black, never fully solid
        float shade = RandomHelper.NextFloat(0.25f, 0.8f);
        var color = new Color(shade, shade, shade) * RandomHelper.NextFloat(0.45f, 0.8f);

        p.Initialize(where, velocity, Vector2.Zero, color, lifetime: 15, scale: scale,
                     rotation: rotation, angularVelocity: angularVelocity);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        // sway side to side as it falls, each flake out of step with the rest
        particle.Position.X += MathF.Sin(particle.TimeSinceStart * 2 + particle.Rotation) * 20 * dt;

        // once it reaches the ground it is done
        if (particle.Position.Y > AshvaleGame.GROUND_Y) particle.TimeSinceStart = particle.Lifetime;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // a thin strip just above the top of the screen, following the camera
        if (Emitting)
            AddParticles(new Rectangle((int)camera.ViewLeft - SPAWN_MARGIN, -20,
                                       AshvaleGame.WINDOW_WIDTH + SPAWN_MARGIN, 10));
    }
}
