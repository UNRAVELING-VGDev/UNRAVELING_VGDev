using UnityEngine;

public class MinigameMouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public Transform clayBody;
    private float _xRotation = 0f;

    // camera offsets
    private DuckController _duck;
    private WalkCameraBob _bob;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _duck = GetComponentInParent<DuckController>();
        _bob = GetComponentInParent<WalkCameraBob>();
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        clayBody.Rotate(Vector3.up * mouseX);
    }

    void LateUpdate()
    {
        float pitch = _xRotation;
        float roll = 0f;

        if (_duck != null)
            pitch += _duck.PitchOffset;

        if (_bob != null && _bob.enabled)
        {
            pitch += _bob.PitchOffset;
            roll = _bob.RollOffset;
        }

        transform.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }
}