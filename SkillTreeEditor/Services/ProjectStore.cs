using SkillTreeEditor.Data;

namespace SkillTreeEditor.Services;

public class ProjectStore
{
    public List<SphereBoardData> SphereBoards { get; } = [];
    public List<SphereData> Spheres { get; } = [];
    public List<SpellData> SpellCards { get; } = [];
    public List<BreedData> Breeds { get; } = [];
    public List<EffectData> CardEffects { get; } = [];
    public List<BreedWeightsData> BreedWeights { get; } = [];
    public List<FighterCardData> FighterCards { get; } = [];
    public Dictionary<int, Fighter> Fighters { get; } = [];
}
