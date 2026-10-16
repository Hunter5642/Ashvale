using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Chips of rock that fly out when a boulder smashes into the ground. They tumble through
/// the air, come to rest on the ground, and fade away there.
/// </summary>
public class DebrisParticleSystem : ParticleSystem
{
    /// <summary>
    /// The rock colors to tint each chip, so a burst isn't all one flat color
    /// </summary>
    private static readonly Color[] colors =
    {
        new Color(90, 70, 60),
        new Color(60, 50, 50),
        new Color(120, 95, 80),
        new Color(160, 60, 30)
    };

    private const float GRAVITY = 900;

    public DebrisParticleSystem(Game game, int maxBursts) : base(game, maxBursts * 18) { }

    protected override void InitializeConstants()
    {
        textureFilename = "shard";
        minNumParticles = 12;
        maxNumParticles = 18;

        blendState = BlendState.AlphaBlend;
        DrawOrder = AlphaBlendDrawOrder;
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
        base.UpdateParticle(ref particle, dt);

        // landing: stop dead on the ground and stop spinning
        if (particle.Position.Y > AshvaleGame.GROUND_Y)
        {
            particle.Position.Y = AshvaleGame.GROUND_Y;
            particle.Velocity = Vector2.Zero;
            particle.Acceleration = Vector2.Zero;
            particle.AngularVelocity = 0;
        }

        // only fade over the last third of its life, so the burst reads clearly first
        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;
        float alpha = MathHelper.Clamp((1 - normalizedLifetime) * 3, 0, 1);
        particle.Color = particle.StartColor * alpha;
    }

    /// <summary>
    /// Sends out a burst of chips from where a rock hit
    /// </summary>
    /// <param name="where">Where the rock hit, in world coordinates</param>
    public void Burst(Vector2 where) => AddParticles(where);
}
