using System.Text.Json.Serialization;
using SkillTreeEditor.Enums;

namespace SkillTreeEditor;

public class BreedData
{
    [JsonPropertyName("breed")] public string? Breed { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("baseEliteModeHP")] public int BaseEliteModeHp { get; set; }
    [JsonPropertyName("baseEliteModeAP")] public int BaseEliteModeAp { get; set; }
    [JsonPropertyName("baseEliteModeMP")] public int BaseEliteModeMp { get; set; }
    [JsonPropertyName("baseInitiative")] public int BaseInitiative { get; set; }
    [JsonPropertyName("baseCriticalRate")] public int BaseCriticalRate { get; set; }
    [JsonPropertyName("baseFumbleRate")] public int BaseFumbleRate { get; set; }
    [JsonPropertyName("baseKamasCost")] public int BaseKamasCost { get; set; }
    [JsonPropertyName("baseSummonCount")] public int BaseSummonCount { get; set; }
    [JsonPropertyName("damageType")] public string DamageType { get; set; } = string.Empty;
    [JsonPropertyName("closeCombatAPCost")] public int CloseCombatApCost { get; set; }
    [JsonPropertyName("closeCombatDamage")] public int CloseCombatDamage { get; set; }
    [JsonPropertyName("closeCombatCriticalDamage")] public int CloseCombatCriticalDamage { get; set; }
    [JsonPropertyName("baseEliteModeTackle")] public int BaseEliteModeTackle { get; set; }
    [JsonPropertyName("baseEliteModeDodge")] public int BaseEliteModeDodge { get; set; }
    [JsonPropertyName("baseEvolutionModeHP")] public int BaseEvolutionModeHp { get; set; }
    [JsonPropertyName("baseEvolutionModeAP")] public int BaseEvolutionModeAp { get; set; }
    [JsonPropertyName("baseEvolutionModeMP")] public int BaseEvolutionModeMp { get; set; }
    [JsonPropertyName("baseEvolutionModeTackle")] public int BaseEvolutionModeTackle { get; set; }
    [JsonPropertyName("baseEvolutionModeDodge")] public int BaseEvolutionModeDodge { get; set; }
    [JsonPropertyName("baseEvolutionModeDamagesInPercent")] public int BaseEvolutionModeDamagesInPercent { get; set; }
    [JsonPropertyName("baseEvolutionModeResistanceInPercent")] public int BaseEvolutionModeResistanceInPercent { get; set; }
    [JsonPropertyName("baseEvolutionModeHeal")] public int BaseEvolutionModeHeal { get; set; }
    [JsonPropertyName("baseEvolutionModeSummonMasteryDamages")] public int BaseEvolutionModeSummonMasteryDamages { get; set; }
    [JsonPropertyName("baseEvolutionModeSummonMasteryResistance")] public int BaseEvolutionModeSummonMasteryResistance { get; set; }
    [JsonPropertyName("baseEvolutionModeSummonMasteryCritical")] public int BaseEvolutionModeSummonMasteryCritical { get; set; }
    [JsonPropertyName("baseEvolutionModeSummonMasteryBlock")] public int BaseEvolutionModeSummonMasteryBlock { get; set; }
    [JsonPropertyName("baseEvolutionModeSummonMasteryHP")] public int BaseEvolutionModeSummonMasteryHp { get; set; }
}
