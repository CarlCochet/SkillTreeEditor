using System.Text.Json.Serialization;
using SkillTreeEditor.Enums;

namespace SkillTreeEditor;

public class EffectData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("actionId")] public int ActionId { get; set; }
    [JsonPropertyName("parentId")] public int ParentId { get; set; }
    [JsonPropertyName("parentType")] public string? ParentType { get; set; }
    [JsonPropertyName("areaShape")] public int AreaShape { get; set; }
    [JsonPropertyName("areaOrderingMethod")] public int AreaOrderingMethod { get; set; }
    [JsonPropertyName("targetTriggerSelf")] public bool TargetTriggerSelf { get; set; }
    [JsonPropertyName("singleTarget")] public bool SingleTarget { get; set; }
    [JsonPropertyName("params")] public List<double> Params { get; set; } = [];
    [JsonPropertyName("triggersBefore")] public List<int> TriggersBefore { get; set; } = [];
    [JsonPropertyName("triggersAfter")] public List<int> TriggersAfter { get; set; } = [];
    [JsonPropertyName("endTriggers")] public List<int> EndTriggers { get; set; } = [];
    [JsonPropertyName("serverSideTriggers")] public List<int> ServerSideTriggers { get; set; } = [];
    [JsonPropertyName("areaSize")] public List<int> AreaSize { get; set; } = [];
    [JsonPropertyName("duration")] public List<int> Duration { get; set; } = [];
    [JsonPropertyName("targets")] public List<long> Targets { get; set; } = [];
    [JsonPropertyName("triggeredWithDuration")] public bool TriggeredWithDuration { get; set; }
    [JsonPropertyName("appliedIfTargetValid")] public bool AppliedIfTargetValid { get; set; }
    [JsonPropertyName("critical")] public bool Critical { get; set; }
    [JsonPropertyName("personal")] public bool Personal { get; set; }
    [JsonPropertyName("affectedByLocalisation")] public bool AffectedByLocalisation { get; set; }
    
    public string ActionTypeName =>
        Enum.IsDefined(typeof(ActionType), ActionId)
            ? ((ActionType)ActionId).ToString()
            : $"Unknown ({ActionId})";
}
