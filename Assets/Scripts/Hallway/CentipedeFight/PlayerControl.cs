using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;   // the child camera

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = 20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float pitchLimit = 85f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 4f;      // how far one dash goes
    [SerializeField] private float dashDuration = 0.18f;   // how long the dash takes
    [SerializeField] private float dashCooldown = 0.8f;    // wait before you can dash again
    [SerializeField] private float dashHopHeight = 0.35f;  // how high the little hop goes (meters)

    private CharacterController controller;
    private float pitch;            // up/down angle of the camera
    private float verticalVelocity; 

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private Vector3 dashDirection;

    // other scripts can check these!!!! 
    public bool IsDashing => isDashing;
    public bool CanDash => !isDashing && cooldownTimer <= 0f && controller.isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Look();

        Vector2 input = ReadMoveInput();

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame && CanDash)
            StartDash(input);

        Move(input);
    }

    void Look()
    {
        if (Mouse.current == null || cameraTransform == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up, delta.x);

        pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }
        return Vector2.ClampMagnitude(input, 1f);   // no faster diagonals
    }

    // direction comes from WASD at the moment Space is pressed
    void StartDash(Vector2 input)
    {
        verticalVelocity = Mathf.Sqrt(2f * gravity * dashHopHeight);
        Vector3 dir = transform.right * input.x + transform.forward * input.y;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;               // no keys held: dash forward

        dashDirection = dir.normalized;
        isDashing = true;
        dashTimer = 0f;
        cooldownTimer = dashCooldown;
    }

    void Move(Vector2 input)
    {
        Vector3 move;

        if (isDashing)
        {
            dashTimer += Time.deltaTime;

            float dashSpeed = dashDistance / dashDuration;
            float t = Mathf.Clamp01(dashTimer / dashDuration);
            move = dashDirection * dashSpeed * (1.5f - t); 

            if (dashTimer >= dashDuration)
                isDashing = false;
        }
        else
        {
            move = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        }
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity -= gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}