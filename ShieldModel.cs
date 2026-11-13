using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// A 3D shield built from triangles: blue with a gold rim and a raised gold cross
/// </summary>
public class ShieldModel
{
    /// <summary>
    /// The shield's half width, top and bottom point
    /// </summary>
    private const float HALF_WIDTH = 0.5f;
    private const float TOP = -0.6f;
    private const float BOTTOM = 0.62f;

    /// <summary>
    /// How far down the curved sides bend
    /// </summary>
    private const float CURVE_PULL = 0.2f;

    /// <summary>
    /// Points along each curved side
    /// </summary>
    private const int CURVE_POINTS = 8;

    /// <summary>
    /// How thick the shield is
    /// </summary>
    private const float THICKNESS = 0.14f;

    /// <summary>
    /// How big the blue middle is. The rest is the gold rim
    /// </summary>
    private const float RIM = 0.82f;

    /// <summary>
    /// How far the cross sticks out
    /// </summary>
    private const float CROSS_DEPTH = 0.06f;

    /// <summary>
    /// The shield's colors, and the gold's glow
    /// </summary>
    private static readonly Vector3 BLUE = new Vector3(0.18f, 0.32f, 0.72f);
    private static readonly Vector3 GOLD = new Vector3(0.95f, 0.75f, 0.25f);
    private static readonly Vector3 GOLD_GLOW = new Vector3(0.2f, 0.15f, 0.02f);

    /// <summary>
    /// Which way each side of a box faces
    /// </summary>
    private static readonly Vector3 TOWARD_PLAYER = new Vector3(0, 0, 1);
    private static readonly Vector3 AWAY_FROM_PLAYER = new Vector3(0, 0, -1);
    private static readonly Vector3 UP_SCREEN = new Vector3(0, -1, 0);
    private static readonly Vector3 DOWN_SCREEN = new Vector3(0, 1, 0);
    private static readonly Vector3 LEFT = new Vector3(-1, 0, 0);
    private static readonly Vector3 RIGHT = new Vector3(1, 0, 0);

    private readonly GraphicsDevice graphicsDevice;

    /// <summary>
    /// The blue and gold triangles
    /// </summary>
    private readonly VertexBuffer blueBuffer;
    private readonly VertexBuffer goldBuffer;

    /// <summary>
    /// The effect that draws the shield
    /// </summary>
    private readonly BasicEffect effect;

    /// <summary>
    /// Builds the shield
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to draw with</param>
    public ShieldModel(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;

        List<Vector2> outline = MakeOutline();
        float front = THICKNESS / 2;
        float back = -THICKNESS / 2;

        var blue = new List<VertexPositionNormalTexture>();
        var gold = new List<VertexPositionNormalTexture>();

        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 p = outline[i];
            Vector2 q = outline[(i + 1) % outline.Count];

            // front
            Graphics3D.AddTriangle(blue, new Vector3(0, 0, front), At(p * RIM, front), At(q * RIM, front), TOWARD_PLAYER);
            Graphics3D.AddTriangle(gold, At(p, front), At(q, front), At(q * RIM, front), TOWARD_PLAYER);
            Graphics3D.AddTriangle(gold, At(p, front), At(q * RIM, front), At(p * RIM, front), TOWARD_PLAYER);

            // back
            Graphics3D.AddTriangle(blue, new Vector3(0, 0, back), At(p, back), At(q, back), AWAY_FROM_PLAYER);

            // edge
            Vector2 along = q - p;
            var outward = Vector3.Normalize(new Vector3(along.Y, -along.X, 0));
            if (Vector2.Dot(new Vector2(outward.X, outward.Y), p + q) < 0) outward = -outward;
            Graphics3D.AddTriangle(gold, At(p, front), At(q, front), At(q, back), outward);
            Graphics3D.AddTriangle(gold, At(p, front), At(q, back), At(p, back), outward);
        }

        // the cross
        AddBar(gold, -0.06f, 0.06f, -0.40f, 0.36f, front);
        AddBar(gold, -0.30f, 0.30f, -0.22f, -0.10f, front);

