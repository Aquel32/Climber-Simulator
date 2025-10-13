using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    private void Awake() { Instance = this; }

    public List<Player> playerList = new List<Player>();

    public Transform spawnsParent;

    [SerializeField] private GameObject playerScriptsHandler;

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
            return;
        }

        photonView.RPC("CreateNewPlayerRPC", RpcTarget.AllBuffered);

        SpawnPlayer(Player.myPlayer);
    }

    [PunRPC]
    public void CreateNewPlayerRPC(PhotonMessageInfo photonMessageInfo)
    {
        Photon.Realtime.Player photonPlayer = photonMessageInfo.Sender;

        Player newPlayer = new Player();
        playerList.Add(newPlayer);

        newPlayer.photonPlayer = photonPlayer;
        newPlayer.isAdmin = PhotonNetwork.IsMasterClient;

        if (PhotonNetwork.NickName == newPlayer.photonPlayer.NickName)
        {
            Player.myPlayer = newPlayer;
        }
    }

    public void SpawnPlayer(Player playerToSpawn)
    {
        GameObject newPlayerObject = PhotonNetwork.Instantiate("Player", GetRandomSpawn(), Quaternion.identity);

        playerScriptsHandler.SetActive(true);
        newPlayerObject.GetComponent<PlayerInitializer>().enabled = true;
        //UiManager.Instance.ChangeCurrentPanel(Panels.None);

        photonView.RPC("SpawnPlayerRPC", RpcTarget.AllBuffered, newPlayerObject.GetComponent<PhotonView>().ViewID);
    }
    [PunRPC]
    public void SpawnPlayerRPC(int objectViewID, PhotonMessageInfo pmi)
    {
        GameObject playerToSpawnObject = PhotonView.Find(objectViewID).gameObject;
        Player playerToSpawn = Player.FindPlayer(pmi.Sender);

        playerToSpawnObject.name = "Player " + playerToSpawn.photonPlayer.NickName;
        playerToSpawn.playerObject = playerToSpawnObject;

        playerToSpawnObject.GetComponent<PlayerReference>().referencedPlayer = playerToSpawn;
    }

    public void DespawnPlayer(Player playerToDespawn)
    {
        if(Player.myPlayer == playerToDespawn) playerScriptsHandler.SetActive(false);
        photonView.RPC("DespawnPlayerRPC", RpcTarget.AllBuffered, playerToDespawn.playerObject.GetComponent<PhotonView>().ViewID);
    }
    [PunRPC]
    public void DespawnPlayerRPC(int goViewId)
    {
        if(PhotonView.Find(goViewId) != null)
        {
            Destroy(PhotonView.Find(goViewId).gameObject);
        }
    }

    public Vector3 GetRandomSpawn()
    {
        int generatedIndex = Random.Range(0, spawnsParent.childCount);
        return spawnsParent.GetChild(generatedIndex).position;
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        DespawnPlayer(Player.FindPlayer(otherPlayer));
        playerList.Remove(Player.FindPlayer(otherPlayer));
    }
}
