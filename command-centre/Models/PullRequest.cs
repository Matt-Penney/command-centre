namespace CommandCentre.Models;

public class PullRequest
{
    public string Title { get; set; } = "";
    public string Repo { get; set; } = "";
    public int Number { get; set; }
    public string Author { get; set; } = "";
    public string Url { get; set; } = "";
    public string Status { get; set; } = "open"; // open, closed, merged
    public bool HasFailedChecks { get; set; } = false;
    public string BuildStatus { get; set; } = "unknown"; // success, failure, pending, unknown
    public DateTime CreatedAt { get; set; }
}