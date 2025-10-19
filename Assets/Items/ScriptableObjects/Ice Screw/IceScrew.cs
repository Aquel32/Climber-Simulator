using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IceScrew : MonoBehaviourPunCallbacks, IUsableItem
{
    public LayerMask groundLayer;
    public GameObject screwPrefab;

    private Transform playerCamera;
    private TwoBoneIKConstraint leftHandRig;
    private Transform leftHandRigTarget;

    private InventoryItem inventoryItem;

    private GameObject screwPlaceholder;

    public void Deinitialize()
    {
        leftHandRig.weight = 0;
        if (screwPlaceholder != null) Destroy(screwPlaceholder);
    }

    public void Initialize(InventoryItem newInventoryItem)
    {
        playerCamera = PlayerCamera.Instance.cameraTransform;
        leftHandRigTarget = InventoryManager.Instance.handBoneTarget;
        leftHandRig = leftHandRigTarget.parent.GetComponent<TwoBoneIKConstraint>();
        inventoryItem = newInventoryItem;
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 2, groundLayer))
        {
            leftHandRig.weight = 1;
            leftHandRigTarget.position = hit.point;

            Quaternion rotation = Quaternion.LookRotation(-hit.normal, Vector3.forward);

            if (screwPlaceholder == null)
            {
                screwPlaceholder = Instantiate(screwPrefab);
            }

            screwPlaceholder.transform.position = hit.point;
            screwPlaceholder.transform.rotation = rotation;

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Destroy(screwPlaceholder);
                InventoryManager.Instance.GetItem(inventoryItem.item, true);
                PhotonNetwork.Instantiate(screwPrefab.name, hit.point, rotation
                    );
                leftHandRig.weight = 0;
            }
        }
        else
        {
            if (screwPlaceholder != null) Destroy(screwPlaceholder);
            leftHandRig.weight = 0;
        }
    }
}
