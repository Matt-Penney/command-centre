namespace CommandCentre.Models;

public class UtilityScript
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Command { get; set; } = "";
    public string Type { get; set; } = "bash"; // bash, powershell, python, etc
    public bool RequiresConfirmation { get; set; } = false;
}