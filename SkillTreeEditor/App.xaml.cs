using System.Configuration;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using Application = System.Windows.Application;

namespace SkillTreeEditor;

public partial class App : Application
{
    private List<SphereBoardData> _sphereBoards = [];
    private List<SphereData> _spheres = [];
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public void LoadProjectFolder(string folderPath)
    {
        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        if (File.Exists(sphereBoardsPath))
        {
            var json = File.ReadAllText(sphereBoardsPath);
            _sphereBoards = JsonSerializer.Deserialize<List<SphereBoardData>>(json, _jsonOptions) ?? [];
        }

        var spheresPath = Path.Combine(folderPath, "spheres.json");
        if (File.Exists(spheresPath))
        {
            var json = File.ReadAllText(spheresPath);
            _spheres = JsonSerializer.Deserialize<List<SphereData>>(json, _jsonOptions) ?? [];
        }
    }
    
    public void SaveProjectFolder(string folderPath)
    {
        var sphereBoardsPath = Path.Combine(folderPath, "sphere_boards.json");
        var spheresPath = Path.Combine(folderPath, "spheres.json");
        
        var sphereBoardsJson = JsonSerializer.Serialize(_sphereBoards, _jsonOptions);
        File.WriteAllText(sphereBoardsPath, sphereBoardsJson);
        
        var spheresJson = JsonSerializer.Serialize(_spheres, _jsonOptions);
        File.WriteAllText(spheresPath, spheresJson);
    }
}
