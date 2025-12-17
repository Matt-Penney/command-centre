namespace CommandCentre.Models;

public class RepoDTO
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; } // "code" or "wsl" or "studio"
}

public class Repo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; }
    public bool IsActive { get; set; }
    public string Owner { get; set; }
    public string Description { get; set; }
    public string Language { get; set; }
}