using UnityEngine;
using Photon.Pun;

public class RoomStatusWatcher : MonoBehaviourPunCallbacks
{
    public override void OnJoinedRoom()
    {
        UIReferenceManager.Instance.isInRoom = true;
        Debug.Log("[RoomStatusWatcher] Entrato nella stanza, interazione UI abilitata.");
    }

    public override void OnLeftRoom()
    {
        UIReferenceManager.Instance.isInRoom = false;
        Debug.Log("[RoomStatusWatcher] Uscito dalla stanza, interazione UI disabilitata.");
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        UIReferenceManager.Instance.isInRoom = false;
        Debug.Log("[RoomStatusWatcher] Disconnesso da Photon, flag resettata.");
    }
}
