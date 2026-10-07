using UnityEngine;

public class Movement : MonoBehaviour
{
  public float moveSpeed = 5f;
  public float gravity = -9.81f;
  public float forwardSpeed = 1f;

  private CharacterController _controller;
  private Vector3 _velocity;
  private Vector3 _runForward;
  private Vector3 _runRight;

  void Start()
  {
    _controller = GetComponent<CharacterController>();

    _runForward = transform.forward;
    _runForward.y = 0f;
    _runForward.Normalize();
    _runRight = Vector3.Cross(Vector3.up, _runForward);
  }

  void Update()
  {
    // 1. Get input
    float moveX = Input.GetAxis("Horizontal");

    // 2. Calculate movement vector relative to player rotation
    Vector3 move = _runRight * moveX + _runForward * forwardSpeed;

    // 3. Move the character controller
    _controller.Move(move * moveSpeed * Time.deltaTime);

    // 4. Handle simple gravity so the player falls down
    if (_controller.isGrounded && _velocity.y < 0)
    {
      _velocity.y = -2f; // Keeps the player snapped to the ground
    }

    _velocity.y += gravity * Time.deltaTime;
    _controller.Move(_velocity * Time.deltaTime);
  }
}
