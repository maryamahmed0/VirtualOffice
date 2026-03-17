using UnityEngine;
using System.Runtime.InteropServices;

public static class WebVoiceGate
{
    public static bool HasUserGesture { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ResumeWebAudioContext();
#endif

    // هننادي على الدالة دي في أي زرار اللاعب بيدوس عليه له علاقة بالصوت
    public static void MarkUserGesture()
    {
        HasUserGesture = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        try 
        {
            ResumeWebAudioContext();
            Debug.Log("[WEB] WebAudio resumed from WebVoiceGate ✅");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[WEB] Failed to resume WebAudio: " + e.Message);
        }
#endif
    }
}