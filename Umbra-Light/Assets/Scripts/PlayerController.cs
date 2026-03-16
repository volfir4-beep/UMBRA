using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float groundDrag = 9f;
    public float airDrag = 1.5f;
    public float airMultiplier = 0.5f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float fallMultiplier = 4.5f;
    public float lowJumpMultiplier = 3f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool readyToJump = true;
    private float jumpCooldown = 0.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.35f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool wasGrounded;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float lookSmoothing = 15f;

    [Header("Camera Bob")]
    public float bobFrequency = 2.5f;
    public float bobAmplitude = 0.055f;
    public float bobSmoothing = 12f;
    private float bobTimer = 0f;
    private float currentBobY = 0f;

    [Header("Camera Tilt")]
    public float tiltAmount = 2.5f;
    public float tiltSmoothing = 8f;
    private float currentTilt = 0f;

    [Header("Landing Impact")]
    public float landingDipAmount = 0.12f;
    public float landingRecoverySpeed = 8f;
    private float landingDip = 0f;
    private float fallVelocityBeforeLanding = 0f;

    [Header("References")]
    public Transform cameraHolder;

    private Rigidbody rb;
    private float xRotation = 0f;
    private float targetXRotation = 0f;
    private float yRotation = 0f;
    private float targetYRotation = 0f;
    private Vector3 cameraDefaultLocalPos;

    // Animator found in children — character mesh
    private Animator characterAnimator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        // Find animator on character mesh child
        characterAnimator =
            GetComponentInChildren<Animator>();

        if (cameraHolder != null)
            cameraDefaultLocalPos =
                cameraHolder.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        CheckGround();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleMouseLook();
        HandleCameraBob();
        HandleCameraTilt();
        HandleLandingDip();
        ApplyDrag();
        UpdateAnimator();

        if (!isGrounded)
            fallVelocityBeforeLanding =
                rb.linearVelocity.y;
    }

    void FixedUpdate()
    {
        HandleMovement();
        ApplyBetterGravity();
    }

    // ─────────────────────────────────────────
    // ANIMATOR
    // ─────────────────────────────────────────

    void UpdateAnimator()
    {
        if (characterAnimator == null) return;

        // Horizontal speed only — ignore vertical
        float speed = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z).magnitude;

        characterAnimator.SetFloat(
            "Speed", speed,
            0.1f,           // Damp time — smooths transitions
            Time.deltaTime);
        // Using damped SetFloat instead of direct set
        // Prevents animation snapping between states
    }

    // ─────────────────────────────────────────
    // GROUND CHECK
    // ─────────────────────────────────────────

    void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask);

        if (!wasGrounded && isGrounded)
            OnLand();
    }

    void OnLand()
    {
        float impactStrength =
            Mathf.Abs(fallVelocityBeforeLanding);
        landingDip = Mathf.Clamp(
            impactStrength * 0.015f,
            0f,
            landingDipAmount);

        if (jumpBufferCounter > 0f && readyToJump)
            Jump();
    }

    // ─────────────────────────────────────────
    // COYOTE TIME + JUMP BUFFER
    // ─────────────────────────────────────────

    void HandleCoyoteTime()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;

            if (coyoteTimeCounter > 0f && readyToJump)
            {
                Jump();
                coyoteTimeCounter = 0f;
            }
        }
    }

    void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    void Jump()
    {
        readyToJump = false;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z);

        rb.AddForce(
            Vector3.up * jumpForce, ForceMode.Impulse);

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ResetJump()
    {
        readyToJump = true;
    }

    // ─────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDir =
            (transform.forward * z +
             transform.right * x).normalized;

        if (isGrounded)
            rb.AddForce(
                moveDir * moveSpeed * 10f,
                ForceMode.Force);
        else
            rb.AddForce(
                moveDir * moveSpeed * 10f * airMultiplier,
                ForceMode.Force);

        Vector3 flat = new Vector3(
            rb.linearVelocity.x, 0f,
            rb.linearVelocity.z);

        if (flat.magnitude > moveSpeed)
        {
            Vector3 capped =
                flat.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(
                capped.x,
                rb.linearVelocity.y,
                capped.z);
        }
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
            rb.AddForce(
                Vector3.down * fallMultiplier *
                9.81f * rb.mass,
                ForceMode.Force);
        else if (rb.linearVelocity.y > 0f &&
                 !Input.GetKey(KeyCode.Space))
            rb.AddForce(
                Vector3.down * lowJumpMultiplier *
                9.81f * rb.mass,
                ForceMode.Force);
    }

    void ApplyDrag()
    {
        rb.linearDamping =
            isGrounded ? groundDrag : airDrag;
    }

    // ─────────────────────────────────────────
    // MOUSE LOOK
    // ─────────────────────────────────────────

    void HandleMouseLook()
    {
        float mouseX =
            Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY =
            Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        targetYRotation += mouseX;
        targetXRotation -= mouseY;
        targetXRotation =
            Mathf.Clamp(targetXRotation, -80f, 80f);

        xRotation = Mathf.Lerp(xRotation,
            targetXRotation,
            Time.deltaTime * lookSmoothing);

        yRotation = Mathf.Lerp(yRotation,
            targetYRotation,
            Time.deltaTime * lookSmoothing);

        transform.rotation =
            Quaternion.Euler(0f, yRotation, 0f);

        if (cameraHolder != null)
            cameraHolder.localRotation =
                Quaternion.Euler(
                    xRotation, 0f, currentTilt);
    }

    // ─────────────────────────────────────────
    // CAMERA BOB
    // ─────────────────────────────────────────

    void HandleCameraBob()
    {
        if (cameraHolder == null) return;

        float speed = new Vector3(
            rb.linearVelocity.x, 0f,
            rb.linearVelocity.z).magnitude;

        bool isMoving = speed > 0.5f && isGrounded;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency *
                Mathf.Clamp(speed / moveSpeed, 0.3f, 1f);

            float targetBobY =
                Mathf.Sin(bobTimer * 2f * Mathf.PI)
                * bobAmplitude;

            currentBobY = Mathf.Lerp(
                currentBobY, targetBobY,
                Time.deltaTime * bobSmoothing);
        }
        else
        {
            bobTimer = 0f;
            currentBobY = Mathf.Lerp(
                currentBobY, 0f,
                Time.deltaTime * bobSmoothing);
        }

        float totalY = cameraDefaultLocalPos.y +
            currentBobY - landingDip;

        cameraHolder.localPosition = new Vector3(
            cameraDefaultLocalPos.x,
            totalY,
            cameraDefaultLocalPos.z);
    }

    // ─────────────────────────────────────────
    // CAMERA TILT
    // ─────────────────────────────────────────

    void HandleCameraTilt()
    {
        float strafeInput =
            Input.GetAxisRaw("Horizontal");

        float targetTilt = -strafeInput * tiltAmount;

        currentTilt = Mathf.Lerp(
            currentTilt, targetTilt,
            Time.deltaTime * tiltSmoothing);
    }

    // ─────────────────────────────────────────
    // LANDING DIP
    // ─────────────────────────────────────────

    void HandleLandingDip()
    {
        if (landingDip <= 0f) return;

        landingDip = Mathf.Lerp(
            landingDip, 0f,
            Time.deltaTime * landingRecoverySpeed);

        if (landingDip < 0.001f) landingDip = 0f;
    }

    public bool IsGrounded() => isGrounded;
}
