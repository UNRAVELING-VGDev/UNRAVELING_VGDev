using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook360 : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] Transform _camera;
    [SerializeField] float _sensitivity = 0.2f;
    [SerializeField] float _pitchLimit = 89f;
    [Tooltip("Degrees either side of the walk direction. 180 would look straight back.")]
    [SerializeField] float _walkYawLimit = 120f;

    DuckController _duck;
    WalkCameraBob _bob;
    ContinuousMovement _movement;
    float _yaw;
    float _pitch;

    void Awake()
    {
        _duck = GetComponent<DuckController>();
        _bob = GetComponent<WalkCameraBob>();
        _movement = GetComponent<ContinuousMovement>();

        if (_camera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                _camera = childCamera.transform;
        }

        if (_camera == null || _camera == transform)
        {
            Debug.LogError("MouseLook360 needs a child camera.", this);
            enabled = false;
            return;
        }

        _yaw = SignedAngle(_camera.localEulerAngles.y);
        _pitch = SignedAngle(_camera.localEulerAngles.x);
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

    void LateUpdate()
    {
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * _sensitivity;
            if (_movement != null && _movement.enabled)
                _yaw = Mathf.Clamp(SignedAngle(_yaw), -_walkYawLimit, _walkYawLimit);
            else
                _yaw = Mathf.Repeat(_yaw, 360f);
            _pitch -= delta.y * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, -_pitchLimit, _pitchLimit);
        }

        float pitch = _pitch;
        float roll = 0f;
        if (_duck != null && _duck.enabled)
            pitch += _duck.PitchOffset;
        if (_bob != null && _bob.enabled)
        {
            pitch += _bob.PitchOffset;
            roll = _bob.RollOffset;
        }

        pitch = Mathf.Clamp(pitch, -_pitchLimit, _pitchLimit);
        _camera.localRotation = Quaternion.Euler(pitch, _yaw, roll);
    }

    static float SignedAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        return angle;
    }
}
