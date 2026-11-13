using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale.Particles;

/// <summary>
/// Glowing embers from the volcanoes
/// </summary>
public class EmberParticleSystem : ParticleSystem
{
    private readonly Vector2[] mouths;

    /// <summary>
    /// Constructs the embers
    /// </summary>
    /// <param name="game">The game</param>
    /// <param name="mouths">Each volcano's mouth, in background coordinates</param>
    public EmberParticleSystem(Game game, Vector2[] mouths) : base(game, 600)
    {
        this.mouths = mouths;
    }

    protected override void InitializeConstants()
    {
        textureFilename = "particle";
        minNumParticles = 0;
        maxNumParticles = 1;

        blendState = BlendState.Additive;
        parallax = ParallaxBackground.PARALLAX;
    }

    protected override void InitializeParticle(ref Particle p, Vector2 where)
    {
        // always upward
        var velocity = RandomHelper.RandomDirection(-MathHelper.Pi * 0.85f, -MathHelper.Pi * 0.15f)
                     * RandomHelper.NextFloat(60, 180);
        var acceleration = new Vector2(0, 120);
        var lifetime = RandomHelper.NextFloat(1, 2.2f);
        var scale = RandomHelper.NextFloat(0.3f, 0.7f);

        p.Initialize(where, velocity, acceleration, Color.Yellow, lifetime: lifetime, scale: scale);
    }

    protected override void UpdateParticle(ref Particle particle, float dt)
    {
        base.UpdateParticle(ref particle, dt);

        float normalizedLifetime = particle.TimeSinceStart / particle.Lifetime;

        // yellow to orange to red
        Color color = normalizedLifetime < 0.5f
            ? Color.Lerp(Color.Yellow, Color.OrangeRed, normalizedLifetime * 2)
            : Color.Lerp(Color.OrangeRed, Color.DarkRed, (normalizedLifetime - 0.5f) * 2);
        particle.Color = color * (1 - normalizedLifetime);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        foreach (var mouth in mouths) AddParticles(mouth);
    }

    /// <summary>
    /// Throws a big burst of embers, for an eruption
    /// </summary>
    /// <param name="where">Where, in background coordinates</param>
    public void Burst(Vector2 where)
    {
        for (int i = 0; i < 12; i++) AddParticles(where);
    }
}
