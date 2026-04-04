using System.Configuration;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using Application = System.Windows.Application;

namespace SkillTreeEditor;

public partial class App : Application
{
    public List<SphereBoardData> SphereBoards = [];
    public List<SphereData> Spheres = [];
    public List<SpellData> SpellCards = [];
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public void LoadProjectFolder(string folderPath)
    {
        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        if (File.Exists(sphereBoardsPath))
        {
            var json = File.ReadAllText(sphereBoardsPath);
            SphereBoards = JsonSerializer.Deserialize<List<SphereBoardData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {SphereBoards.Count} sphere boards");
        }

        var spheresPath = Path.Combine(folderPath, "spheres.json");
        if (File.Exists(spheresPath))
        {
            var json = File.ReadAllText(spheresPath);
            Spheres = JsonSerializer.Deserialize<List<SphereData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {Spheres.Count} spheres");
        }
        
        var spellPath = Path.Combine(folderPath, "spell_cards.json");
        if (File.Exists(spellPath))
        {
            var json = File.ReadAllText(spellPath);
            SpellCards = JsonSerializer.Deserialize<List<SpellData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {SpellCards.Count} spells");
        }
    }
    
    public void SaveProjectFolder(string folderPath)
    {
        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        var spheresPath = Path.Combine(folderPath, "spheres.json");
        
        var sphereBoardsJson = JsonSerializer.Serialize(SphereBoards, _jsonOptions);
        File.WriteAllText(sphereBoardsPath, sphereBoardsJson);
        
        var spheresJson = JsonSerializer.Serialize(Spheres, _jsonOptions);
        File.WriteAllText(spheresPath, spheresJson);
    }
}
