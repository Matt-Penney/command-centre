using System;
namespace CommandCentre.Data;

public class GlobalState
{
  public bool? IsGitHubAuthenticated { get; set; } = null;

  // ToDo: move SideBar.razor SelectedTab state to here so it persists across pages
  public string SelectedNavTab { get; set; } = "Home";

  public event Action OnChange;

  public void SetGitHubAuthenticated(bool isAuthenticated)
  {
    IsGitHubAuthenticated = isAuthenticated;
    NotifyStateChanged();
  }

  private void NotifyStateChanged() => OnChange?.Invoke();
}