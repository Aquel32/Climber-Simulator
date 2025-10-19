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
            model.Find(armor.modelObjectNames[i]).gameObject.SetActive(newState);
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


        //if (equipment[index] == null)
        //{
        //    GameObject toDestoy = null;
        //    GameObject toDestoyTwo = null;

        //    switch(lastArmor.slotType)
        //    {
        //        case SlotType.Head:
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, headBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            break;
        //        case SlotType.Body:
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, chestBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            break;
        //        case SlotType.Legs:
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, RightLegBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, LeftLegBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            break;
        //        case SlotType.Shoes:
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, RightFootBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            photonView.RPC("DestroyRPC", RpcTarget.AllBuffered, LeftFootBone.Find("Armor").GetComponent<PhotonView>().ViewID);
        //            break;
        //    }

        //    Destroy(toDestoy);
        //    if(toDestoyTwo != null) Destroy(toDestoyTwo);
        //}
        //else
        //{
        //    switch (equipment[index].slotType)
        //    {
        //        case SlotType.Head:
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, headBone.GetComponent<PhotonView>().ViewID, index);
        //            break;
        //        case SlotType.Body:
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, chestBone.GetComponent<PhotonView>().ViewID, index);
        //            break;
        //        case SlotType.Legs:
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, RightLegBone.GetComponent<PhotonView>().ViewID, index);
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, LeftLegBone.GetComponent<PhotonView>().ViewID, index);
        //            break;
        //        case SlotType.Shoes:
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, RightFootBone.GetComponent<PhotonView>().ViewID, index);
        //            photonView.RPC("InstantiateRPC", RpcTarget.AllBuffered, PhotonNetwork.Instantiate(equipment[index].OnBodyPrefab.name, Vector3.zero, Quaternion.identity).GetComponent<PhotonView>().ViewID, LeftFootBone.GetComponent<PhotonView>().ViewID, index);
        //            break;
        //    }
        //}
    }

    [PunRPC]
    public void InstantiateRPC(int prefabViewId, int boneViewId, int slotIndex, PhotonMessageInfo pmi)
    {
        GameObject go = PhotonView.Find(prefabViewId).gameObject;
        go.transform.SetParent(PhotonView.Find(boneViewId).transform);

        go.name = "Armor";
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = new Quaternion(0, 0, 0, 0);


        if(pmi.Sender == Player.myPlayer.photonPlayer)
        {
            go.GetComponentInChildren<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            foreach(Transform child in go.GetComponentInChildren<MeshRenderer>().transform)
            {
                child.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    [PunRPC]
    public void DestroyRPC(int viewId)
    {
        Destroy(PhotonView.Find(viewId).gameObject);
    }
}
