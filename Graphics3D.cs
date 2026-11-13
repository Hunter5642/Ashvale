using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ashvale;

/// <summary>
/// Helpers for drawing 3D models in the 2D world
/// </summary>
public static class Graphics3D
{
    /// <summary>
    /// How far in front of or behind the screen a model can be drawn
    /// </summary>
    private const float DEPTH_RANGE = 1000;

    /// <summary>
    /// Builds the projection matrix for drawing 3D models over the 2D world.
    /// </summary>
    /// <param name="viewport">The viewport being drawn to</param>
    /// <returns>The projection matrix</returns>
    public static Matrix Projection(Viewport viewport)
    {
        return Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0,
                                                  -DEPTH_RANGE, DEPTH_RANGE);
    }

    /// <summary>
    /// Turns the depth buffer back on and clears it, since SpriteBatch turns it off
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    public static void Begin(GraphicsDevice graphicsDevice)
    {
        graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);
        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        graphicsDevice.BlendState = BlendState.Opaque;
    }

    /// <summary>
    /// Lights an effect with sunlight, a lava glow and some ambient light
    /// </summary>
    /// <param name="effect">The effect to light</param>
    public static void SetUpLighting(BasicEffect effect)
    {
        effect.LightingEnabled = true;
        effect.AmbientLightColor = new Vector3(0.25f, 0.22f, 0.22f);

        // sunlight from the upper left
        effect.DirectionalLight0.Enabled = true;
        effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, 1, -1));
        effect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 0.95f, 0.85f);
        effect.DirectionalLight0.SpecularColor = Vector3.Zero;

        // lava glow from below
        effect.DirectionalLight1.Enabled = true;
        effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-0.4f, -1, -0.3f));
        effect.DirectionalLight1.DiffuseColor = new Vector3(0.6f, 0.25f, 0.1f);
        effect.DirectionalLight1.SpecularColor = Vector3.Zero;

        effect.DirectionalLight2.Enabled = false;
        effect.SpecularColor = Vector3.Zero;
    }

    /// <summary>
    /// Adds a triangle with its corners in the order that makes its outside face the player
    /// </summary>
    /// <param name="vertices">The list to add the triangle to</param>
    /// <param name="a">The first corner</param>
    /// <param name="b">The second corner</param>
    /// <param name="c">The third corner</param>
    /// <param name="outward">Which way the outside of the triangle faces</param>
    public static void AddTriangle(List<VertexPositionNormalTexture> vertices,
                                   Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        // swap two corners if the triangle faces the wrong way
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0)
            (b, c) = (c, b);

        vertices.Add(new VertexPositionNormalTexture(a, outward, Vector2.Zero));
        vertices.Add(new VertexPositionNormalTexture(b, outward, Vector2.Zero));
        vertices.Add(new VertexPositionNormalTexture(c, outward, Vector2.Zero));
    }

    /// <summary>
    /// Copies a list of vertices into a VertexBuffer
    /// </summary>
    /// <param name="graphicsDevice">The graphics device</param>
    /// <param name="vertices">The vertices, three per triangle</param>
    /// <returns>The vertex buffer</returns>
    public static VertexBuffer ToVertexBuffer(GraphicsDevice graphicsDevice,
                                              List<VertexPositionNormalTexture> vertices)
    {
        var buffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture),
                                      vertices.Count, BufferUsage.None);
        buffer.SetData(vertices.ToArray());
        return buffer;
    }
}
