using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Items/Item")]
public class Item : ScriptableObject
{
    [Header("General")]
    public string itemName;
    public Sprite image;
    public GameObject handlerPrefab;
    public GameObject inHandModel;
    public bool stackable = true;
    public string defaultData;
    public int maxInStack = 4;
    public SlotType slotType;
}