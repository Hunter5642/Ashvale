using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ashvale;

/// <summary>
/// A box with a label that can be clicked with the mouse. It is drawn by stretching a
/// single white pixel, so it needs no artwork of its own
/// </summary>
public class Button
{
    /// <summary>
    /// How thick the outline is, in pixels
    /// </summary>
    private const int BORDER = 3;

    /// <summary>
    /// The words written on the button
    /// </summary>
    public string Text;

    /// <summary>
    /// Where the button sits on the screen, and how big it is
    /// </summary>
    public Rectangle Bounds;

    /// <summary>
    /// Creates a button centered on a point
    /// </summary>
    /// <param name="text">The label</param>
    /// <param name="centerX">Where the middle of the button goes across the screen</param>
    /// <param name="centerY">Where the middle of the button goes down the screen</param>
    public Button(string text, int centerX, int centerY)
    {
        Text = text;
        Bounds = new Rectangle(centerX - 150, centerY - 32, 300, 64);
    }

    /// <summary>
    /// Whether the button was clicked this frame. The click counts on the frame the left
    /// button goes down, so holding it down doesn't press the button again and again
    /// </summary>
    /// <param name="currentMouseState">The mouse this frame</param>
    /// <param name="priorMouseState">The mouse last frame</param>
    public bool IsClicked(MouseState currentMouseState, MouseState priorMouseState)
    {
        return Bounds.Contains(currentMouseState.Position)
            && currentMouseState.LeftButton == ButtonState.Pressed
            && priorMouseState.LeftButton == ButtonState.Released;
    }

    /// <summary>
    /// Draws the button, lit up in gold while the mouse is over it
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    /// <param name="font">The font to write the label in</param>
    /// <param name="pixel">A single white pixel to stretch into the box</param>
    /// <param name="mousePosition">Where the mouse is, to tell if it is over the button</param>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Point mousePosition)
    {
        bool hovered = Bounds.Contains(mousePosition);

        spriteBatch.Draw(pixel, Bounds, hovered ? Color.Gold : Color.White * 0.7f);
        Rectangle inside = Bounds;
        inside.Inflate(-BORDER, -BORDER);
        spriteBatch.Draw(pixel, inside, hovered ? new Color(40, 30, 10) : new Color(20, 20, 20));

        Vector2 size = font.MeasureString(Text);
        Vector2 position = new Vector2(Bounds.Center.X - size.X / 2, Bounds.Center.Y - size.Y / 2);
        spriteBatch.DrawString(font, Text, position, hovered ? Color.Gold : Color.White);
    }
}
