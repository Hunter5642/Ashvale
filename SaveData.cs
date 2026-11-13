using System;
using System.IO;
using System.Text.Json;

namespace Ashvale;

/// <summary>
/// The saved game: the level and lives
/// </summary>
public class SaveData
{
    /// <summary>
    /// Where the save file goes, next to the game
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
    /// Writes the save file
    /// </summary>
    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Reads the save file
    /// </summary>
    /// <returns>The save, or null if there isn't one</returns>
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
    /// Deletes the save file
    /// </summary>
    public static void Delete()
    {
        if (Exists) File.Delete(FilePath);
    }
}
