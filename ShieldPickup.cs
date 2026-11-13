using System;
using Microsoft.Xna.Framework;
using Ashvale.Collisions;

namespace Ashvale;

/// <summary>
/// The shield power-up, a spinning 3D shield over a ledge
/// </summary>
public class ShieldPickup
{
    /// <summary>
    /// The shield's size in pixels
    /// </summary>
    private const float SIZE = 56;

    /// <summary>
    /// How far above the ledge it floats
    /// </summary>
    private const float HOVER_HEIGHT = 55;

    /// <summary>
    /// How far and how fast it bobs
    /// </summary>
    private const float BOB_HEIGHT = 6;
    private const float BOB_SPEED = 3;

    /// <summary>
    /// How fast it spins
    /// </summary>
    private const float SPIN_SPEED = 2.2f;

    /// <summary>
    /// The pickup radius
    /// </summary>
    private const float PICKUP_RADIUS = 28;

    /// <summary>
    /// How far it appears from the knight and the villager
    /// </summary>
    private const float MIN_KNIGHT_DISTANCE = 300;
    private const float MIN_VILLAGER_DISTANCE = 200;

    /// <summary>
    /// How many spots to try before settling for the last one
    /// </summary>
    private const int MAX_ATTEMPTS = 50;

    /// <summary>
    /// The game's random number generator
    /// </summary>
    private readonly MathHelper.Random random;

    private ShieldModel model;

    /// <summary>
    /// The middle of the shield
    /// </summary>
    private Vector2 position;

    /// <summary>
    /// Time since the shield appeared
    /// </summary>
    private float timer;

    /// <summary>
    /// Whether the shield is waiting to be picked up
    /// </summary>
    public bool Waiting { get; private set; }

    /// <summary>
    /// The middle of the shield
    /// </summary>
    public Vector2 Position => position;

    /// <summary>
    /// The pickup bounds
    /// </summary>
    public BoundingCircle Bounds => new BoundingCircle(position, PICKUP_RADIUS);

    /// <summary>
    /// Constructs the power-up
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public ShieldPickup(MathHelper.Random random)
    {
        this.random = random;
    }

    /// <summary>
    /// Sets the shield's 3D model
    /// </summary>
    /// <param name="model">The shield model</param>
    public void SetModel(ShieldModel model)
    {
        this.model = model;
    }

    /// <summary>
    /// Hides the shield
    /// </summary>
    public void Reset()
    {
        Waiting = false;
    }

    /// <summary>
    /// Puts the shield over a random ledge, away from the knight and the villager
    /// </summary>
    /// <param name="knight">The knight's bounding box</param>
    /// <param name="villager">Where the villager is standing</param>
    /// <param name="tilemap">The level</param>
    public void Appear(BoundingRectangle knight, Vector2 villager, Tilemap tilemap)
    {
        var ledges = tilemap.FindLedgeSpots();
        Vector2 spot = ledges[0];
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            spot = ledges[random.Next(ledges.Count)];
            if (Math.Abs(spot.X - knight.Center.X) >= MIN_KNIGHT_DISTANCE
                && Math.Abs(spot.X - villager.X) >= MIN_VILLAGER_DISTANCE) break;
        }

        position = spot - new Vector2(0, HOVER_HEIGHT);
        timer = 0;
        Waiting = true;
    }

    /// <summary>
    /// Picks the shield up
    /// </summary>
    public void PickUp()
    {
        Waiting = false;
    }

    /// <summary>
    /// Spins and bobs the shield
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    /// <summary>
    /// Draws the shield
    /// </summary>
    /// <param name="view">The camera's transform</param>
    /// <param name="projection">The projection from Graphics3D.Projection()</param>
    public void Draw(Matrix view, Matrix projection)
    {
        if (!Waiting) return;

        float bob = MathF.Sin(timer * BOB_SPEED) * BOB_HEIGHT;
        Matrix world = Matrix.CreateScale(SIZE)
                     * Matrix.CreateRotationY(timer * SPIN_SPEED)
                     * Matrix.CreateTranslation(position.X, position.Y + bob, 0);

        model.Draw(world, view, projection);
    }
}
