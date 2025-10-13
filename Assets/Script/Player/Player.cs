using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public static Player myPlayer;

    public Photon.Realtime.Player photonPlayer;
    public GameObject playerObject;
    public bool isAdmin;

    public static Player FindPlayer(Photon.Realtime.Player photonPlayerToFind)
    {
        Player playerToReturn = null;

        foreach(Player player in GameManager.Instance.playerList)
        {
            if(player.photonPlayer == photonPlayerToFind)
            {
                playerToReturn = player;
            }
        }

        return playerToReturn;
    }
}
