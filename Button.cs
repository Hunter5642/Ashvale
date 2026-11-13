using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ashvale;

/// <summary>
/// A clickable button with a label
/// </summary>
public class Button
{
    /// <summary>
    /// How thick the outline is, in pixels
    /// </summary>
    private const int BORDER = 3;

    /// <summary>
    /// The label
    /// </summary>
    public string Text;

    /// <summary>
    /// Where the button is and how big it is
    /// </summary>
    public Rectangle Bounds;

    /// <summary>
    /// Whether the button can be pressed
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Creates a button centered on a point
    /// </summary>
    /// <param name="text">The label</param>
    /// <param name="centerX">The X of the button's middle</param>
    /// <param name="centerY">The Y of the button's middle</param>
    public Button(string text, int centerX, int centerY)
    {
        Text = text;
        Bounds = new Rectangle(centerX - 150, centerY - 32, 300, 64);
    }

    /// <summary>
    /// Whether the button was clicked this frame
    /// </summary>
    /// <param name="currentMouseState">The mouse this frame</param>
    /// <param name="priorMouseState">The mouse last frame</param>
    public bool IsClicked(MouseState currentMouseState, MouseState priorMouseState)
    {
        return Enabled
            && Bounds.Contains(currentMouseState.Position)
            && currentMouseState.LeftButton == ButtonState.Pressed
            && priorMouseState.LeftButton == ButtonState.Released;
    }

    /// <summary>
    /// Draws the button, highlighted on hover and greyed out when disabled
    /// </summary>
    /// <param name="spriteBatch">The SpriteBatch to draw with</param>
    /// <param name="font">The font to write the label in</param>
    /// <param name="pixel">A single white pixel</param>
    /// <param name="mousePosition">Where the mouse is</param>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Point mousePosition)
    {
        bool hovered = Enabled && Bounds.Contains(mousePosition);

        Color outline = hovered ? Color.Gold : Color.White * 0.7f;
        Color label = hovered ? Color.Gold : Color.White;
        if (!Enabled)
        {
            outline = Color.White * 0.25f;
            label = Color.Gray * 0.6f;
        }

        spriteBatch.Draw(pixel, Bounds, outline);
        Rectangle inside = Bounds;
        inside.Inflate(-BORDER, -BORDER);
        spriteBatch.Draw(pixel, inside, hovered ? new Color(40, 30, 10) : new Color(20, 20, 20));

        Vector2 size = font.MeasureString(Text);
        Vector2 position = new Vector2(Bounds.Center.X - size.X / 2, Bounds.Center.Y - size.Y / 2);
        spriteBatch.DrawString(font, Text, position, label);
    }
}
