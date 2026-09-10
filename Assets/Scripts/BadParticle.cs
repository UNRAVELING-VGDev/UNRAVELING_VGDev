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
    public float attackDistance = 0.5f;
    public float hitRadius = 0.4f;
    public bool isHit = false;

    public Transform playerHead;

    private Vector3 driftPos;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private float bobOffset;

    //paranoia increase
    public ParanoiaMeter paranoia;
    public float paranoiaIncrease = 20f;

    //attack movement (the plugged-in variant: Diver / Weaver)
    private IAttackMovement movement;
    private Vector3 attackStart;      // where the attack launched from
    private Vector3 attackPoint;      // the LOCKED target - does not move
    private float attackTime;         // how long we've been attacking

    private Transform attackTarget;   // null = just drifting (still the "am I attacking" flag)

    //manager reads to know if attack is done
    public bool IsAttacking => attackTarget != null;

    void Start()
    {
        driftPos = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        movement = GetComponent<IAttackMovement>();   // grab whichever variant is attached

        PickNewTargetPosition();
    }

    //Called by BadParticleManager when this one is chosen
    public void Attack(Transform target)
    {
        attackStart = transform.position;   // where we launched from
        attackPoint = target.position;      // lock the target NOW (this is the dodge fix)
        attackTime = 0f;
        attackTarget = target;              // flag: we're attacking
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
        attackTime += Time.deltaTime;

        // ask the plugged-in variant where we should be
        transform.position = movement.GetPosition(attackStart, attackPoint, attackTime);

        // reached the locked target point?
        if (Vector3.Distance(transform.position, attackPoint) <= attackDistance)
        {
            // hit test against the player's ACTUAL head (they may have leaned away)
            isHit = Vector3.Distance(transform.position, playerHead.position) <= hitRadius;

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
            StopAttacking();
        }
    }

    void StopAttacking()
    {
        attackTarget = null;
        driftPos = transform.position;
        velocity = Vector3.zero;
        PickNewTargetPosition();
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