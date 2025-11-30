using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 8f;
    public float rotationSpeed = 10f;

    [Header("Ground Detection")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 0.1f;

    [Header("Animation Parameters")]
    public string speedParameter = "Speed";
    public string jumpParameter = "Jump";
    public string isGroundedParameter = "IsGrounded";
    public string verticalVelocityParameter = "VerticalVelocity";

    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider capsuleCollider;

    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isGrounded;
    private bool wasGrounded;

    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Setup Rigidbody for character movement
        rb.freezeRotation = true; // Prevent physics rotation
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add CapsuleCollider if missing
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.center = new Vector3(0, 1f, 0);
        }
    }

    void Update()
    {
        // Handle input and animations
        HandleInput();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        // Handle physics-based movement
        CheckGrounded();
        ApplyMovement();
    }

    void HandleInput()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate move direction relative to camera
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Keep movement horizontal
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Determine speed (walk/run)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void ApplyMovement()
    {
        if (moveDirection.magnitude > 0.01f)
        {
            // Move the character
            Vector3 movement = moveDirection * currentSpeed;
            rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);

            // Rotate character to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // Trigger jump animation
        if (animator != null)
        {
            animator.SetTrigger(jumpParameter);
        }
    }

    void CheckGrounded()
    {
        // Raycast from character's feet
        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(origin, Vector3.down,
            groundCheckDistance + 0.1f, groundLayer);

        // Alternative: SphereCast for more reliable ground detection
        // isGrounded = Physics.SphereCast(origin, capsuleCollider.radius * 0.9f, 
        //     Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Update movement speed for blend trees
        float animationSpeed = moveDirection.magnitude * currentSpeed;
        animator.SetFloat(speedParameter, animationSpeed);

        // Update grounded state
        animator.SetBool(isGroundedParameter, isGrounded);

        // Update vertical velocity (for jump/fall animations)
        animator.SetFloat(verticalVelocityParameter, rb.linearVelocity.y);

        // Landing detection
        if (!wasGrounded && isGrounded && rb.linearVelocity.y < -1f)
        {
            // Trigger landing animation if you have one
            // animator.SetTrigger("Land");
        }
    }

    // Optional: Add foot IK for better ground alignment
    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // This is where you'd implement foot IK if needed
        // animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        // animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Show ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        Gizmos.DrawLine(origin, origin + Vector3.down * (groundCheckDistance + 0.1f));
    }
}