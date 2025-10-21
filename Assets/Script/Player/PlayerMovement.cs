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
    public float groundCheckRadius = 0.3f;

    // --- Private Variables ---
    private Vector3 movementInput;
    private Vector3 surfaceNormal = Vector3.up;

    // --- State Enumerator ---
    public enum MovementState { Walking = 1, IceAxeSupport = 2, Climbing = 10, Airborne = 0, Ragdoll = 100}
    public MovementState currentState = MovementState.Airborne;
    public bool moving = false;

    bool ragdoll = false;
    public bool IsRagdoll { get { return ragdoll; } }

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
        movementInput.x = Input.GetAxis("Horizontal");
        movementInput.z = Input.GetAxis("Vertical");

        playerAnimator.SetFloat("horizontalInput", movementInput.x);
        playerAnimator.SetFloat("verticalInput", movementInput.z);

        // Example trigger for Ragdoll state
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetRagdollState(!ragdoll);
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
        if (currentState == MovementState.Ragdoll)
        {
            playerAnimator.SetBool("IsGrounded", false);
            return;
        }

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

        if (Physics.SphereCast(rayOrigin, groundCheckRadius, rayDirection, out hit, raycastDistance, groundLayer))
        {
            surfaceNormal = hit.normal;
            float surfaceAngle = Vector3.Angle(surfaceNormal, Vector3.up);

            // Store the distance to the ground for the snap logic
            float distanceToGround = hit.distance - 0.1f; // Account for ray origin offset

            FallSystem.Instance.SetSteepnessModifier(surfaceAngle);
            FallSystem.Instance.SetHeightModifier(playerTransform.position.y);

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
            if (currentState == MovementState.Ragdoll && newState != MovementState.Airborne)
            {
                // Only allow ragdoll to be exited by the SetRagdollState(false) method
                // which sets the state to Airborne.
                return;
            }

            currentState = newState;
            FallSystem.Instance.SetBaseChance((int)newState);
            Debug.Log($"State changed to: {currentState}");
        }
    }

    private void ApplyCustomGravityAndSnap()
    {
        if (currentState == MovementState.Ragdoll)
        {
            return; // Do not apply sticky force when ragdolled
        }

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
        if (currentState == MovementState.Ragdoll)
        {
            moving = false;
            return; // Do not apply scripted movement when ragdolled
        }

        moving = false;

        if (!canMove) return;

        // Preserve vertical velocity when stopping horizontal input
        if (movementInput.magnitude < 0.1f)
        {
            // Stop horizontal Rigidbody velocity, but RETAIN the vertical velocity (gravity/falling).
            playerRigidbody.linearVelocity = new Vector3(0, playerRigidbody.linearVelocity.y, 0);
        
            // Only return if we're not airborne (allowing an airborne player with zero input to fall).
            if (currentState != MovementState.Airborne)
            {
                 return;
            }
        }

        float currentSpeed;
        bool usesMovePosition = true;

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
                usesMovePosition = true; // Still use MovePosition for explicit wall crawling
                break;
            default:
                // Airborne state relies entirely on gravity, so we exit.
                return;
        }

        // --- Kinematic Movement Logic ---

        // 1. Calculate the raw desired direction based on player input
        Vector3 desiredInput = playerTransform.forward * movementInput.z + playerTransform.right * movementInput.x;

        // 2. Project the direction onto the plane defined by the surface normal
        Vector3 moveDirection = Vector3.ProjectOnPlane(desiredInput, surfaceNormal).normalized;

        // 3. Calculate the horizontal movement only
        Vector3 horizontalDelta = moveDirection * currentSpeed * Time.fixedDeltaTime;

        if (usesMovePosition)
        {
            // When using MovePosition, we must manually include the Y movement 
            // generated by gravity (playerRigidbody.linearVelocity.y).
            Vector3 verticalDelta = Vector3.up * playerRigidbody.linearVelocity.y * Time.fixedDeltaTime;
        
            // Use the combined horizontal and vertical movement for the next position
            playerRigidbody.MovePosition(playerTransform.position + horizontalDelta + verticalDelta);
        } 
        // If you were to switch to AddForce for Walking/IceAxe, you'd use that here instead.

        if(movementInput.magnitude > 0) moving = true;
    }

    /// <summary>
    /// Sets the player's state to Ragdoll or restores control.
    /// </summary>
    public void SetRagdollState(bool isRagdoll)
    {
        if (isRagdoll == ragdoll) return;

        ragdoll = isRagdoll;

        if (isRagdoll)
        {
            SetState(MovementState.Ragdoll);

            // --- 1. APPLY IMPULSE FORCE ---
            Vector3 pushDirection = surfaceNormal.normalized;
            float pushMagnitude = 1f;
            Vector3 force = (pushDirection * pushMagnitude) + (Vector3.down * pushMagnitude * 0.5f);

            // --- CORRECTION: Set to NON-KINEMATIC and allow rotation ---
            playerRigidbody.isKinematic = false;
            playerRigidbody.freezeRotation = false; // Allow tumbling

            // Clear existing velocity
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            playerRigidbody.AddForce(force, ForceMode.Impulse);

            // --- 2. HANDOVER CONTROL ---
            // --- CORRECTION: DO NOT SET isKinematic = true ---
            // By leaving it 'false', the Rigidbody will now be controlled
            // by Unity's physics, including standard gravity, and "fall freely".
        }
        else
        {
            // --- RESTORE SCRIPT CONTROL ---
            SetState(MovementState.Airborne); // Reset state

            // Restore scripted control parameters
            playerRigidbody.isKinematic = false; // Script assumes non-kinematic
            playerRigidbody.freezeRotation = true; // Stop tumbling
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            // The 'CheckGroundAndSetState' function will take over on the
            // next FixedUpdate and find the ground if available.
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