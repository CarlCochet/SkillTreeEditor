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
    public List<BreedData> Breeds = [];
    public List<EffectData> CardEffects = [];
    public List<BreedWeightsData> BreedWeights = [];
    public readonly Dictionary<int, Fighter> Fighters = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
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
            if (sphere.Id == 0)
                sphere.Id = GenerateSphereId();
            foreach (var sphereEffect in sphere.Effects)
            {
                if (sphereEffect.Id == 0)
                    sphereEffect.Id = GenerateEffectId();
            }
        }
        
        var spellPath = Path.Combine(folderPath, "spell_cards.json");
        if (File.Exists(spellPath))
        {
            var json = File.ReadAllText(spellPath);
            SpellCards = JsonSerializer.Deserialize<List<SpellData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {SpellCards.Count} spells");
        }

        var breedPath = Path.Combine(folderPath, "breed_characteristics.json");
        if (File.Exists(breedPath))
        {
            var json = File.ReadAllText(breedPath);
            Breeds = JsonSerializer.Deserialize<List<BreedData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {Breeds.Count} breeds");
        }
        
        var effectPath = Path.Combine(folderPath, "card_effects.json");
        if (File.Exists(effectPath))
        {
            var json = File.ReadAllText(effectPath);
            CardEffects = JsonSerializer.Deserialize<List<EffectData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {CardEffects.Count} card effects");
        }
        
        var breedWeightsPath = Path.Combine(folderPath, "breed_stats_weights.json");
        if (File.Exists(breedWeightsPath))
        {
            var json = File.ReadAllText(breedWeightsPath);
            BreedWeights = JsonSerializer.Deserialize<List<BreedWeightsData>>(json, _jsonOptions) ?? [];
            Console.WriteLine($"Loaded {BreedWeights.Count} breed weights");
        }
    }
    
    public void SaveProjectFolder(string folderPath)
    {
        ComputeLinkedSpheres();
            
        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        var spheresPath = Path.Combine(folderPath, "spheres.json");
        
        var sphereBoardsJson = JsonSerializer.Serialize(SphereBoards, _jsonOptions);
        File.WriteAllText(sphereBoardsPath, sphereBoardsJson);
        
        var spheresJson = JsonSerializer.Serialize(Spheres, _jsonOptions);
        File.WriteAllText(spheresPath, spheresJson);
    }

    public SphereBoardData CreateSphereBoard()
    {
        return new SphereBoardData
        {
            Id = GenerateSphereBoardId(),
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
        var newSphere = new SphereData
        {
            Id = GenerateSphereId(),
            SphereBoardId = sphereBoardId,
            XPosition = x,
            YPosition = y
        };
        Spheres.Add(newSphere);
        return newSphere;
    }

    public void RemoveSphere(int x, int y, int sphereBoardId)
    {
        Spheres.RemoveAll(sphere => sphere.SphereBoardId == sphereBoardId 
                                    && sphere.XPosition == x 
                                    && sphere.YPosition == y);
    }

    public EffectData CreateEffect(SphereData sphere)
    {
        var newEffect = new EffectData
        {
            Id = GenerateEffectId(),
            ParentId = sphere.Id,
            ParentType = "SPHERE",
            AreaShape = 1,
            Personal = true
        };
        sphere.Effects.Add(newEffect);
        return newEffect;
    }

    public void CreateEffectCopy(SphereData sphere, EffectData effect)
    {
        var newEffect = effect.Copy();
        effect.Id = GenerateEffectId();
        sphere.Effects.Add(newEffect);
    }

    private void ComputeLinkedSpheres()
    {
        foreach (var sphereBoardData in SphereBoards)
        {
            var spheres = Spheres
                .Where(s => s.SphereBoardId == sphereBoardData.Id && !s.Impassable)
                .ToList();

            var startSphere = spheres.FirstOrDefault(s => s.XPosition == sphereBoardData.StartX 
                                                          && s.YPosition == sphereBoardData.StartY);

            if (startSphere is null)
            {
                startSphere = new SphereData
                {
                    Id = 0,
                    SphereBoardId = sphereBoardData.Id, 
                    XPosition = sphereBoardData.StartX, 
                    YPosition = sphereBoardData.StartY
                };
                Spheres.Add(startSphere);
            }
            
            var sphereByPosition = spheres.ToDictionary(s => (s.XPosition, s.YPosition));
            var effectSpheres = spheres.Where(IsEffectSphere).ToList();
            effectSpheres.Add(startSphere);

            foreach (var sphere in effectSpheres)
            {
                sphere.LinkedSphereIds = [];
            }

            foreach (var origin in effectSpheres)
            {
                HashSet<int> linkedSphereIds = [];

                foreach (var (dx, dy) in Directions)
                {
                    var nextPos = (origin.XPosition + dx, origin.YPosition + dy);

                    if (!sphereByPosition.TryGetValue(nextPos, out var neighbor))
                        continue;

                    HashSet<int> visited = [origin.Id];
                    ExploreBranch(neighbor, sphereByPosition, visited, linkedSphereIds);
                }

                origin.LinkedSphereIds = linkedSphereIds.ToList();
            }
        }
    }

    private static void ExploreBranch(
        SphereData current,
        Dictionary<(int X, int Y), SphereData> sphereByPosition,
        HashSet<int> visited,
        HashSet<int> foundEffectIds)
    {
        if (!visited.Add(current.Id))
            return;

        if (IsEffectSphere(current))
        {
            foundEffectIds.Add(current.Id);
            return;
        }

        foreach (var (dx, dy) in Directions)
        {
            var nextPos = (current.XPosition + dx, current.YPosition + dy);

            if (!sphereByPosition.TryGetValue(nextPos, out var nextSphere))
                continue;

            if (visited.Contains(nextSphere.Id))
                continue;

            ExploreBranch(nextSphere, sphereByPosition, visited, foundEffectIds);
        }
    }

    private static bool IsEffectSphere(SphereData sphere)
    {
        return sphere.Effects.Count > 0
               || sphere.SpellId > 0
               || sphere.FighterCardListId > 0
               || sphere.TeleportXPosition > 0
               || sphere.TeleportYPosition > 0;
    }

    private static readonly (int Dx, int Dy)[] Directions =
    [
        (0, -1),
        (1, 0),
        (0, 1),
        (-1, 0)
    ];

    private int GenerateSphereBoardId()
    {
        var newId = 1;
        while (SphereBoards.Any(sb => sb.Id == newId))
            newId++;
        return newId;
    }

    private int GenerateSphereId()
    {
        var newId = 1;
        var sphereIds = Spheres.Select(s => s.Id).ToHashSet();
        while (sphereIds.Contains(newId))
            newId++;
        return newId;
    }

    private int GenerateEffectId()
    {
        var newId = 1;
        var effects = CardEffects.Select(e => e.Id);
        var sphereEffects = Spheres.SelectMany(s => s.Effects).Select(e => e.Id);
        var effectIds = effects.Concat(sphereEffects).ToHashSet();
        while (effectIds.Contains(newId))
            newId++;
        return newId;
    }
}
