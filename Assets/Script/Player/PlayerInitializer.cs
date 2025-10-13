using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerInitializer : MonoBehaviour
{
    [SerializeField] private GameObject cam;
    [SerializeField] private Animator animator;
    [SerializeField] private TwoBoneIKConstraint leftHandRig;
    [SerializeField] private Transform leftHandRigTarget;
    [SerializeField] private Transform cameraRotationTransform;
    [SerializeField] private Transform itemHolder;
    [SerializeField] private Transform handTarget;

    [SerializeField] private List<SkinnedMeshRenderer> meshesToHide = new List<SkinnedMeshRenderer>();

    private void Start()
    {
        cam.SetActive(true);

        for (int i = 0; i < meshesToHide.Count; i++)
        {
            meshesToHide[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }


        PlayerCamera.Instance.cameraRotationTransform = cameraRotationTransform;
        PlayerCamera.Instance.cameraTransform = cam.transform;
        PlayerCamera.Instance.orientationTransform = this.transform;
        PlayerCamera.Instance.enabled = true;

        PlayerMovement.Instance.playerRigidbody = GetComponent<Rigidbody>();
        PlayerMovement.Instance.playerTransform = this.transform;
        PlayerMovement.Instance.playerCamera = cam.transform;
        PlayerMovement.Instance.playerAnimator = animator;

        PlayerMovement.Instance.enabled = true;

        Interactor.Instance.rayOrigin = cam.transform;
        Interactor.Instance.enabled = true;

        InventoryManager.Instance.itemHolder = itemHolder;
        InventoryManager.Instance.handBoneTarget = handTarget;
        InventoryManager.Instance.enabled = true;
    }
}
