using UnityEngine;

/// <summary>
/// Standard PC First-Person Controller using Unity's native Input Manager,
/// optimized for laptop and keyboard/mouse testing.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── Inspector Settings ──────────────────────────────────────────────────
    [Header("Mouse Look Settings")]
    [Tooltip("Sensitivity for horizontal (yaw) mouse movement.")]
    [SerializeField] private float mouseSensitivityX = 2.0f;

    [Tooltip("Sensitivity for vertical (pitch) mouse movement.")]
    [SerializeField] private float mouseSensitivityY = 2.0f;

    [Tooltip("Minimum vertical look angle in degrees.")]
    [SerializeField] private float minPitch = -80f;

    [Tooltip("Maximum vertical look angle in degrees.")]
    [SerializeField] private float maxPitch = 80f;

    [Header("Movement Settings")]
    [Tooltip("Walking speed in units per second.")]
    [SerializeField] private float walkSpeed = 5.0f;

    [Tooltip("Sprinting speed in units per second (optional sprint via Left-Shift).")]
    [SerializeField] private float sprintSpeed = 8.0f;

    [Tooltip("Downwards force applied when grounded to keep the player snapped to terrain/slopes.")]
    [SerializeField] private float groundedGravity = -2.0f;

    [Tooltip("Gravity multiplier applied to Physics.gravity.y.")]
    [SerializeField] private float gravityMultiplier = 1.0f;

    [Header("Camera & View")]
    [Tooltip("Reference to the player's camera (usually child of Player). If null, Camera.main is used.")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance for the interaction raycast from camera center.")]
    [SerializeField] private float interactRayLength = 4.0f;

    [Tooltip("Layer mask specifying which objects are interactable.")]
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Debug / State")]
    [SerializeField] private bool showDebugRay = true;

    // ── Public Properties ───────────────────────────────────────────────────
    /// <summary>
    /// Returns true when the center crosshair raycast is currently pointing at an IInteractable.
    /// Can be used by UI scripts to tint/scale a crosshair image.
    /// </summary>
    public bool CrosshairHighlightActive => currentTarget != null;

    /// <summary>
    /// The interactable currently targeted by the crosshair raycast, or null if none.
    /// </summary>
    public IInteractable CurrentTarget => currentTarget;

    // ── Private State ───────────────────────────────────────────────────────
    private CharacterController characterController;
    private Transform cameraTransform;

    private float pitchAngle = 0f;
    private float verticalVelocity = 0f;
    private IInteractable currentTarget;

    // ── Unity Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main;
            }
        }

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            pitchAngle = cameraTransform.localEulerAngles.x;
            if (pitchAngle > 180f) pitchAngle -= 360f;
        }
        else
        {
            Debug.LogWarning("[PlayerController] No Camera assigned or found in children/MainCamera!", this);
        }
    }

    private void Start()
    {
        // Lock and hide the mouse cursor for first-person PC gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCursorLockToggle();
        HandleLook();
        HandleMovement();
        HandleInteraction();
    }

    // ── Mouse Look ──────────────────────────────────────────────────────────
    private void HandleLook()
    {
        // Only look around when cursor is locked
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // Native Input axes
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // Yaw: rotate the player's body around the Y-axis
        transform.Rotate(Vector3.up * mouseX);

        // Pitch: rotate the camera around its local X-axis (clamped between minPitch and maxPitch)
        if (cameraTransform != null)
        {
            pitchAngle -= mouseY;
            pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
            cameraTransform.localEulerAngles = new Vector3(pitchAngle, 0f, 0f);
        }
    }

    // ── Movement & Gravity ──────────────────────────────────────────────────
    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down

        // Calculate planar movement relative to player orientation
        Vector3 moveInput = transform.right * h + transform.forward * v;
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 moveVelocity = moveInput * currentSpeed;

        // Grounding and Gravity calculation
        if (characterController.isGrounded)
        {
            // Apply slight downward velocity to stay snapped to terrain and slopes
            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }
        }
        else
        {
            // Apply Unity Physics.gravity.y
            verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }

        moveVelocity.y = verticalVelocity;

        // Move the CharacterController
        characterController.Move(moveVelocity * Time.deltaTime);
    }

    // ── Interaction ─────────────────────────────────────────────────────────
    private void HandleInteraction()
    {
        if (cameraTransform == null)
            return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactRayLength,
                currentTarget != null ? Color.green : Color.red);
        }

        // Raycast from camera center up to interactRayLength
        if (Physics.Raycast(ray, out RaycastHit hit, interactRayLength, interactLayerMask))
        {
            currentTarget = hit.collider.GetComponentInParent<IInteractable>();
        }
        else
        {
            currentTarget = null;
        }

        // Trigger interaction when pressing 'E' or Left Mouse Button (Fire1)
        bool interactPressed = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        if (interactPressed && currentTarget != null)
        {
            currentTarget.PerformJob();
        }
    }

    // ── Cursor Lock Management (Convenience for Laptop/Editor Testing) ──────
    private void HandleCursorLockToggle()
    {
        // Press Escape to unlock mouse cursor (for interacting with Unity Editor / Inspector / UI)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Left-click back in game window to re-lock cursor if currently unlocked
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
