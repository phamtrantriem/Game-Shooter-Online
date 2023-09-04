using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using TMPro;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text text;
    Player player;
    public void SetUp(Player _player)
    {
        player = _player;
        text.text = _player.NickName;
        if(_player.NickName == PhotonNetwork.NickName)
        {
            text.color = Color.green;
        }
        if(_player.IsMasterClient)
        {
            text.color = Color.red;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(player == otherPlayer)
        {
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);
    }
}
