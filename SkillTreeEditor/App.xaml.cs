namespace SkillTreeEditor;

public partial class App : System.Windows.Application
{
    public Services.ProjectStore Store { get; } = new();
    public Services.ProjectService Service { get; }

    public App()
    {
        Service = new Services.ProjectService(Store);
    }
}
