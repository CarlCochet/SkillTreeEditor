using System.Text.Json.Serialization;

namespace SkillTreeEditor;

public class SphereData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("sphereBoardId")] public int SphereBoardId { get; set; }
    [JsonPropertyName("xpNumber")] public int XpNumber { get; set; }
    [JsonPropertyName("spellId")] public int SpellId { get; set; }
    [JsonPropertyName("effects")] public List<EffectData> Effects { get; set; } = [];
    [JsonPropertyName("fighterCardListId")] public int FighterCardListId { get; set; }
    [JsonPropertyName("barrierCoachCards")] public List<int> BarrierCoachCards { get; set; } = [];
    [JsonPropertyName("teleportXPosition")] public int TeleportXPosition { get; set; }
    [JsonPropertyName("teleportYPosition")] public int TeleportYPosition { get; set; }
    [JsonPropertyName("yposition")] public int YPosition { get; set; }
    [JsonPropertyName("impassable")] public bool Impassable { get; set; }
    [JsonPropertyName("xposition")] public int XPosition { get; set; }
}
