using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// A burst of glittering sparks, built on the firework from the particle system tutorial.
/// Collecting a coin gives off a gold burst, and winning sets off fireworks in every color.
/// </summary>
public class SparkleParticleSystem : ParticleSystem
{
    /// <summary>
    /// The colors the victory fireworks pick from
    /// </summary>
    public static readonly Color[] FireworkColors =
    {
        Color.Gold,
        Color.OrangeRed,
        Color.Crimson,
        Color.Aqua,
        Color.HotPink,
        Color.LimeGreen
    };

    /// <summary>
    /// The color of the burst being added right now
    /// </summary>
    private Color color;

    public SparkleParticleSystem(Game game, int maxBursts) : base(game, maxBursts * 30) { }

    protected override void InitializeConstants()
    {
        textureFilename = "particle";
        minNumParticles = 22;
        maxNumParticles = 30;

        blendState = BlendState.Additive;
        DrawOrder = AdditiveBlendDrawOrder;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = RandomHelper.NextDirection() * RandomHelper.NextFloat(60, 260);

        var lifetime = RandomHelper.NextFloat(0.5f, 1.0f);

        // slow to a stop as they fade, with a little gravity so they droop like a firework
        var acceleration = -velocity / lifetime + new Vector2(0, 150);

        var scale = RandomHelper.NextFloat(0.6f, 1.2f);

        p.Initialize(where, velocity, acceleration, color, lifetime: lifetime, scale: scale);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;

        float alpha = 4 * normalizedLifetime * (1 - normalizedLifetime);
        // Keep this particle's color and only fade its alpha
        particle.Color = new Color(particle.Color, alpha);
    }

    /// <summary>
    /// Sets off a burst of sparks
    /// </summary>
    /// <param name="where">Where to center the burst, in world coordinates</param>
    /// <param name="burstColor">What color to make it</param>
    public void Burst(Vector2 where, Color burstColor)
    {
        color = burstColor;
        AddParticles(where);
    }
}
