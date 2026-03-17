using UnityEngine;
using System.Runtime.InteropServices;

public class WebAudioEnabler : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ForceUnlockWebAudio();
#endif

    public void OnEnableAudioClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try 
        {
            ForceUnlockWebAudio();
            Debug.Log("[WebAudio] Enable Button Clicked!");
        }
        catch (System.Exception e) 
        {
            Debug.LogWarning("[WebAudio] Error clicking enable: " + e.Message);
        }
#else
        Debug.Log("Audio Enabler only needed in WebGL.");
#endif
        // اختياري: ممكن تخفي الزرار بعد ما اللاعب يدوس عليه
        gameObject.SetActive(false);
    }
}