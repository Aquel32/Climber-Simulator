using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Image image;
    public Color selectedColor, notSelectedColor;

    public SlotType slotType;

    public InventoryItem currentInventoryItem;

    void Awake()
    {
        Deselect();
    }

    public void Select()
    {
        image.color = selectedColor;
    }

    public void Deselect()
    {
        image.color = notSelectedColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItem inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();

        if (inventoryItem == null || inventoryItem.item.slotType != slotType && slotType != SlotType.All) return;

        if (transform.childCount == 0)
        {
            inventoryItem.parentAfterDrag = transform;
            currentInventoryItem = inventoryItem;
            currentInventoryItem.currentSlot = this;
        }
        else if (GetComponentInChildren<InventoryItem>().item != null && GetComponentInChildren<InventoryItem>().item == inventoryItem.item)
        {
            int totalCount = GetComponentInChildren<InventoryItem>().count + inventoryItem.count;

            if (totalCount <= inventoryItem.item.maxInStack) 
            {
                Destroy(inventoryItem.gameObject);
                GetComponentInChildren<InventoryItem>().count += inventoryItem.count;
                GetComponentInChildren<InventoryItem>().RefreshCount();
            }
            else
            {
                inventoryItem.count = inventoryItem.item.maxInStack;
                inventoryItem.RefreshCount();
                GetComponentInChildren<InventoryItem>().count = totalCount % inventoryItem.item.maxInStack;
                GetComponentInChildren<InventoryItem>().RefreshCount();
            }
        }
    }

    
}

public enum SlotType { All, Normal, Head, Body, Legs, Shoes }