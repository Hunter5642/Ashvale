using System;
using System.IO;
using System.Text.Json;

namespace Ashvale;

/// <summary>
/// Everything needed to pick a game back up: which level the knight is on and how many lives
/// he has. The game writes this out to a save file whenever either one changes, and the
/// Continue button on the title screen reads it back in and starts that level over. It only
/// holds plain numbers in properties, so System.Text.Json can turn it into text and back
/// without any extra setup
/// </summary>
public class SaveData
{
    /// <summary>
    /// Where the save file goes: in the same folder as the game itself
    /// </summary>
    public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "ashvale_save.json");

    /// <summary>
    /// Which level the knight is on, counting from 0
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// How many lives the knight has left
    /// </summary>
    public int Lives { get; set; }

    /// <summary>
    /// Whether there is a save file to continue from
    /// </summary>
    public static bool Exists => File.Exists(FilePath);

    /// <summary>
    /// Writes this save out to the save file, replacing whatever was there
    /// </summary>
    public void Save()
    {
        // indented so the file is easy to read if you open it
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Reads the save file back in
    /// </summary>
    /// <returns>The save, or null if there isn't one or it can't be read</returns>
    public static SaveData Load()
    {
        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<SaveData>(json);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the save file, once a game is won or lost and there is nothing left to continue
    /// </summary>
    public static void Delete()
    {
        if (Exists) File.Delete(FilePath);
    }
}
