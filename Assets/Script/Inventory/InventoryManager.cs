using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public interface IUsableItem
{
    public void Initialize(InventoryItem newInventoryItem);
    public void Deinitialize();
}

public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager Instance;

    public InventorySlot[] inventorySlots;
    public InventoryItem inventoryItemPrefab;

    public int selectedSlot = -1;

    public Transform itemHolder;
    public Transform handBoneTarget;

    private bool isThereSomethingInHand;

    public List<Item> items;

    public TextMeshProUGUI hintItemNameText;

    void Awake()
    {
        Instance = this;

        if (itemHolder == null || handBoneTarget == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        ChangeSelectedSlot(0);
    }

    private void Update()
    {
        //if (isThereSomethingInHand)
        //{
        //    handBoneTarget.position = itemHolder.GetChild(0).Find("HandPosition").position;
        //    handBoneTarget.rotation = itemHolder.GetChild(0).Find("HandPosition").rotation;
        //}

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            UiManager.Instance.ChangeCurrentPanel(Panels.Inventory);
        }

        if (UiManager.Instance.somePanelTurnedOn) return;

        if (Input.inputString != null)
        {
            bool isNumber = int.TryParse(Input.inputString, out int number);
            if (isNumber && number > 0 && number < 5)
            {
                ChangeSelectedSlot(number - 1);
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropItem(selectedSlot);
        }
    }

    void ChangeSelectedSlot(int newValue)
    {
        if(selectedSlot >= 0) inventorySlots[selectedSlot].Deselect();

        inventorySlots[newValue].Select();
        selectedSlot = newValue;

        if(inventorySlots[selectedSlot].currentInventoryItem)
        {
            //Armor tempArmor = inventorySlots[selectedSlot].currentInventoryItem.item as Armor;
            //Tool tempTool = inventorySlots[selectedSlot].currentInventoryItem.item as Tool;
            //if (tempArmor != null)
            //{
            //    ShowHint(inventorySlots[selectedSlot].currentInventoryItem.item.itemName + " (" + inventorySlots[selectedSlot].currentInventoryItem.customData + "/" + tempArmor.durability + ")");
            //}
            //else if (tempTool != null && tempTool.haveDurability)
            //{
            //    ShowHint(inventorySlots[selectedSlot].currentInventoryItem.item.itemName + " (" + inventorySlots[selectedSlot].currentInventoryItem.customData + "/" + tempTool.durability + ")");
            //}
            //else
            //{
            //    ShowHint(inventorySlots[selectedSlot].currentInventoryItem.item.itemName);
            //}
            ShowHint(inventorySlots[selectedSlot].currentInventoryItem.item.itemName);
        }

        UpdateHand();
    }

    public void UpdateHand()
    {
        InventoryItem inventoryItem = inventorySlots[selectedSlot].GetComponentInChildren<InventoryItem>();

        ClearHand();

        if(inventoryItem != null)
        {
            GameObject itemInHandModel = PhotonNetwork.Instantiate(inventoryItem.item.inHandModel.name, Vector3.zero, Quaternion.identity);

            photonView.RPC("InstantiateItemInHandModelRPC", RpcTarget.AllBuffered, itemInHandModel.GetComponent<PhotonView>().ViewID, itemHolder.GetComponent<PhotonView>().ViewID);

            itemInHandModel.transform.Find("Script").gameObject.SetActive(true);
            if (itemInHandModel.transform.Find("Script").TryGetComponent(out IUsableItem usableItem))
            {
                usableItem.Initialize(inventoryItem);
            }

            isThereSomethingInHand = true;
            handBoneTarget.parent.GetComponent<TwoBoneIKConstraint>().weight = 1f;
        }
    }


    [PunRPC]
    public void InstantiateItemInHandModelRPC(int itemInHandPhotonView, int itemHolderPhotonView, PhotonMessageInfo pmi)
    {
        if (PhotonView.Find(itemInHandPhotonView) == null) return;
        PhotonView.Find(itemInHandPhotonView).transform.SetParent(PhotonView.Find(itemHolderPhotonView).transform, false);
    }

    public void ClearHand()
    {
        if (itemHolder == null) return;

        if (itemHolder.childCount > 0) 
        {
            foreach (Transform child in itemHolder)
            {
                if(child.gameObject.TryGetComponent<IUsableItem>(out IUsableItem item))
                {
                    item.Deinitialize();
                }
                PhotonNetwork.Destroy(child.GetComponent<PhotonView>());
            }
        }

        isThereSomethingInHand = false;
        handBoneTarget.parent.GetComponent<TwoBoneIKConstraint>().weight = 0;
    }

    public bool AddItem(Item item, string customData)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventoryItem itemInSlot = inventorySlots[i].GetComponentInChildren<InventoryItem>();

            if (itemInSlot != null && itemInSlot.item == item && itemInSlot.count < item.maxInStack && itemInSlot.item.stackable && (inventorySlots[i].slotType == SlotType.All || inventorySlots[i].slotType == item.slotType))
            {
                items.Add(item);
                itemInSlot.count++;
                itemInSlot.RefreshCount();
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            InventorySlot slot = inventorySlots[i];
            InventoryItem itemInSlot = slot.GetComponentInChildren<InventoryItem>();

            if(itemInSlot == null && (slot.slotType == SlotType.All || slot.slotType == item.slotType))
            {
                items.Add(item);
                SpawnNewItem(item, slot, customData);
                if (selectedSlot == i) UpdateHand();
                return true;
            }
        }

        return false;
    }

    void SpawnNewItem(Item item, InventorySlot slot, string customData)
    {
        InventoryItem inventoryItem = Instantiate(inventoryItemPrefab, slot.transform);
        inventoryItem.InitializeItem(item, customData, slot);
    }

    public Item GetItem(int index, bool use)
    {
        InventoryItem itemInSlot = inventorySlots[index].GetComponentInChildren<InventoryItem>();

        if(itemInSlot != null)
        {
            if(use == true)
            {
                items.Remove(itemInSlot.item);
                itemInSlot.count--;
                if(itemInSlot.count <= 0)
                {
                    Destroy(itemInSlot.gameObject);
                    if(selectedSlot == index) { ClearHand(); }
                }
                else
                {
                    itemInSlot.RefreshCount();
                }
            }
            return itemInSlot.item;
        }
        return null;
    }

    public Item GetItem(Item item, bool use)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].GetComponentInChildren<InventoryItem>() != null && inventorySlots[i].GetComponentInChildren<InventoryItem>().item == item)
            {
                return GetItem(i, use);
            }
        }

        return null;
    }

    public void DropItem(int index)
    {
        InventoryItem inventoryItem = inventorySlots[index].GetComponentInChildren<InventoryItem>();
        if(inventoryItem != null) 
        {
            Vector3 position = itemHolder.position;
            if (itemHolder.childCount > 0) position = itemHolder.GetChild(0).position;

            GameObject handlerObject = PhotonNetwork.Instantiate(inventoryItem.item.handlerPrefab.name, position, Quaternion.identity);
            photonView.RPC("SpawnNewItemHandler", RpcTarget.AllBuffered, inventoryItem.customData, handlerObject.GetComponent<PhotonView>().ViewID);

            GetItem(index, true);
        }
    }

    [PunRPC]
    public void SpawnNewItemHandler(string customData, int handlerViewId)
    {
        ItemHandler handler = PhotonView.Find(handlerViewId).GetComponent<ItemHandler>();
        handler.customData = customData;
    }

    public void DropAllItems()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i].transform.childCount > 0)
            {
                DropItem(i);
            }
        }
    }

    public void ShowHint(string text)
    {
        StopAllCoroutines();
        StartCoroutine(ShowHintEnumerator(text));
    }

    private IEnumerator ShowHintEnumerator(string text)
    {
        hintItemNameText.text = text;
        yield return new WaitForSeconds(1);
        hintItemNameText.text = "";
    }
}