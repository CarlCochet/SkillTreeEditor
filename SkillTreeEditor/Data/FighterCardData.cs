using System.Text.Json.Serialization;

namespace SkillTreeEditor;

public class FighterCardData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("effects")] public int[] Effects { get; set; } = [];
    [JsonPropertyName("castMinInterval")] public int CastMinInterval { get; set; }
    [JsonPropertyName("castGlobalCooldown")] public int CastGlobalCooldown { get; set; }
    [JsonPropertyName("castMaxPerTurn")] public int CastMaxPerTurn { get; set; }
    [JsonPropertyName("castMaxPerTarget")] public int CastMaxPerTarget { get; set; }
    [JsonPropertyName("castMaxPerTargetPerTurn")] public int CastMaxPerTargetPerTurn { get; set; }
    [JsonPropertyName("type")] public int Type { get; set; }
    [JsonPropertyName("weaponSubType")] public int WeaponSubType { get; set; }
    [JsonPropertyName("animId")] public int AnimId { get; set; }
    [JsonPropertyName("actionPoints")] public int ActionPoints { get; set; }
    [JsonPropertyName("rangeMin")] public int RangeMin { get; set; }
    [JsonPropertyName("rangeMax")] public int RangeMax { get; set; }
    [JsonPropertyName("onlyLine")] public bool OnlyLine { get; set; }
    [JsonPropertyName("testLos")] public bool TestLos { get; set; }
    [JsonPropertyName("testFree")] public bool TestFree { get; set; }
    [JsonPropertyName("generateEffectsText")] public bool GenerateEffectsText { get; set; }
    [JsonPropertyName("allowCarried")] public bool AllowCarried { get; set; }
    [JsonPropertyName("allowCarrying")] public bool AllowCarrying { get; set; }
}
