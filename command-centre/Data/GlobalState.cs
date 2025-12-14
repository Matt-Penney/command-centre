using System;
namespace CommandCentre.Data;

public class GlobalState
{
  public bool? IsGitHubAuthenticated { get; set; } = null;
  public event Action OnChange;

  public void SetGitHubAuthenticated(bool isAuthenticated)
  {
    IsGitHubAuthenticated = isAuthenticated;
    NotifyStateChanged();
  }

  private void NotifyStateChanged() => OnChange?.Invoke();
}