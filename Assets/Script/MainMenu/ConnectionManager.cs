using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConnectionManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI connectionStateText;

    private void Start()
    {
        PhotonNetwork.GameVersion = Application.version;
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;
    }

    private void Update()
    {
        connectionStateText.text = PhotonNetwork.NetworkClientState.ToString();
    }

    public override void OnConnectedToMaster()
    {
        print("Connected to server.");

        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        print("Dissconnected from server for reason: " + cause.ToString());
    }

    public override void OnJoinedRoom()
    {
        print("Joined room.");

        Settings.Instance.CloseSettingsMenu();
        PhotonNetwork.LoadLevel(1);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        print("Joining random failed with return code: " + returnCode);
    }

}
