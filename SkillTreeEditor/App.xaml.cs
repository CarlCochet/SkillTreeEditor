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

        foreach (var sphere in Spheres)
        {
            sphere.BarrierCoachCards = [];
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

    public SphereBoardData CreateSphereBoard()
    {
        var newId = 1;
        while (SphereBoards.Any(sphereBoard => sphereBoard.Id == newId))
            newId++;
        
        return new SphereBoardData
        {
            Id = newId,
            SeasonId = 1,
            BreedId = 1,
            FighterCardListId = 5,
            InitialSpellIds = [ 31, 36, 34 ],
            StartX = 1,
            StartY = 1,
        };
    }

    public SphereData CreateSphere(int x, int y, int sphereBoardId)
    {
        var newId = 1;
        while (Spheres.Any(sphere => sphere.Id == newId))
            newId++;
        
        var sphere = new SphereData
        {
            SphereBoardId = sphereBoardId,
            XPosition = x,
            YPosition = y
        };

        return sphere;
    }

    public void RemoveSphere(int x, int y, int sphereBoardId)
    {
        Spheres.RemoveAll(sphere => sphere.SphereBoardId == sphereBoardId && sphere.XPosition == x && sphere.YPosition == y);
    }
}
