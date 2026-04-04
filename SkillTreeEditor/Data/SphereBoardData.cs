using System.Text.Json.Serialization;

namespace SkillTreeEditor;

public class SphereBoardData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("seasonId")] public int SeasonId { get; set; }
    [JsonPropertyName("breedId")] public int BreedId { get; set; }
    [JsonPropertyName("fighterCardListId")] public int FighterCardListId { get; set; }
    [JsonPropertyName("initialSpellIds")] public List<int> InitialSpellIds { get; set; }
    [JsonPropertyName("startX")] public int StartX { get; set; }
    [JsonPropertyName("startY")] public int StartY { get; set; }
}
