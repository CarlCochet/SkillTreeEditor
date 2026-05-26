using System.IO;
using System.Text.Json;
using SkillTreeEditor.Data;

namespace SkillTreeEditor.Services;

public class ProjectService(ProjectStore store)
{
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
            var boards = JsonSerializer.Deserialize<List<SphereBoardData>>(json, _jsonOptions) ?? [];
            store.SphereBoards.Clear();
            store.SphereBoards.AddRange(boards);
            Console.WriteLine($"Loaded {store.SphereBoards.Count} sphere boards");
        }

        var spheresPath = Path.Combine(folderPath, "spheres.json");
        if (File.Exists(spheresPath))
        {
            var json = File.ReadAllText(spheresPath);
            var spheres = JsonSerializer.Deserialize<List<SphereData>>(json, _jsonOptions) ?? [];
            store.Spheres.Clear();
            store.Spheres.AddRange(spheres);
            Console.WriteLine($"Loaded {store.Spheres.Count} spheres");
        }

        IndexSpheres();

        var spellPath = Path.Combine(folderPath, "spell_cards.json");
        if (File.Exists(spellPath))
        {
            var json = File.ReadAllText(spellPath);
            var spells = JsonSerializer.Deserialize<List<SpellData>>(json, _jsonOptions) ?? [];
            store.SpellCards.Clear();
            store.SpellCards.AddRange(spells);
            Console.WriteLine($"Loaded {store.SpellCards.Count} spells");
        }

        var breedPath = Path.Combine(folderPath, "breed_characteristics.json");
        if (File.Exists(breedPath))
        {
            var json = File.ReadAllText(breedPath);
            var breeds = JsonSerializer.Deserialize<List<BreedData>>(json, _jsonOptions) ?? [];
            store.Breeds.Clear();
            store.Breeds.AddRange(breeds);
            Console.WriteLine($"Loaded {store.Breeds.Count} breeds");
        }

        var effectPath = Path.Combine(folderPath, "card_effects.json");
        if (File.Exists(effectPath))
        {
            var json = File.ReadAllText(effectPath);
            var effects = JsonSerializer.Deserialize<List<EffectData>>(json, _jsonOptions) ?? [];
            store.CardEffects.Clear();
            store.CardEffects.AddRange(effects);
            Console.WriteLine($"Loaded {store.CardEffects.Count} card effects");
        }
        
        IndexEffects();

        var breedWeightsPath = Path.Combine(folderPath, "breed_stats_weights.json");
        if (File.Exists(breedWeightsPath))
        {
            var json = File.ReadAllText(breedWeightsPath);
            var weights = JsonSerializer.Deserialize<List<BreedWeightsData>>(json, _jsonOptions) ?? [];
            store.BreedWeights.Clear();
            store.BreedWeights.AddRange(weights);
            Console.WriteLine($"Loaded {store.BreedWeights.Count} breed weights");
        }

        var fighterCardsPath = Path.Combine(folderPath, "fighter_cards.json");
        if (File.Exists(fighterCardsPath))
        {
            var json = File.ReadAllText(fighterCardsPath);
            var cards = JsonSerializer.Deserialize<List<FighterCardData>>(json, _jsonOptions) ?? [];
            store.FighterCards.Clear();
            store.FighterCards.AddRange(cards);
            Console.WriteLine($"Loaded {store.FighterCards.Count} fighter cards");
        }
    }

    public void SaveProjectFolder(string folderPath)
    {
        ComputeLinkedSpheres();

        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        var spheresPath = Path.Combine(folderPath, "spheres.json");

        var sphereBoardsJson = JsonSerializer.Serialize(store.SphereBoards, _jsonOptions);
        File.WriteAllText(sphereBoardsPath, sphereBoardsJson);

        var spheresJson = JsonSerializer.Serialize(store.Spheres, _jsonOptions);
        File.WriteAllText(spheresPath, spheresJson);
    }

    public void InitializeFighters()
    {
        store.Fighters.Clear();
        foreach (var board in store.SphereBoards)
        {
            var fighter = new Fighter(board, store);
            store.Fighters[board.Id] = fighter;
        }
    }

    public SphereBoardData CreateSphereBoard()
    {
        return new SphereBoardData
        {
            Id = GenerateSphereBoardId(),
            SeasonId = 1,
            BreedId = 1,
            InitialSpellIds = [31, 36, 34],
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
        store.Spheres.Add(newSphere);
        return newSphere;
    }

    public void RemoveSphere(int x, int y, int sphereBoardId)
    {
        store.Spheres.RemoveAll(sphere => sphere.SphereBoardId == sphereBoardId
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
        newEffect.Id = GenerateEffectId();
        sphere.Effects.Add(newEffect);
    }

    internal void ComputeLinkedSpheresForBoard(SphereBoardData sphereBoardData)
    {
        var spheres = store.Spheres
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
            store.Spheres.Add(startSphere);
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

            var teleportPos = (origin.TeleportXPosition, origin.TeleportYPosition);
            if (sphereByPosition.TryGetValue(teleportPos, out var teleportDest))
            {
                linkedSphereIds.Add(teleportDest.Id);
            }

            origin.LinkedSphereIds = linkedSphereIds.ToList();
        }
    }

    private void ComputeLinkedSpheres()
    {
        foreach (var sphereBoardData in store.SphereBoards)
        {
            ComputeLinkedSpheresForBoard(sphereBoardData);
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
               || sphere.FighterCardsIds.Count > 0
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

    private void IndexSpheres()
    {
        foreach (var sphere in store.Spheres)
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

        var duplicateSphereIds = store.Spheres
            .GroupBy(s => s.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        if (duplicateSphereIds.Count <= 0) 
            return;
        
        foreach (var sphere in store.Spheres)
        {
            if (!duplicateSphereIds.Contains(sphere.Id))
                continue;

            var newId = GenerateSphereId();
            sphere.Id = newId;
            foreach (var sphereEffect in sphere.Effects)
            {
                sphereEffect.ParentId = newId;
            }
        }

        foreach (var sphereBoard in store.SphereBoards)
        {
            ComputeLinkedSpheresForBoard(sphereBoard);
        }
    }

    private void IndexEffects()
    {
        var sphereEffectIds = store.Spheres.SelectMany(s => s.Effects).Select(e => e.Id).ToList();
        var cardEffectIdSet = store.CardEffects.Select(e => e.Id).ToHashSet();

        var duplicateEffectIds = sphereEffectIds
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Concat(sphereEffectIds.Intersect(cardEffectIdSet))
            .ToHashSet();

        if (duplicateEffectIds.Count <= 0)
            return;
        
        foreach (var effect in store.Spheres.SelectMany(s => s.Effects))
        {
            if (duplicateEffectIds.Contains(effect.Id))
                effect.Id = GenerateEffectId();
        }
    }

    private int GenerateSphereBoardId()
    {
        var newId = 1;
        while (store.SphereBoards.Any(sb => sb.Id == newId))
            newId++;
        return newId;
    }

    private int GenerateSphereId()
    {
        var newId = 1;
        var sphereIds = store.Spheres.Select(s => s.Id).ToHashSet();
        while (sphereIds.Contains(newId))
            newId++;
        return newId;
    }

    private int GenerateEffectId()
    {
        var newId = 1;
        var effects = store.CardEffects.Select(e => e.Id);
        var sphereEffects = store.Spheres.SelectMany(s => s.Effects).Select(e => e.Id);
        var effectIds = effects.Concat(sphereEffects).ToHashSet();
        while (effectIds.Contains(newId))
            newId++;
        return newId;
    }
}
