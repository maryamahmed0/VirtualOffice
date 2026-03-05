using UnityEngine;
using System.Runtime.InteropServices;

public static class WebVoiceGate
{
    public static bool HasUserGesture { get; private set; }
    public static bool VoiceStarted { get; set; } 

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void WebGL_ResumeAudioContext();
#endif

    public static void MarkUserGesture()
    {
        HasUserGesture = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGL_ResumeAudioContext();
#endif
        Debug.Log("[WEB] User gesture captured ✅");
    }
}