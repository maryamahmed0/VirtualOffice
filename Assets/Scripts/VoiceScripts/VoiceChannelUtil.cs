using System.Text;
using UnityEngine;

public static class VoiceChannelUtil
{
    // Vivox channel names لازم تكون clean: حروف/أرقام/underscore غالبًا
    public static string Build(string org, string joinCode)
    {
        org = Sanitize(org);
        joinCode = Sanitize(joinCode);

        if (string.IsNullOrEmpty(org)) org = "ORG";
        if (string.IsNullOrEmpty(joinCode)) joinCode = "ROOM";

        // مثال: ORG_ABC123
        return $"{org}_{joinCode}";
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s.Trim())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            else if (c == '_' || c == '-') sb.Append('_');
        }

        // قللي الطول احتياط
        string clean = sb.ToString();
        if (clean.Length > 24) clean = clean.Substring(0, 24);
        return clean;
    }
}