using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;
using UnityEngine.Windows;
using UnityEngine.SceneManagement;

public class ScoreboardItem : MonoBehaviourPunCallbacks
{
    public TMP_Text usernameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;

    Player player;
    int maxPoints;
    public void Initalize(Player player, int maxPoints)
    {
        this.maxPoints = maxPoints;
        this.player = player;
        usernameText.text = player.NickName;
        UpdateStats();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if(targetPlayer == player)
        {
            if(changedProps.ContainsKey("kills") || changedProps.ContainsKey("deaths"))
            {
                UpdateStats();
            }
        }
    }

    void UpdateStats()
    {
        if(player.CustomProperties.TryGetValue("kills", out object kills))
        {
            killsText.text = kills.ToString();

            if(Int32.Parse(kills.ToString()) == maxPoints) {
                PhotonNetwork.DestroyAll();
                SceneManager.LoadScene(0);
            }
        }

        if (player.CustomProperties.TryGetValue("deaths", out object deaths))
        {
            deathsText.text = deaths.ToString();
        }
    }
}
