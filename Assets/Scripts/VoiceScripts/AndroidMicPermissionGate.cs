using UnityEngine;
using UnityEngine.Android;

public static class AndroidMicPermissionGate
{
    public static bool HasMicPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return true;
#endif
    }

    public static void RequestMicPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("[ANDROID PERM] Requesting Microphone permission...");
            Permission.RequestUserPermission(Permission.Microphone);
        }
        else
        {
            Debug.Log("[ANDROID PERM] Microphone permission already granted.");
        }
#endif
    }
}