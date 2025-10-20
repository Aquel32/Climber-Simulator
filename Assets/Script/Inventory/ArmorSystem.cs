using Photon.Pun;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public interface IGearScript
{
    public void Initialize();
    public void Deinitialize();
}

public class ArmorSystem : MonoBehaviourPunCallbacks
{
    public static ArmorSystem Instance;

    public InventorySlot[] inventorySlots;
    public List<Armor> equipment = new List<Armor>();
    public Transform model;
    //[HideInInspector] public Transform headBone, chestBone, RightLegBone, LeftLegBone, RightFootBone, LeftFootBone;

    public void Awake() { 
        Instance = this; 

        if(model == null)
        {
            enabled = false;
        }
    }


    private void Start()
    {
        LookForChanges();
    }

    public void LookForChanges()
    {
        print("Looking for changes in equipment");

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            Armor tempArmor = equipment[i];

            if (inventorySlots[i].currentInventoryItem != null) equipment[i] = (Armor)inventorySlots[i].currentInventoryItem.item;
            else equipment[i] = null;

            if (equipment[i] != tempArmor)
            {
                UpdateEquipment(i, tempArmor);
            }
        }
    }

    public void UpdateEquipment(int index, Armor lastArmor)
    {
        Armor armor = equipment[index] == null ? lastArmor : equipment[index];
        bool newState = equipment[index] == null ? false : true;

        for (int i = 0; i < armor.modelObjectNames.Length; i++)
        {
            photonView.RPC("DisplayModelRPC", RpcTarget.AllBuffered, armor.modelObjectNames[i], newState);
        }

        if (armor.modelObjectNames.Length == 0) return;
        if (model.parent.Find("GearScripts").Find(armor.modelObjectNames[0]) == null) return;
        model.parent.Find("GearScripts").Find(armor.modelObjectNames[0]).gameObject.SetActive(newState);
        if (model.parent.Find("GearScripts").Find(armor.modelObjectNames[0]).TryGetComponent<IGearScript>(out IGearScript gearScript))
        {
            if(newState)
            {
                model.parent.Find("GearScripts").Find(armor.modelObjectNames[0]).gameObject.SetActive(true);
                gearScript.Initialize();
            }
            else
            {
                gearScript.Deinitialize();
                model.parent.Find("GearScripts").Find(armor.modelObjectNames[0]).gameObject.SetActive(false);
            }
        }
    }

    [PunRPC]
    public void DisplayModelRPC(string modelName, bool state, PhotonMessageInfo pmi)
    {
        Player.FindPlayer(pmi.Sender).playerObject.transform.Find("Model").Find("Climber").Find(modelName).gameObject.SetActive(state);
    }
}
