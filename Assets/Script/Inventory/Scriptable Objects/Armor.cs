using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Items/Armor")]
public class Armor : Item
{
    [Header("Armor")]
    public int durability;
    public GameObject OnBodyPrefab;
}
