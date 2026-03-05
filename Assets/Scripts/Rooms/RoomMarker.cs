using UnityEngine;

public class RoomMarker : MonoBehaviour
{
    [Header("Room Identity")]
    public string roomId = "main";
    public RoomType roomType = RoomType.None;

    [Header("Optional Metadata")]
    public string teamId = "";
    public bool enableRoomVoice = false;

    public string GetDebugName()
    {
        if (roomType == RoomType.Team && !string.IsNullOrWhiteSpace(teamId))
            return $"{roomType}:{teamId}";

        return $"{roomType}:{roomId}";
    }
}