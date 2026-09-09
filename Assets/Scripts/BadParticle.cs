using UnityEngine;

public class BadParticle : MonoBehaviour
{
    //bounds
    public BoxCollider roomBounds;
    public float padding = 0.5f;

    //drifting
    public float smoothTime = 1.5f;
    public float arriveDistance = 0.4f;

    //bobbing
    public float floatSpeed = 0.5f;
    public float floatHeight = 0.12f;

    //attacking
    public float attackSpeed = 2f;
    public float acceleration = 6f;
    public float maxAttackSpeed = 12f;
    public float attackDistance = 0.5f;
    public float hitRadius = 0.4f;
    public bool isHit = false;

    public Transform playerHead;

    private float currentSpeed;

    private Vector3 driftPos;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private float bobOffset;

    //paranoia increase
    public ParanoiaMeter paranoia;
    public float paranoiaIncrease = 20f;

    private Transform attackTarget;   // null = just drifting

    void Start()
    {
        driftPos = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        PickNewTargetPosition();
    }

    /// Called by BadParticleManager when this one is chosen.
    public void Attack(Transform target)
    {
        attackTarget = target;
        currentSpeed = attackSpeed;
    }

    void Update()
    {
        if (attackTarget != null)
            FlyAtTarget();
        else
            Drift();
    }

    void FlyAtTarget()
    {
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxAttackSpeed);

        transform.position = Vector3.MoveTowards(
            transform.position,
            attackTarget.position,
            currentSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, attackTarget.position) <= attackDistance)
        {
            isHit = Vector3.Distance(
                transform.position,
                playerHead.position
            ) <= hitRadius;

            if (isHit)
            {
                Debug.Log("HIT");

                if (paranoia != null)
                {
                    paranoia.Add(paranoiaIncrease);
                }
            }
            else
            {
                Debug.Log("DODGED");
            }

            Destroy(gameObject);
        }
    }

    void Drift()
    {
        driftPos = Vector3.SmoothDamp(driftPos, targetPosition, ref velocity, smoothTime);

        float bob = Mathf.Sin(Time.time * floatSpeed + bobOffset) * floatHeight;
        transform.position = driftPos + Vector3.up * bob;

        if (Vector3.Distance(driftPos, targetPosition) < arriveDistance)
            PickNewTargetPosition();
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

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.15f);
    }
}