using UnityEngine;
using UnityEngine.InputSystem;

public class LeanController : MonoBehaviour
{
    public Transform leanPivot;
    public Transform cameraHolder;
    public Camera cam;

    // Lean shape
    public float maxPivotAngle = 18f;
    public float extraHeadRoll = 8f;
    public float extraHeadOffset = 0.12f;
    public float leanDip = 0.05f;

    // Spring
    public float stiffness = 110f;
    [Range(0.3f, 1.2f)]
    public float dampingRatio = 0.8f;

    // FOV
    public float fovKickPerSpeed = 1.2f;
    public float maxFovKick = 6f;
    public float fovReturnSpeed = 8f;

    private float lean;
    private float leanVelocity;
    private float baseFov;
    private float fovKick;
    private float cameraHolderBaseY;

    void Awake()
    {
        cameraHolderBaseY = cameraHolder.localPosition.y;
    }

    void Start()
    {
        baseFov = cam.fieldOfView;
    }

    void Update()
    {
        // A/D decides which way we want to lean
        float target = 0f;

        if (Keyboard.current.aKey.isPressed)
            target = -1f;

        if (Keyboard.current.dKey.isPressed)
            target = 1f;


        // Spring movement
        float dt = Time.deltaTime;
        float omega = Mathf.Sqrt(stiffness);

        float acceleration =
            stiffness * (target - lean)
            - 2f * dampingRatio * omega * leanVelocity;

        leanVelocity += acceleration * dt;
        lean += leanVelocity * dt;


        // Rotate the HIP pivot
        float bodyAngle = -lean * maxPivotAngle;

        leanPivot.localRotation =
            Quaternion.Euler(0f, 0f, bodyAngle);


        // Move and rotate the HEAD
        float absLean = Mathf.Abs(lean);

        cameraHolder.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                -lean * extraHeadRoll
            );

        cameraHolder.localPosition =
            new Vector3(
                lean * extraHeadOffset,
                cameraHolderBaseY - absLean * leanDip,
                0f
            );


        // FOV reacts to how FAST we're moving
        float targetKick =
            Mathf.Min(
                Mathf.Abs(leanVelocity) * fovKickPerSpeed,
                maxFovKick
            );

        if (targetKick > fovKick)
            fovKick = targetKick;
        else
            fovKick = Mathf.Lerp(
                fovKick,
                targetKick,
                fovReturnSpeed * dt
            );

        cam.fieldOfView = baseFov + fovKick;
    }
}