        blueBuffer = Graphics3D.ToVertexBuffer(graphicsDevice, blue);
        goldBuffer = Graphics3D.ToVertexBuffer(graphicsDevice, gold);

        effect = new BasicEffect(graphicsDevice);
        Graphics3D.SetUpLighting(effect);
    }

    /// <summary>
    /// Works out the points around the shield's edge
    /// </summary>
    /// <returns>The outline's points</returns>
    private static List<Vector2> MakeOutline()
    {
        // right side curve
        var start = new Vector2(HALF_WIDTH, TOP);
        var control = new Vector2(HALF_WIDTH, CURVE_PULL);
        var end = new Vector2(0, BOTTOM);

        var rightSide = new List<Vector2>();
        for (int i = 0; i <= CURVE_POINTS; i++)
        {
            float t = (float)i / CURVE_POINTS;
            rightSide.Add((1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end);
        }

        // right side, then the mirrored left side
        var outline = new List<Vector2> { new Vector2(-HALF_WIDTH, TOP) };
        outline.AddRange(rightSide);
        for (int i = CURVE_POINTS - 1; i > 0; i--)
            outline.Add(new Vector2(-rightSide[i].X, rightSide[i].Y));
        return outline;
    }

    /// <summary>
    /// A 2D outline point at a given depth
    /// </summary>
    private static Vector3 At(Vector2 point, float z) => new Vector3(point.X, point.Y, z);

    /// <summary>
    /// Adds one bar of the cross
    /// </summary>
    /// <param name="vertices">The list to add to</param>
    /// <param name="left">The left edge of the bar</param>
    /// <param name="right">The right edge of the bar</param>
    /// <param name="top">The top edge of the bar</param>
    /// <param name="bottom">The bottom edge of the bar</param>
    /// <param name="front">Where the front of the shield is</param>
    private static void AddBar(List<VertexPositionNormalTexture> vertices,
                               float left, float right, float top, float bottom, float front)
    {
        float z = front + CROSS_DEPTH;

        var a = new Vector3(left, top, z);
        var b = new Vector3(right, top, z);
        var c = new Vector3(right, bottom, z);
        var d = new Vector3(left, bottom, z);
        var aBase = new Vector3(left, top, front);
        var bBase = new Vector3(right, top, front);
        var cBase = new Vector3(right, bottom, front);
        var dBase = new Vector3(left, bottom, front);

        AddRectangle(vertices, a, b, c, d, TOWARD_PLAYER);
        AddRectangle(vertices, aBase, bBase, b, a, UP_SCREEN);
        AddRectangle(vertices, dBase, cBase, c, d, DOWN_SCREEN);
        AddRectangle(vertices, aBase, dBase, d, a, LEFT);
        AddRectangle(vertices, bBase, cBase, c, b, RIGHT);
    }

    /// <summary>
    /// Adds a rectangle as two triangles
    /// </summary>
    private static void AddRectangle(List<VertexPositionNormalTexture> vertices,
                                     Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
    {
        Graphics3D.AddTriangle(vertices, a, b, c, outward);
        Graphics3D.AddTriangle(vertices, a, c, d, outward);
    }

    /// <summary>
    /// Draws the shield
    /// </summary>
    /// <param name="world">The shield's position, rotation and size</param>
    /// <param name="view">The camera's transform</param>
    /// <param name="projection">The projection from Graphics3D.Projection()</param>
    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        effect.World = world;
        effect.View = view;
        effect.Projection = projection;

        DrawPart(blueBuffer, BLUE, Vector3.Zero);
        DrawPart(goldBuffer, GOLD, GOLD_GLOW);
    }

    /// <summary>
    /// Draws one part of the shield in one color
    /// </summary>
    private void DrawPart(VertexBuffer buffer, Vector3 color, Vector3 glow)
    {
        effect.DiffuseColor = color;
        effect.EmissiveColor = glow;
        effect.CurrentTechnique.Passes[0].Apply();

        graphicsDevice.SetVertexBuffer(buffer);
        graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, buffer.VertexCount / 3);
    }
}
