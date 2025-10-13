using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipment : MonoBehaviourPunCallbacks, IUsableItem
{
    private InventoryItem inventoryItem;
    public Armor item;

    void Awake()
    {
        if (photonView.IsMine == false)
        {
            this.enabled = false;
        }
    }

    public void Initialize(InventoryItem newInventoryItem)
    {
        inventoryItem = newInventoryItem;
    }

    public void Update()
    {
        if (UiManager.Instance.somePanelTurnedOn) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Equip();
        }
    }

    public void Equip()
    {
        if (ArmorSystem.Instance.inventorySlots[(int)(item.slotType) - 2].currentInventoryItem != null) return;

        inventoryItem.transform.SetParent(ArmorSystem.Instance.inventorySlots[(int)(item.slotType) - 2].transform);
        ArmorSystem.Instance.inventorySlots[(int)(item.slotType) - 2].currentInventoryItem = inventoryItem;
        inventoryItem.currentSlot = ArmorSystem.Instance.inventorySlots[(int)(item.slotType) - 2];
        ArmorSystem.Instance.LookForChanges();

        InventoryManager.Instance.UpdateHand();
    }

    public void Deinitialize()
    {

    }
}
