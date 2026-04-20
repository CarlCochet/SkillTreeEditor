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
    [JsonPropertyName("linkedSphereIds")] public List<int> LinkedSphereIds { get; set; } = [];

    public void Reset()
    {
        XpNumber = 0;
        SpellId = 0;
        Effects.Clear();
        FighterCardListId = 0;
        BarrierCoachCards.Clear();
        TeleportXPosition = 0;
        TeleportYPosition = 0;
        Impassable = false;
        LinkedSphereIds.Clear();
    }
    
    public SphereData Copy()
    {
        return new SphereData
        {
            Id = Id,
            SphereBoardId = SphereBoardId,
            XpNumber = XpNumber,
            SpellId = SpellId,
            FighterCardListId = FighterCardListId,
            BarrierCoachCards = [.. BarrierCoachCards],
            TeleportXPosition = TeleportXPosition,
            TeleportYPosition = TeleportYPosition,
            YPosition = YPosition,
            Impassable = Impassable,
            XPosition = XPosition,
            LinkedSphereIds = [.. LinkedSphereIds],
            Effects = Effects.Select(e => e.Copy()).ToList()
        };
    }
}
