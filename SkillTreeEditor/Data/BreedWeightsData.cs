using System.Text.Json.Serialization;
using SkillTreeEditor.Enums;

namespace SkillTreeEditor;

public class BreedWeightsData
{
    [JsonPropertyName("breedId")] public Breeds Breed { get; set; }
    [JsonPropertyName("hpWeight")] public float HpWeight { get; set; }
    [JsonPropertyName("rangeWeight")] public float RangeWeight { get; set; }
    [JsonPropertyName("resPercentFireWeight")] public float ResPercentFireWeight { get; set; }
    [JsonPropertyName("resPercentEarthWeight")] public float ResPercentEarthWeight { get; set; }
    [JsonPropertyName("resPercentWaterWeight")] public float ResPercentWaterWeight { get; set; }
    [JsonPropertyName("resPercentWindWeight")] public float ResPercentWindWeight { get; set; }
    [JsonPropertyName("resPercentAllWeight")] public float ResPercentAllWeight { get; set; }
    [JsonPropertyName("dmgPercentFireWeight")] public float DmgPercentFireWeight { get; set; }
    [JsonPropertyName("dmgPercentEarthWeight")] public float DmgPercentEarthWeight { get; set; }
    [JsonPropertyName("dmgPercentWaterWeight")] public float DmgPercentWaterWeight { get; set; }
    [JsonPropertyName("dmgPercentWindWeight")] public float DmgPercentWindWeight { get; set; }
    [JsonPropertyName("dmgPercentAllWeight")] public float DmgPercentAllWeight { get; set; }
    [JsonPropertyName("ccWeight")] public float CcWeight { get; set; }
    [JsonPropertyName("healWeight")] public float HealWeight { get; set; }
    [JsonPropertyName("tackleWeight")] public float TackleWeight { get; set; }
    [JsonPropertyName("dodgeWeight")] public float DodgeWeight { get; set; }
    [JsonPropertyName("summonNumberWeight")] public float SummonNumberWeight { get; set; }
    [JsonPropertyName("summonDmgWeight")] public float SummonDmgWeight { get; set; }
    [JsonPropertyName("summonResWeight")] public float SummonResWeight { get; set; }
    [JsonPropertyName("summonCcWeight")] public float SummonCcWeight { get; set; }
    [JsonPropertyName("summonTackleWeight")] public float SummonTackleWeight { get; set; }
    [JsonPropertyName("summonHpWeight")] public float SummonHpWeight { get; set; }
}
