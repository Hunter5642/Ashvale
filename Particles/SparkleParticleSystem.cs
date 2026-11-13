using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Sparks for rescues and victory fireworks
/// </summary>
public class SparkleParticleSystem : ParticleSystem
{
    /// <summary>
    /// The firework colors
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
    /// The color of the current burst
    /// </summary>
    private Color color;

    public SparkleParticleSystem(Game game, int maxBursts) : base(game, maxBursts * 30) { }

    protected override void InitializeConstants()
    {
        textureFilename = "particle";
        minNumParticles = 22;
        maxNumParticles = 30;

        blendState = BlendState.Additive;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        var velocity = RandomHelper.NextDirection() * RandomHelper.NextFloat(60, 260);

        var lifetime = RandomHelper.NextFloat(0.5f, 1.0f);

        // slow to a stop and droop
        var acceleration = -velocity / lifetime + new Vector2(0, 150);

        var scale = RandomHelper.NextFloat(0.6f, 1.2f);

        p.Initialize(where, velocity, acceleration, color, lifetime: lifetime, scale: scale);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;

        float alpha = 4 * normalizedLifetime * (1 - normalizedLifetime);
        // fade, keeping the color
        particle.Color = new Color(particle.Color, alpha);
    }

    /// <summary>
    /// Sets off a burst of sparks
    /// </summary>
    /// <param name="where">Where to center the burst</param>
    /// <param name="burstColor">Its color</param>
    public void Burst(Vector2 where, Color burstColor)
    {
        color = burstColor;
        AddParticles(where);
    }
}
