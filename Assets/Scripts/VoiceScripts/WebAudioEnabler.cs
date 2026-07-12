using UnityEngine;
using System.Runtime.InteropServices;

public class WebAudioEnabler : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ForceUnlockWebAudio();

   
    [DllImport("__Internal")]
    private static extern void StartPeriodicAudioUnlock();
#endif

    public void OnEnableAudioClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            ForceUnlockWebAudio();
            StartPeriodicAudioUnlock();
            Debug.Log("[WebAudio] Enable Button Clicked!");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[WebAudio] Error: " + e.Message);
        }
#else
        Debug.Log("Audio Enabler only needed in WebGL.");
#endif
        gameObject.SetActive(false);
    }
}