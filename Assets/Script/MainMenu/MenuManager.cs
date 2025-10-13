using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviourPunCallbacks
{
    public static MenuManager Instance;
    void Awake() { Instance = this; }

    [SerializeField] private TMP_InputField nickInputField;
    [SerializeField] private TextMeshProUGUI gameVersionText;
    [SerializeField] private Button joinButton;

    private void Start()
    {
        gameVersionText.text = Application.version;

    }

    private void Update()
    {
        joinButton.interactable = !(nickInputField.text == string.Empty);
    }

    public void StartButton()
    {
        PhotonNetwork.NickName = nickInputField.text;

        print("Connecting to server.");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}
