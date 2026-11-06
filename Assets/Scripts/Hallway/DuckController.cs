using UnityEngine;
using UnityEngine.InputSystem;

// This script is used to control the ducking (crouching) of the player.

public class DuckController : MonoBehaviour
{
    enum DuckPhase
    {
        Ready,
        Ducking,
        Recovering
    }

    [Header("Duck")]
    [SerializeField] Transform _camera;
    [Tooltip("How far the camera drops (meters). This is what makes it feel like a crouch.")]
    [SerializeField] float _duckHeight = 0.6f;
    [Tooltip("Tilt while crouched. Negative = look up (ducking under something), positive = look down.")]
    [SerializeField] float _duckPitch = -5f;
    [SerializeField] float _duckedSpeedMultiplier = 0.5f;

    [Header("Timing")]
    [Min(0.01f)] [SerializeField] float _duckDownDuration = 0.12f;
    [Tooltip("How long to stay crouched at the bottom before standing back up.")]
    [Min(0f)] [SerializeField] float _duckHoldDuration = 0.25f;
    [Min(0.01f)] [SerializeField] float _duckUpDuration = 0.2f;
    [Tooltip("Upright time after a duck. Presses during this window are ignored.")]
    [Min(0.01f)] [SerializeField] float _recoveryDuration = 0.5f;

    ContinuousMovement _movement;
    WalkCameraBob _bob;
    Quaternion _baseLocalRotation;
    Vector3 _baseLocalPosition;
    DuckPhase _phase = DuckPhase.Ready;
    float _phaseTime;
    float _duckAmount;
    bool _ready;

    public float PitchOffset => _duckPitch * _duckAmount;
    public float HeightOffset => -_duckHeight * _duckAmount;

    float TotalDuckTime => _duckDownDuration + _duckHoldDuration + _duckUpDuration;

    void Awake()
    {
        _movement = GetComponent<ContinuousMovement>();
        _bob = GetComponent<WalkCameraBob>();

        if (_camera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                _camera = childCamera.transform;
        }

        if (_camera == null || _camera == transform)
        {
            Debug.LogError("DuckController needs a child camera to move.", this);
            enabled = false;
            return;
        }

        _baseLocalRotation = _camera.localRotation;
        _baseLocalPosition = _camera.localPosition;
        _ready = true;
    }

    void Update()
    {
        bool pressed = Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame);

        if (_phase == DuckPhase.Ready && pressed)
        {
            _phase = DuckPhase.Ducking;
            _phaseTime = 0f;
        }

        if (_phase == DuckPhase.Ducking)
        {
            _phaseTime += Time.deltaTime;
            _duckAmount = EvaluateDuck(_phaseTime);

            if (_phaseTime >= TotalDuckTime)
            {
                _duckAmount = 0f;
                _phase = DuckPhase.Recovering;
                _phaseTime = 0f;
            }
        }
        else if (_phase == DuckPhase.Recovering)
        {
            _phaseTime += Time.deltaTime;
            _duckAmount = 0f;

            if (_phaseTime >= _recoveryDuration)
            {
                _phase = DuckPhase.Ready;
                _phaseTime = 0f;
            }
        }

        if (_movement != null)
            _movement.SpeedMultiplier = Mathf.Lerp(1f, _duckedSpeedMultiplier, _duckAmount);
    }

    // 0 = standing, 1 = fully crouched: drop down, hold, rise back up
    float EvaluateDuck(float time)
    {
        if (time <= _duckDownDuration)
            return Mathf.SmoothStep(0f, 1f, time / _duckDownDuration);

        if (time <= _duckDownDuration + _duckHoldDuration)
            return 1f;

        float upTime = time - _duckDownDuration - _duckHoldDuration;
        return Mathf.SmoothStep(1f, 0f, upTime / _duckUpDuration);
    }

    void LateUpdate()
    {
        // height: always applied, so the crouch works even when a look script owns rotation
        _camera.localPosition = _baseLocalPosition + Vector3.up * HeightOffset;

        if (LookOwnsCamera())
            return;

        float pitch = PitchOffset;
        float roll = 0f;
        if (_bob != null && _bob.enabled)
        {
            pitch += _bob.PitchOffset;
            roll = _bob.RollOffset;
        }

        _camera.localRotation = _baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
    }

    bool LookOwnsCamera()
    {
        MouseLook360 look = GetComponent<MouseLook360>();
        return look != null && look.enabled;
    }

    void OnDisable()
    {
        if (!_ready)
            return;

        _phase = DuckPhase.Ready;
        _phaseTime = 0f;
        _duckAmount = 0f;

        if (_camera != null)
        {
            _camera.localPosition = _baseLocalPosition;
            if (!LookOwnsCamera())
                _camera.localRotation = _baseLocalRotation;
        }

        if (_movement != null)
            _movement.SpeedMultiplier = 1f;
    }
}