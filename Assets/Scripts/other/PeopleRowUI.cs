using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PeopleRowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text zoneText;
    [SerializeField] private Button callButton;

    private ulong targetClientId;

    public void Bind(PresenceEntry p, ulong localId)
    {
        targetClientId = p.ClientId;

        if (nameText) nameText.text = p.Name.ToString();
        if (zoneText) zoneText.text = ZoneToString((NetRoomState.Zone)p.Zone);

        bool isMe = (targetClientId == localId);
        bool localInMeeting = false;
        bool targetInMeeting = ((NetRoomState.Zone)p.Zone) == NetRoomState.Zone.Meeting;

        var lp = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (lp != null)
        {
            var netRoom = lp.GetComponent<NetRoomState>();
            if (netRoom != null)
                localInMeeting = netRoom.GetZone() == NetRoomState.Zone.Meeting;
        }

        bool canShowCall = !isMe && !localInMeeting && !targetInMeeting;

        if (callButton)
        {
            callButton.gameObject.SetActive(canShowCall);
            callButton.onClick.RemoveAllListeners();

            if (canShowCall)
                callButton.onClick.AddListener(TryCall);
        }
    }

    private void TryCall()
    {
        var lp = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (lp == null) return;

        var netRoom = lp.GetComponent<NetRoomState>();
        if (netRoom != null && netRoom.GetZone() == NetRoomState.Zone.Meeting)
            return;

        var call = lp.GetComponent<CallController>();
        if (call == null) return;
        if (call.State != CallController.CallState.Idle) return;

        call.RequestCall(targetClientId);
    }

    private static string ZoneToString(NetRoomState.Zone z) => z switch
    {
        NetRoomState.Zone.Lobby => "Lobby",
        NetRoomState.Zone.TeamRoom => "TeamRoom",
        NetRoomState.Zone.Meeting => "Meeting",
        _ => z.ToString()
    };

}