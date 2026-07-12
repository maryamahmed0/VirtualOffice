public static class AuthBridge
{
    public static string Token { get; set; }
    public static string DisplayName { get; set; }
    public static string OrgCode { get; set; }
    public static string TeamId { get; set; }
    public static int TeamSize { get; set; }
    public static string TeamName { get; set; }
    public static string Gender { get; set; }
    public static string Role { get; set; }
    public static bool IsReady { get; set; }
    public static TeamService.TeamItem[] Teams { get; set; }
    public static string SelectedTeamId { get; set; }
    public static AuthService.WorkspaceItem[] Workspaces { get; set; }

    public static bool IsManager => Role == "Manager";

    public static void Clear()
    {
        Token = DisplayName = OrgCode = TeamId = Gender = Role = "";
        TeamSize = 0;
        IsReady = false;
        Workspaces = null;
        Teams = null;
    }
}