using UnityEngine;

public class MinigameMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Boundary Settings")]
    public Transform centerPoint;
    public float maxRadius = 3f;

    private CharacterController _controller;
    private Vector3 _velocity;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        _controller.Move(move * moveSpeed * Time.deltaTime);

        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Keeps the player snapped to the ground
        }

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);

        if (centerPoint != null)
        {
            Vector3 offset = transform.position - centerPoint.position;
            offset.y = 0; // Ignore height for the radius calculation

            if (offset.magnitude > maxRadius)
            {
                Vector3 clampedOffset = Vector3.ClampMagnitude(offset, maxRadius);
                Vector3 targetPosition = centerPoint.position + clampedOffset;
                targetPosition.y = transform.position.y;

                // Push the controller back using its own Move method to prevent stuttering
                Vector3 correction = targetPosition - transform.position;
                _controller.Move(correction);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.green;
            Vector3 center = centerPoint.position;
            
            int segments = 36;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * maxRadius, 0, Mathf.Sin(angle1) * maxRadius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * maxRadius, 0, Mathf.Sin(angle2) * maxRadius);

                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}