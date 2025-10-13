using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target & Components")]
    public Transform cameraRotationTransform;
    public Transform cameraTransform;
    public Transform orientationTransform;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5.0f;
    public float minVerticalAngle = -45f;
    public float maxVerticalAngle = 85f;

    [Header("Smoothing")]
    public float lookSpeed = 10.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    [Header("Meshes")]
    public List<SkinnedMeshRenderer> meshesToHide = new List<SkinnedMeshRenderer>();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        for (int i = 0; i < meshesToHide.Count; i++)
        {
            meshesToHide[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        yaw += mouseX;
        pitch -= mouseY;

        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        Quaternion targetYawRotation = Quaternion.Euler(0f, yaw, 0f);
        orientationTransform.rotation = Quaternion.Slerp(orientationTransform.rotation, targetYawRotation, lookSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        Quaternion targetPitchRotation = Quaternion.Euler(pitch, 0f, 0f);

        if(Input.GetKey(KeyCode.LeftAlt))
        {
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);
        }
        else
        {
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.identity, lookSpeed * Time.deltaTime);
            cameraRotationTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);
        }
    }
}