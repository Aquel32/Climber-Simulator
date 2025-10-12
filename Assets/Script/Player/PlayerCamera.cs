using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target & Components")]
    // Assign the camera's Transform (the child object)
    public Transform cameraTransform;

    // Assign the player's orientation Transform (the parent that holds the camera)
    public Transform orientationTransform;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5.0f;
    public float minVerticalAngle = -45f;
    public float maxVerticalAngle = 85f;

    [Header("Smoothing")]
    // Note: Since the camera is a child, position smoothing isn't needed. 
    // We only need look smoothing.
    public float lookSpeed = 5.0f;

    // Private variables to store camera angles
    private float yaw = 0.0f; // Horizontal rotation
    private float pitch = 0.0f; // Vertical rotation

    void Start()
    {
        if (cameraTransform == null || orientationTransform == null)
        {
            Debug.LogError("Camera or Orientation Transform references are missing! Disabling script.");
            enabled = false;
            return;
        }

        // Initialize angles
        yaw = orientationTransform.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;

        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Get Mouse Input
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // 2. Accumulate Yaw (Horizontal) and Pitch (Vertical)
        yaw += mouseX;
        pitch -= mouseY;

        // Clamp the vertical angle (pitch)
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        // 3. Smoothly Rotate Player Orientation (Yaw)
        Quaternion targetYawRotation = Quaternion.Euler(0f, yaw, 0f);

        // This line rotates the WHOLE player/orientation object for movement direction
        orientationTransform.rotation = Quaternion.Slerp(orientationTransform.rotation, targetYawRotation, lookSpeed * Time.deltaTime);


        // 4. Smoothly Rotate Camera (Pitch)
        Quaternion targetPitchRotation = Quaternion.Euler(pitch, 0f, 0f);

        // This line rotates only the CAMERA (child object) for the vertical view
        cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetPitchRotation, lookSpeed * Time.deltaTime);
    }

    // Removed LateUpdate since the camera is a child, its position is inherited.
}