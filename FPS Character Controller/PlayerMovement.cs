using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    private float sprintSpeed = 7f;
    private float walkSpeed = 3f;
    private float groundDrag = 3f;
    public float jumpForce = 12f;
    private float jumpCooldown = 0.25f;
    private float airAccelerate = 2.2f;
    bool readyToJump;

    [Header("Model")]
    public Transform playerModel;

    [Header("Keybinds")]
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    private bool grounded;
    private bool animGrounded;
    private float ungroundedTimer;

    [Header("Slope Handling")]
    private float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;
    private float slopeAngle;

    [Header("Coyote Time")]
    private float coyoteTime = 0.15f;
    private float coyoteTimer;

    public Transform orientation;
    private float horizontalInput;
    private float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public bool AnimGrounded => animGrounded;
    public Vector3 Velocity => rb.linearVelocity;
    public event System.Action OnJump;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
    }

    private void Update()
    {
        GroundCheck();
        MyInput();
        StateHandler();
        if (grounded)
        {
            SpeedControl();
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotateModel();
    }

    #region Input & Jump
    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    
        if (Input.GetKeyDown(jumpKey) && readyToJump && (grounded || coyoteTimer > 0f) && !OnSteepSlope())
        {
            readyToJump = false;
            coyoteTimer = 0f;

            Jump();
            OnJump?.Invoke();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSteepSlope())
        {
            rb.AddForce(Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (OnWalkableSlope())
        {
            rb.AddForce(Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            AirStrafe(moveDirection.normalized);
        }
    }

    private void StateHandler()
    {
        moveSpeed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;
    }

    private void AirStrafe(Vector3 wishDir)
    {
        float wishSpeed = moveSpeed;
        float accel = airAccelerate;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = Vector3.Dot(flatVel, wishDir);

        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0) return;

        float accelSpeed = accel * wishSpeed * Time.fixedDeltaTime;
        accelSpeed = Mathf.Min(accelSpeed, addSpeed);

        rb.linearVelocity += wishDir * accelSpeed;
    }
    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }
    #endregion

    #region Ground & Slope
    private void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        float extraDistance = Mathf.Abs(Mathf.Min(0f, rb.linearVelocity.y)) * Time.deltaTime * 3f;
        float maxDistance = 0.3f + extraDistance;

        grounded = Physics.SphereCast(origin, 0.2f, Vector3.down, out slopeHit, maxDistance, ~(1 << gameObject.layer));

        if (grounded)
        {
            slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            coyoteTimer = coyoteTime;
            ungroundedTimer = 0f;
        }
        else
        {
            slopeAngle = 0f;
            coyoteTimer -= Time.deltaTime;
            ungroundedTimer += Time.deltaTime;
        }

        animGrounded = grounded || ungroundedTimer < 0.1f;
    }

    private bool OnWalkableSlope() => grounded && slopeAngle > 2f && slopeAngle <= maxSlopeAngle;
    private bool OnSteepSlope() => grounded && slopeAngle > maxSlopeAngle;
    #endregion

    #region Model
    private void RotateModel()
    {
        Vector3 flatForward = new Vector3(orientation.forward.x, 0f, orientation.forward.z);
        if (flatForward.sqrMagnitude < 0.0001f) return;

        playerModel.rotation = Quaternion.LookRotation(flatForward);
    }
    #endregion
}