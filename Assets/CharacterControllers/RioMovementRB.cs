using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementRB : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float turnSmoothTime = 0.1f;
    public float maxTurnSpeed = 200f;
    public Transform cameraTransform;  // Assign your camera transform in the inspector

    private Rigidbody rb;
    private Animator animator;
    private float turnSmoothVelocity;
    private float previousTargetAngle;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        animator = GetComponent<Animator>();
        previousTargetAngle = transform.eulerAngles.y;

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void FixedUpdate()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(inputX, 0, inputZ).normalized;

        float speed = 0f;
        float turnSpeed = 0f;

        if (input.magnitude > 0.1f)
        {
            // Get camera's forward and right directions (flattened to horizontal plane)
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            // Flatten to ignore vertical component
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate movement direction relative to camera
            Vector3 moveDirection = (cameraForward * inputZ + cameraRight * inputX).normalized;

            // Apply movement
            Vector3 velocity = moveDirection * moveSpeed;
            rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

            // Calculate target angle based on movement direction
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

            // Calculate turn speed based on change in TARGET direction
            float angleDelta = Mathf.DeltaAngle(previousTargetAngle, targetAngle);
            turnSpeed = angleDelta / Time.fixedDeltaTime;

            // Update previous target angle
            previousTargetAngle = targetAngle;

            // Smooth rotation
            float currentAngle = transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            speed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
        }
        else
        {
            // No input, stop horizontal movement
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // Normalize turn speed to -1 to 1 range for blend tree
        float normalizedTurn = Mathf.Clamp(turnSpeed / maxTurnSpeed, -1f, 1f);

        // Send to animator
        animator.SetFloat("speed", speed);
        animator.SetFloat("turnSpeed", normalizedTurn, 0.1f, Time.fixedDeltaTime);
    }
}