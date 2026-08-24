using UnityEngine;

public class BadParticle : MonoBehaviour
{
    [Header("Bounds")]
    public BoxCollider roomBounds;
    public float padding = 0.5f;

    [Header("Drifting")]
    public float smoothTime = 1.5f;
    public float arriveDistance = 0.4f;

    [Header("Bobbing")]
    public float floatSpeed = 0.5f;
    public float floatHeight = 0.12f;

    private Vector3 driftPos;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private float bobOffset;

    void Start()
    {
        driftPos = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 1.2f);

        PickNewTargetPosition();
    }

    void Update()
    {
        // drift toward the target, easing in and out
        driftPos = Vector3.SmoothDamp(driftPos, targetPosition, ref velocity, smoothTime);

        // bob is added on top, so it never affects the arrival check
        float bob = Mathf.Sin(Time.time * floatSpeed + bobOffset) * floatHeight;
        transform.position = driftPos + Vector3.up * bob;

        if (Vector3.Distance(driftPos, targetPosition) < arriveDistance)
        {
            PickNewTargetPosition();
        }
    }

    void PickNewTargetPosition()
    {
        if (roomBounds == null)
        {
            Debug.LogWarning("BadParticle: no roomBounds assigned!", this);
            return;
        }

        Bounds b = roomBounds.bounds;

        targetPosition = new Vector3(
            Random.Range(b.min.x + padding, b.max.x - padding),
            Random.Range(b.min.y + padding, b.max.y - padding),
            Random.Range(b.min.z + padding, b.max.z - padding)
        );
    }

    // shows where it's currently headed, handy while tuning
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.15f);
    }
}

