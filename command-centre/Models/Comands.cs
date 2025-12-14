namespace CommandCentre.Models;

public class CommandInfo
{
    public string fileName { get; set; }
    public string arguments { get; set; }
    public bool useShellExecute { get; set; }
    public bool redirectStandardOutput { get; set; }
    public bool redirectStandardError { get; set; }
    public bool createNoWindow { get; set; }
    public string? verb { get; set; } = null;
}