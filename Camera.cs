using System;
using Microsoft.Xna.Framework;

namespace Ashvale;

/// <summary>
/// Looks at one window-sized piece of the world at a time. Every SpriteBatch that draws the
/// world begins with one of this camera's transforms, so scrolling, zooming and shaking move
/// the whole batch at once instead of each sprite working out where it should be on screen.
/// The game registers it as a service so the particle systems can draw through it too.
/// </summary>
public class Camera
{
    /// <summary>
    /// How quickly the camera catches up to the knight. Higher is snappier
    /// </summary>
    private const float FOLLOW_SHARPNESS = 6;

    /// <summary>
    /// How quickly the zoom settles on the level it is heading for
    /// </summary>
    private const float ZOOM_SHARPNESS = 4;

    private readonly MathHelper.Random random;

    private float shakeMagnitude;
    private float shakeDuration;
    private float shakeTimer;
    private Vector2 shakeOffset;

    /// <summary>
    /// The world position of the top-left corner of the screen, before zooming. This is the
    /// same offset the parallax tutorial works out from the player's position
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// How far the camera is zoomed in. 1 shows exactly one window of the world
    /// </summary>
    public float Zoom { get; private set; } = 1;

    /// <summary>
    /// The zoom the camera is easing toward
    /// </summary>
    public float TargetZoom { get; set; } = 1;

    /// <summary>
    /// The point on the screen that stays put while zooming, so zooming in moves toward it.
    /// Zooming in never shows past the edges of what zoom 1 shows, so nothing outside the
    /// world can slip into view
    /// </summary>
    public Vector2 ZoomFocus { get; set; } = new Vector2(AshvaleGame.WINDOW_WIDTH / 2f, AshvaleGame.WINDOW_HEIGHT / 2f);

    /// <summary>
    /// The left and right edges of the world that the camera is looking at
    /// </summary>
    public float ViewLeft => Position.X;
    public float ViewRight => Position.X + AshvaleGame.WINDOW_WIDTH;

    /// <summary>
    /// Constructs a camera
    /// </summary>
    /// <param name="random">The game's random number generator, for shaking</param>
    public Camera(MathHelper.Random random)
    {
        this.random = random;
    }

    /// <summary>
    /// Snaps the camera back to the very start of the world, unzoomed and still
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Zoom = TargetZoom = 1;
        shakeTimer = shakeDuration = 0;
        shakeOffset = Vector2.Zero;
    }

    /// <summary>
    /// Jumps straight to a zoom level without easing into it
    /// </summary>
    /// <param name="zoom">The zoom level</param>
    /// <param name="focus">The point on screen to zoom toward</param>
    public void SnapZoom(float zoom, Vector2 focus)
    {
        Zoom = TargetZoom = zoom;
        ZoomFocus = focus;
    }

    /// <summary>
    /// Starts the screen shaking. A stronger shake takes over from a weaker one, but a weak
    /// shake won't cut a strong one short
    /// </summary>
    /// <param name="magnitude">How far the screen can be thrown, in pixels</param>
    /// <param name="duration">How long the shake takes to die down, in seconds</param>
    public void Shake(float magnitude, float duration)
    {
        float remaining = shakeDuration > 0 ? shakeMagnitude * (shakeTimer / shakeDuration) : 0;
        if (magnitude < remaining) return;

        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    /// <summary>
    /// Slides the camera toward keeping the given world X in the middle of the screen, without
    /// ever showing past either end of the world
    /// </summary>
    /// <param name="x">The world X to keep centered</param>
    /// <param name="gameTime">The game time</param>
    public void Follow(float x, GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float target = MathHelper.Clamp(x - AshvaleGame.WINDOW_WIDTH / 2f,
                                        0, AshvaleGame.WORLD_WIDTH - AshvaleGame.WINDOW_WIDTH);

        // closing the same fraction of the gap each second means a smooth slow-down at the end,
        // however fast or slow the frames come
        Position.X = MathHelper.Lerp(Position.X, target, 1 - MathF.Exp(-FOLLOW_SHARPNESS * t));
    }

    /// <summary>
    /// Eases the zoom toward its target and works out this frame's shake
    /// </summary>
    /// <param name="gameTime">The game time</param>
    public void Update(GameTime gameTime)
    {
        float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Zoom = MathHelper.Lerp(Zoom, TargetZoom, 1 - MathF.Exp(-ZOOM_SHARPNESS * t));

        if (shakeTimer > 0)
        {
            shakeTimer = Math.Max(0, shakeTimer - t);

            // the shake fades out as its time runs down, and picks a new direction every frame
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
    /// Builds the matrix to hand to SpriteBatch.Begin(). Layers further away scroll more
    /// slowly than the world does, which is what makes them look far away
    /// </summary>
    /// <param name="parallax">How fast this layer scrolls compared to the world. 1 is the
    /// world itself, 0.5 is a background layer moving at half speed</param>
    /// <returns>The transform for a SpriteBatch drawing that layer</returns>
    public Matrix GetTransform(float parallax = 1)
    {
        // A shake pushes the picture a few pixels aside, which would uncover the black
        // behind it, so zoom in just far enough to keep the edges covered
        float shakeCover = 1 + 2 * shakeOffset.Length() / AshvaleGame.WINDOW_HEIGHT;

        return Matrix.CreateTranslation(-Position.X * parallax, -Position.Y * parallax, 0)
             * Matrix.CreateTranslation(-ZoomFocus.X, -ZoomFocus.Y, 0)
             * Matrix.CreateScale(Zoom * shakeCover)
             * Matrix.CreateTranslation(ZoomFocus.X + shakeOffset.X, ZoomFocus.Y + shakeOffset.Y, 0);
    }
}
