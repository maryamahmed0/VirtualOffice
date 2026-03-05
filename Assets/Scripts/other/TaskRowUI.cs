using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image statusDot;

    public void Bind(string title, string status)
    {
        if (titleText) titleText.text = title;

        // نخلي شكل الستيت موحّد
        string s = Normalize(status);
        if (statusText) statusText.text = s;

        var c = ColorForStatus(s);
        if (statusDot) statusDot.color = c;
        if (statusText) statusText.color = c;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "Todo";
        s = s.Trim();

        // توحيد الكتابة
        if (s.Equals("to do", System.StringComparison.OrdinalIgnoreCase)) return "Todo";
        if (s.Equals("doing", System.StringComparison.OrdinalIgnoreCase)) return "Doing";
        if (s.Equals("done", System.StringComparison.OrdinalIgnoreCase)) return "Done";
        return s;
    }

    private static Color ColorForStatus(string s)
    {
        // ألوان لطيفة ومش فاقعة
        if (s == "Done") return new Color(0.35f, 0.85f, 0.55f, 1f); // أخضر
        if (s == "Doing") return new Color(1.00f, 0.82f, 0.25f, 1f); // أصفر
        return new Color(0.95f, 0.45f, 0.45f, 1f);                  // أحمر (Todo)
    }
}