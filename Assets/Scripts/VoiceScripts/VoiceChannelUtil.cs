using System.Text;
using UnityEngine;

public static class VoiceChannelUtil
{
    public static string Build(string org, string joinCode)
    {
        org = Sanitize(org);
        joinCode = Sanitize(joinCode);

        if (string.IsNullOrEmpty(org)) org = "ORG";
        if (string.IsNullOrEmpty(joinCode)) joinCode = "ROOM";

        // 👉 استخدمنا Dash هنا
        return $"{org}-{joinCode}";
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s.Trim())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            // 👉 حولنا أي مسافة أو Underscore لـ Dash مسموح بيها
            else if (c == '_' || c == '-') sb.Append('-');
        }

        string clean = sb.ToString();
        if (clean.Length > 24) clean = clean.Substring(0, 24);
        return clean;
    }
}