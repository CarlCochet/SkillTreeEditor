namespace SkillTreeEditor;

public class Parsing
{
    public static int ParseIntOrDefault(string? text)
    {
        return int.TryParse(text, out var value) ? value : 0;
    }
}