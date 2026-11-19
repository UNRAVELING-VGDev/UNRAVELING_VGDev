using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;   // the child camera

    [Header("Movement")]
    [SerializeField] private float maxMoveSpeed = 15f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private Vector3 velocity = Vector3.zero;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float groundFriction = 0.985f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float pitchLimit = 85f;

    [Header("Dash")]
    [SerializeField] private float dashDuration = 0.175f;   // how long it takes for the dash to reach full speed
    [SerializeField] private float dashCooldown = 0.8f;    // wait before you can dash again
    [SerializeField] private float dashAcceleration = 0.6f;  
    [SerializeField] private float dashHopHeight = 0.6f;  // how high the little hop goes (meters)
    [SerializeField] private float dashFriction = 0.98f;

    private CharacterController controller;
    private float pitch;            // up/down angle of the camera
    private float verticalVelocity; 
    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private Transform dashTransform;
    private Vector2 dashInput = Vector2.zero;
    private Vector3 dashVel = Vector3.zero;
    private float friction = 1;

    // other scripts can check these!!!! 
    public bool IsDashing => isDashing;
    public bool CanDash => !isDashing && cooldownTimer <= 0f && controller.isGrounded;
    public float WalkingDistanceThisFrame { get; private set; }

    public event System.Action DashCompleted;

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
        dashVel = Vector3.zero;
        verticalVelocity = Mathf.Sqrt(2f * gravity * dashHopHeight);
        dashTransform = transform;
        dashInput = input;
        if (dashInput.sqrMagnitude < 0.01f)
            dashInput = Vector2.up;               // no keys held: dash forward

        dashInput = dashInput.normalized;
        isDashing = true;
        dashTimer = 0f;
        cooldownTimer = dashCooldown;
    }

    void Move(Vector2 input)
    {
        Vector3 startPosition = transform.position;
        bool walking = !isDashing;
        Vector3 move;

        if (isDashing)
        {
            dashTimer += Time.deltaTime;

            float t = Mathf.Clamp01(dashTimer / dashDuration);
            dashVel.x += dashInput.x * dashAcceleration;
            dashVel.z += dashInput.y * dashAcceleration;

            if (dashTimer >= dashDuration)
                isDashing = false;
        } else {
            dashVel = dashVel * dashFriction;
        }

        velocity.x += input.x * acceleration;
        velocity.z += input.y * acceleration;
        velocity *= friction;
        velocity = Vector3.ClampMagnitude(velocity, maxMoveSpeed);

        Vector3 deltaPosition = (velocity) * friction * Time.deltaTime;
        deltaPosition = (transform.right * deltaPosition.x + transform.forward * deltaPosition.z);
        Vector3 deltaPositionDash = (dashVel) * Time.deltaTime;
        if (dashTransform != null)
        {
            deltaPositionDash = (dashTransform.right * deltaPositionDash.x + dashTransform.forward * deltaPositionDash.z);
        }
        

        move = deltaPosition + deltaPositionDash;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            friction = groundFriction;
        }
        else { 
            verticalVelocity -= gravity * Time.deltaTime;
            friction = 1f;
        }
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move);
    }
}
