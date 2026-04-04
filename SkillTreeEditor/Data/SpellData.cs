using System.Text.Json.Serialization;

namespace SkillTreeEditor;

public class SpellData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("linkedToSpell")] public int LinkedToSpell { get; set; }
    [JsonPropertyName("apCost")] public int ApCost { get; set; }
    [JsonPropertyName("minRange")] public int MinRange { get; set; }
    [JsonPropertyName("maxRange")] public int MaxRange { get; set; }
    [JsonPropertyName("isOnlyLine")] public bool IsOnlyLine { get; set; }
    [JsonPropertyName("noRangeBonus")] public bool NoRangeBonus { get; set; }
    [JsonPropertyName("isTestLOS")] public bool IsTestLoS { get; set; }
    [JsonPropertyName("isTestFreeCell")] public bool IsTestFreeCell { get; set; }
    [JsonPropertyName("useAutoDescription")] public bool UseAutoDescription { get; set; }
    [JsonPropertyName("useDisplayCriteriaAsCastCriteria")] public bool UseDisplayCriteriaAsCastCriteria { get; set; }
    [JsonPropertyName("castMinInterval")] public int CastMinInterval { get; set; }
    [JsonPropertyName("castGlobalCooldown")] public int CastGlobalCooldown { get; set; }
    [JsonPropertyName("castMaxPerTurn")] public int CastMaxPerTurn { get; set; }
    [JsonPropertyName("castMaxPerTarget")] public int CastMaxPerTarget { get; set; }
    [JsonPropertyName("castMaxPerTargetPerTurn")] public int CastMaxPerTargetPerTurn { get; set; }
    [JsonPropertyName("categoryId")] public Enums.Breeds Category { get; set; }
    [JsonPropertyName("scriptId")] public int ScriptId { get; set; }
    [JsonPropertyName("aiTargetId")] public int AiTargetId { get; set; }
    [JsonPropertyName("budget")] public int Budget { get; set; }
    [JsonPropertyName("criteria")] public string Criteria { get; set; } = string.Empty;
    [JsonPropertyName("effectDisplayCriteria")] public long[] EffectDisplayCriteria { get; set; } = [];
    [JsonPropertyName("effects")] public int[] Effects { get; set; } = [];
}
