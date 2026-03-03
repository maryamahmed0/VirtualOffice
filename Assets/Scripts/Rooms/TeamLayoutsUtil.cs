public static class TeamLayoutUtil
{
    public static int LayoutFromTeamSize(int teamSize)
    {
        if (teamSize <= 8) return 0;      // Small
        if (teamSize <= 12) return 1;     // Medium
        return 2;                         // Large
    }
}