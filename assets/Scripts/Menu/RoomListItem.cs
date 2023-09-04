using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    public RoomInfo roomInfo;
    public void SetUp(RoomInfo _roomInfo)
    {
        if (_roomInfo != null)
        {
            roomInfo = _roomInfo;
            text.text = _roomInfo.Name;
        }
    }

    public void OnClick()
    {
        Server.Instance.JoinRoom(roomInfo);
    }
         
}
