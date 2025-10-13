using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    public bool canMove;

    [Header("Required References")]
    public Rigidbody playerRigidbody;
    public Transform playerTransform;
    public Transform playerCamera;

    public Animator playerAnimator;

    [Header("Movement Speeds (m/s)")]
    public float walkSpeed = 5.0f;
    public float iceAxeSpeed = 2.0f;
    public float climbingSpeed = 1.5f;

    [Header("Angle Thresholds (Degrees)")]
    public float walkMaxAngle = 30f;
    public float iceAxeMaxAngle = 60f;

    [Header("Ground Detection & Physics")]
    public float raycastDistance = 1.5f;
    public float stickyForce = 50f;
    public LayerMask groundLayer;
    public float snapDistance = 0.2f;

    // --- Private Variables ---
    private Vector3 movementInput;
    private Vector3 surfaceNormal = Vector3.up;

    // --- State Enumerator ---
    public enum MovementState { Walking, IceAxeSupport, Climbing, Airborne, Ragdoll }
    public MovementState currentState = MovementState.Airborne;

    void Awake()
    {
        Instance = this;

        if (playerRigidbody == null || playerTransform == null || playerAnimator == null)
        {
            //Debug.LogError("Required Rigidbody/Transform references are missing! Disabling script.");
            enabled = false;
            return;
        }

        // Set Rigidbody to Kinematic only if you want NO physics simulation other than gravity
        // For this approach, we keep it NON-KINEMATIC to allow gravity/forces to work.
        playerRigidbody.freezeRotation = true;
        if (groundLayer.value == 0) groundLayer = ~0;
    }

    void Update()
    {
        if (currentState == MovementState.Ragdoll) return;

        movementInput.x = Input.GetAxis("Horizontal");
        movementInput.z = Input.GetAxis("Vertical");

        playerAnimator.SetFloat("horizontalInput", movementInput.x);
        playerAnimator.SetFloat("verticalInput", movementInput.z);

        // Example trigger for Ragdoll state
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetRagdollState(true);
        }
    }

    void FixedUpdate()
    {
        CheckGroundAndSetState();

        ApplyCustomGravityAndSnap();

        HandleMovement();

        HandleRigging();
    }

    // --- Core Logic Methods ---

    private void CheckGroundAndSetState()
    {
        Vector3 rayDirection;
        float currentAngle = Vector3.Angle(surfaceNormal, Vector3.up);

        // Define the raycast direction (biased towards surface normal for stability)
        if (currentAngle >= walkMaxAngle && currentAngle < 90f)
        {
            rayDirection = (Vector3.down * 0.2f + -surfaceNormal * 0.8f).normalized;
        }
        else
        {
            rayDirection = Vector3.down;
        }

        Vector3 rayOrigin = playerTransform.position + Vector3.up * 0.1f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastDistance, groundLayer))
        {
            surfaceNormal = hit.normal;
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);

            // Store the distance to the ground for the snap logic
            float distanceToGround = hit.distance - 0.1f; // Account for ray origin offset

            // 4. State Update Logic
            if (surfaceAngle < walkMaxAngle)
            {
                SetState(MovementState.Walking);
            }
            else if (surfaceAngle < iceAxeMaxAngle)
            {
                SetState(MovementState.IceAxeSupport);
            }
            else
            {
                SetState(MovementState.Climbing);
            }

            // CRITICAL: Snap the player down if they are too high above the hit point
            if (distanceToGround > 0f && distanceToGround < snapDistance)
            {
                // Move the player's transform down to the ground level
                playerTransform.position -= rayDirection * (distanceToGround);
            }
        }
        else
        {
            SetState(MovementState.Airborne);
        }

        playerAnimator.SetBool("IsGrounded", currentState != MovementState.Airborne);
    }

    private void SetState(MovementState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            Debug.Log($"State changed to: {currentState}");
        }
    }

    private void ApplyCustomGravityAndSnap()
    {
        // Only apply extra force when grounded to ensure the player sticks/snaps
        if (currentState != MovementState.Airborne)
        {
            Vector3 forceToApply = Vector3.zero;

            // Apply a constant downward force (Sticky Force)
            if (currentState == MovementState.Climbing)
            {
                // Push the player INTO the wall when climbing
                forceToApply = -surfaceNormal * stickyForce;
            }
            else
            {
                // Push the player DOWN along the surface when walking/bracing
                forceToApply = -surfaceNormal * stickyForce;
            }

            // Add world gravity back into the force calculation 
            playerRigidbody.AddForce(forceToApply, ForceMode.Acceleration);
        }
    }

    private void HandleMovement()
    {
        if (!canMove) return;

        if (movementInput.magnitude < 0.1f)
        {
            // Stop horizontal Rigidbody velocity immediately when input is released
            // (We keep the Y velocity for gravity/falling)
            playerRigidbody.linearVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, 0);
            return;
        }

        float currentSpeed;

        switch (currentState)
        {
            case MovementState.Walking:
                currentSpeed = walkSpeed;
                break;
            case MovementState.IceAxeSupport:
                currentSpeed = iceAxeSpeed;
                break;
            case MovementState.Climbing:
                currentSpeed = climbingSpeed;

                // CRITICAL FOR CLIMBING: Disable Rigidbody velocity entirely for clean movement.
                playerRigidbody.linearVelocity = Vector3.zero;
                break;
            default:
                return;
        }

        // --- Kinematic Movement Logic ---

        // 1. Calculate the raw desired direction based on player input
        Vector3 desiredInput = playerTransform.forward * movementInput.z + playerTransform.right * movementInput.x;

        // 2. Project the direction onto the plane defined by the surface normal
        Vector3 moveDirection = Vector3.ProjectOnPlane(desiredInput, surfaceNormal).normalized;

        // 3. Move the player's position directly (Kinematic/Teleport)
        Vector3 deltaPosition = moveDirection * currentSpeed * Time.fixedDeltaTime;

        // Use MovePosition for Rigidbody-based smooth movement (important for collision)
        playerRigidbody.MovePosition(playerTransform.position + deltaPosition);
    }

    /// <summary>
    /// Sets the player's state to Ragdoll or restores control.
    /// </summary>
    /// <param name="isRagdoll">True to enable ragdoll physics, False to resume control.</param>
    public void SetRagdollState(bool isRagdoll)
    {
        if (isRagdoll)
        {
            SetState(MovementState.Ragdoll);

            // --- PHYSICS CONTROL HANDOVER ---

       
            // 2. Enable full Rigidbody physics control for falling/ragdoll
            playerRigidbody.isKinematic = false;

            // 3. Allow physics to rotate the main body
            playerRigidbody.freezeRotation = false;

            // NOTE: You still need separate logic to enable the child ragdoll Rigidbodies/Colliders 
            // and disable the main animation script, as the main Rigidbody alone won't look like a ragdoll.
        }
        else
        {
            // Reset to Airborne state (will transition to Walking/Climbing on next FixedUpdate)
            SetState(MovementState.Airborne);

            // --- RESTORE SCRIPT CONTROL ---


            // 2. Restore scripted control parameters
            playerRigidbody.isKinematic = false; // Ensure it's not kinematic for our movement
            playerRigidbody.freezeRotation = true; // Lock rotation for player control
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            // NOTE: You would add logic here to disable child ragdoll Rigidbodies/Colliders
        }
    }

    public void HandleRigging()
    {
        //SHOES RIGID FOR VISUALS (remember about climbing)

        //CAMERA SENDS RAYCAST
        //WE SET LEFT HAND TARGET POSITION TO THAT RAYCAST HIT POSITION
        //IF NO HIT HAND WEIGHT = 0

        //IF HIT LESS MISTAKE CHANCE
        //IF NOT (we re using ice axe) INCREASE MISTAKE CHANCE

        


        //leftHandRig.gameObject.SetActive(currentState == MovementState.IceAxeSupport);
        //leftHandRig.weight = currentState == MovementState.IceAxeSupport ? 1 : 0;
    }
}