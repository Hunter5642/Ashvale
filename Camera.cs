using System;
using Microsoft.Xna.Framework;

namespace Ashvale;

/// <summary>
/// The camera that scrolls, zooms and shakes the world
/// </summary>
public class Camera
{
    /// <summary>
    /// How quickly the camera catches up to the knight
    /// </summary>
    private const float FOLLOW_SHARPNESS = 6;

    /// <summary>
    /// How quickly the zoom settles
    /// </summary>
    private const float ZOOM_SHARPNESS = 4;

    private readonly MathHelper.Random random;

    private float shakeMagnitude;
    private float shakeDuration;
    private float shakeTimer;
    private Vector2 shakeOffset;

    /// <summary>
    /// The world position of the top-left corner of the screen
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// The current zoom. 1 shows one window of the world
    /// </summary>
    public float Zoom { get; private set; } = 1;

    /// <summary>
    /// The zoom the camera is easing toward
    /// </summary>
    public float TargetZoom { get; set; } = 1;

    /// <summary>
    /// The point on the screen the zoom centers on
    /// </summary>
    public Vector2 ZoomFocus { get; set; } = new Vector2(AshvaleGame.WINDOW_WIDTH / 2f, AshvaleGame.WINDOW_HEIGHT / 2f);

    /// <summary>
    /// The left and right edges of the world in view
    /// </summary>
    public float ViewLeft => Position.X;
    public float ViewRight => Position.X + AshvaleGame.WINDOW_WIDTH;

    /// <summary>
    /// Constructs a camera
    /// </summary>
    /// <param name="random">The game's random number generator</param>
    public Camera(MathHelper.Random random)
    {
        this.random = random;
    }

    /// <summary>
    /// Resets the camera to the start of the world, unzoomed and still
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Zoom = TargetZoom = 1;
        shakeTimer = shakeDuration = 0;
        shakeOffset = Vector2.Zero;
    }

    /// <summary>
    /// Jumps straight to a zoom level
    /// </summary>
    /// <param name="zoom">The zoom level</param>
    /// <param name="focus">The point on screen to zoom toward</param>
    public void SnapZoom(float zoom, Vector2 focus)
    {
        Zoom = TargetZoom = zoom;
        ZoomFocus = focus;
    }

    /// <summary>
    /// Starts the screen shaking. A weaker shake won't cut a stronger one short
    /// </summary>
    /// <param name="magnitude">How far the screen can move, in pixels</param>
    /// <param name="duration">How long the shake lasts, in seconds</param>
    public void Shake(float magnitude, float duration)
    {
        float remaining = shakeDuration > 0 ? shakeMagnitude * (shakeTimer / shakeDuration) : 0;
        if (magnitude < remaining) return;

        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    /// <summary>
    /// Slides the camera to keep the given X in the middle of the screen
    /// </summary>
    /// <param name="x">The world X to keep centered</param>
    /// <param name="gameTime">The game time</param>
    public void Follow(float x, GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float target = MathHelper.Clamp(x - AshvaleGame.WINDOW_WIDTH / 2f,
                                        0, AshvaleGame.WORLD_WIDTH - AshvaleGame.WINDOW_WIDTH);

        // smooth follow
        Position.X = MathHelper.Lerp(Position.X, target, 1 - MathF.Exp(-FOLLOW_SHARPNESS * t));
    }

    /// <summary>
    /// Eases the zoom and updates the shake
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Zoom = MathHelper.Lerp(Zoom, TargetZoom, 1 - MathF.Exp(-ZOOM_SHARPNESS * t));

        if (shakeTimer > 0)
        {
            shakeTimer = Math.Max(0, shakeTimer - t);

            // fading shake in a random direction
            float strength = shakeMagnitude * (shakeTimer / shakeDuration);
            float angle = random.NextFloat(0, MathHelper.TwoPi);
            shakeOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * strength;
        }
        else
        {
            shakeOffset = Vector2.Zero;
        }
    }

    /// <summary>
    /// Builds the transform for a SpriteBatch drawing a layer
    /// </summary>
    /// <param name="parallax">How fast the layer scrolls compared to the world</param>
    /// <returns>The transform</returns>
    public Matrix GetTransform(float parallax = 1)
    {
        // zoom in a little so the shake doesn't show the edges
        float shakeCover = 1 + 2 * shakeOffset.Length() / AshvaleGame.WINDOW_HEIGHT;

        return Matrix.CreateTranslation(-Position.X * parallax, -Position.Y * parallax, 0)
             * Matrix.CreateTranslation(-ZoomFocus.X, -ZoomFocus.Y, 0)
             * Matrix.CreateScale(Zoom * shakeCover)
             * Matrix.CreateTranslation(ZoomFocus.X + shakeOffset.X, ZoomFocus.Y + shakeOffset.Y, 0);
    }
}
