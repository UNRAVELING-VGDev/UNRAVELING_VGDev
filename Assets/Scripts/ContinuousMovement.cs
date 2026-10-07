using UnityEngine;

// This script gives continuous forward movement to the player.
[RequireComponent(typeof(CharacterController))]
public class ContinuousMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float _moveSpeed = 2.5f;
    [SerializeField] float _gravity = -9.81f;

    CharacterController _controller;
    float _verticalVelocity;
    float _speedMultiplier = 1f;

    public float MoveSpeed
    {
        get => _moveSpeed;
        set => _moveSpeed = Mathf.Max(0f, value);
    }

    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = Mathf.Max(0f, value);
    }

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
            forward.Normalize();
        else
            forward = Vector3.forward;

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 motion = forward * (_moveSpeed * _speedMultiplier);
        motion.y = _verticalVelocity;
        _controller.Move(motion * Time.deltaTime);
    }
}
