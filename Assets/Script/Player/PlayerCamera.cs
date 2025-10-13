using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera Instance;

    public bool canUseMouse;

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

    void Awake()
    {
        Instance = this;

        if (cameraRotationTransform == null || cameraTransform == null || orientationTransform == null)
        {
            //Debug.LogError("Required transforms references are missing! Disabling script.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!canUseMouse) return;

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
        cameraRotationTransform.localRotation = Quaternion.Slerp(cameraRotationTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);

        //if(Input.GetKey(KeyCode.LeftAlt))
        //{
        //    cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);
        //}
        //else
        //{
        //    cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, Quaternion.identity, lookSpeed * Time.deltaTime);
        //    cameraRotationTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);
        //}
    }
}