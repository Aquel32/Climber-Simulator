using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Rope : MonoBehaviour, IUsableItem
{
    public LayerMask groundLayer;

    private Transform playerCamera;
    private TwoBoneIKConstraint leftHandRig;
    private Transform leftHandRigTarget;
    private PlacedIceScrew currentIceScrew;

    public void Deinitialize()
    {
        leftHandRig.weight = 0;

        if (currentIceScrew != null)
        {
            currentIceScrew.DetachRope();
            currentIceScrew = null;
        }
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

        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, 2, groundLayer) && hit.collider.TryGetComponent<PlacedIceScrew>(out PlacedIceScrew placedIceScrew))
        {
            leftHandRig.weight = 1;
            leftHandRigTarget.position = hit.point;

            if(Input.GetKeyDown(KeyCode.Mouse0))
            {
                if(currentIceScrew == null)
                {
                    print("ATTACHING ROPE");
                    placedIceScrew.AttachRope();
                }
                else
                {
                    print("ATTACHING ROPE FURTHER");
                    currentIceScrew.ModifyEnd(placedIceScrew.transform);
                    placedIceScrew.AttachRope();
                }

                currentIceScrew = placedIceScrew;
            }
        }
        else
        {
            leftHandRig.weight = 0;
        }
    }
}
