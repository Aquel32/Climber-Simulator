using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ItemHandler : MonoBehaviourPunCallbacks, IInteractible
{
    public Item item;
    public string customData;

    public bool destroyAfterInteraction = true;

    public void Interact()
    {
        if(InventoryManager.Instance.AddItem(item, customData) == true)
        {
            if (destroyAfterInteraction == true)
            {
                photonView.RPC("DestroyHandlerObjectRPC", RpcTarget.AllBuffered);
            }
        }
    }

    [PunRPC]
    public void DestroyHandlerObjectRPC()
    {
        Destroy(gameObject);
    }
}
