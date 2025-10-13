using Photon.Pun;
using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance;
    private void Awake() { Instance = this; }

    public Panels currentPanel;

    public GameObject pauseMenu, inventoryPanel;
    public GameObject crosshair;

    public bool somePanelTurnedOn;

    public void ChangeCurrentPanel(Panels newPanel)
    {
        if (newPanel == currentPanel) newPanel = Panels.None;

        currentPanel = newPanel;

        pauseMenu.SetActive(newPanel == Panels.Pause);
        inventoryPanel.SetActive(newPanel == Panels.Inventory);

        ChangeCursorState(newPanel != Panels.None);
        CrosshairState(newPanel == Panels.None);

        somePanelTurnedOn = (newPanel != Panels.None);

        PlayerMovement.Instance.canMove = newPanel == Panels.None;
        PlayerCamera.Instance.canUseMouse = newPanel == Panels.None;
    }

    public void ChangeCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = newState;
    }

    public void CrosshairState(bool newState)
    {
        crosshair.SetActive(newState);
    }
}

public enum Panels
{
    None,
    Pause,
    Inventory,
}