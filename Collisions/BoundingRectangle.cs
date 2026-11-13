using Microsoft.Xna.Framework;

namespace Ashvale.Collisions;

/// <summary>
/// A bounding rectangle for collision detection
/// </summary>
public struct BoundingRectangle
{
    public float X;

    public float Y;

    public float Width;

    public float Height;

    public float Left => X;

    public float Right => X + Width;

    public float Top => Y;

    public float Bottom => Y + Height;

    /// <summary>
    /// The middle of the rectangle
    /// </summary>
    public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

    public BoundingRectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public BoundingRectangle(Vector2 position, float width, float height)
    {
        X = position.X;
        Y = position.Y;
        Width = width;
        Height = height;
    }

    public bool CollidesWith(BoundingRectangle other)
    {
        return CollisionHelper.Collides(this, other);
    }

    public bool CollidesWith(BoundingCircle other)
    {
        return CollisionHelper.Collides(other, this);
    }
}
