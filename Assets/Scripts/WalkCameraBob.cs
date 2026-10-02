using UnityEngine;

// This script adds a bobbing effect to the camera, for use when walking.
public class WalkCameraBob : MonoBehaviour
{
    [Header("Step")]
    [SerializeField] Transform _camera;
    [SerializeField] float _stepsPerSecond = 2.1f;
    [SerializeField] float _referenceSpeed = 2.5f;
    [SerializeField] float _blendTime = 0.15f;

    [Header("Sway")]
    [SerializeField] float _verticalBob = 0.04f;
    [SerializeField] float _sideBob = 0.025f;
    [SerializeField] float _forwardBob = 0.012f;
    [SerializeField] float _rollDegrees = 1.5f;
    [SerializeField] float _pitchDegrees = 1f;

    CharacterController _controller;
    Vector3 _baseLocalPosition;
    float _cycle;
    float _intensity;
    float _intensityVelocity;
    bool _ready;

    public float PitchOffset { get; private set; }
    public float RollOffset { get; private set; }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (_camera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                _camera = childCamera.transform;
        }

        if (_controller == null || _camera == null || _camera == transform)
        {
            Debug.LogError("WalkCameraBob needs a CharacterController and a child camera.", this);
            enabled = false;
            return;
        }

        _baseLocalPosition = _camera.localPosition;
        _ready = true;
    }

    void Update()
    {
        Vector3 velocity = _controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        float targetIntensity = 0f;
        if (_controller.isGrounded && _referenceSpeed > 0.01f)
            targetIntensity = Mathf.Clamp01(speed / _referenceSpeed);

        _intensity = Mathf.SmoothDamp(_intensity, targetIntensity, ref _intensityVelocity, _blendTime);

        if (speed > 0.05f)
        {
            float stepRate = _stepsPerSecond * (speed / Mathf.Max(_referenceSpeed, 0.01f));
            _cycle += stepRate * 0.5f * Mathf.PI * 2f * Time.deltaTime;
        }

        float plant = Mathf.Cos(_cycle);
        float dip = plant * plant;

        PitchOffset = dip * _pitchDegrees * _intensity;
        RollOffset = plant * _rollDegrees * _intensity;
    }

    void LateUpdate()
    {
        float plant = Mathf.Cos(_cycle);
        float dip = plant * plant;

        _camera.localPosition = _baseLocalPosition + new Vector3(
            plant * _sideBob,
            -dip * _verticalBob,
            -dip * _forwardBob) * _intensity;
    }

    void OnDisable()
    {
        if (!_ready)
            return;

        _intensity = 0f;
        _intensityVelocity = 0f;
        PitchOffset = 0f;
        RollOffset = 0f;

        if (_camera != null)
            _camera.localPosition = _baseLocalPosition;
    }
}
