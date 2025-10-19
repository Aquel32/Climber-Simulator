using Photon.Pun;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IceAxe : MonoBehaviourPunCallbacks, IUsableItem
{
    public LayerMask groundLayer;

    private Transform playerCamera;
    private TwoBoneIKConstraint leftHandRig;
    private Transform leftHandRigTarget;

    public void Deinitialize()
    {
        leftHandRig.weight = 0;
    }

    public void Initialize(InventoryItem newInventoryItem)
    {
        playerCamera = PlayerCamera.Instance.cameraTransform;
        leftHandRigTarget = InventoryManager.Instance.handBoneTarget;
        leftHandRig = leftHandRigTarget.parent.GetComponent<TwoBoneIKConstraint>();
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 1, groundLayer))
        {
            //surfaceNormal = hit.normal;
            //float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            //float distanceToGround = hit.distance - 0.1f;
            leftHandRig.weight = 1;
            leftHandRigTarget.position = hit.point;
            //SHOW SNOW EFFECT ON ICE AXE
            //SMOOTH CHANGE OF WEIGHT
        }
        else
        {
            leftHandRig.weight = 0;
        }
    }
}
