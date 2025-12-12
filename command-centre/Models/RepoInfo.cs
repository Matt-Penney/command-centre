namespace CommandCentre.Models;

public class RepoInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Type { get; set; } = "code"; // "code" or "wsl" or "studio"
}