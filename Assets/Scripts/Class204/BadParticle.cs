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
    public float attackOffsetRange = 0.5f;
    private Vector3 attackOffset;

    //paranoia increase
    public ParanoiaMeter paranoia;
    public float paranoiaIncrease = 20f;

    //teacher: particles only attack while her back is turned
    public bool cancelWhenTeacherTurns = true;

    //attack movement (the plugged-in variant: Diver / Weaver)
    private IAttackMovement movement;
    private Vector3 attackStart;      // where the attack launched from
    private Vector3 attackPoint;      // the LOCKED target - does not move
    private float attackTime;         // how long we've been attacking

    private Transform attackTarget;   // null = just drifting (still the "am I attacking" flag)

    //manager reads to know if attack is done
    public bool IsAttacking => attackTarget != null;

    void OnEnable()
    {
        TeacherState.OnStateChanged += OnTeacherStateChanged;
    }

    void OnDisable()
    {
        TeacherState.OnStateChanged -= OnTeacherStateChanged;
    }

    void Start()
    {
        driftPos = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        movement = GetComponent<IAttackMovement>();   // grab whichever variant is attached

        // always use the ParanoiaMeter that's actually in the scene
        paranoia = FindFirstObjectByType<ParanoiaMeter>();
        if (paranoia == null)
            Debug.LogWarning("BadParticle: no ParanoiaMeter found in the scene!", this);

        PickNewTargetPosition();
    }

    //Called by BadParticleManager when this one is chosen
    public void Attack(Transform target)
    {
        // offset only left/right
        float side = Random.Range(-attackOffsetRange, attackOffsetRange);
        attackOffset = playerHead.right * side;

        attackStart = transform.position;
        attackPoint = target.position + attackOffset;   // locked, offset to a side
        attackTime = 0f;
        attackTarget = target;
    }

    // stops an attack early (no hit, no sound) and respawns somewhere in the room
    public void CancelAttack()
    {
        if (IsAttacking) StopAttacking();
    }

    // the teacher started turning around: call off the attack
    void OnTeacherStateChanged(TeacherState.State newState)
    {
        if (!cancelWhenTeacherTurns) return;

        if (newState != TeacherState.State.FacingBoard &&
            newState != TeacherState.State.WalkingToPlayer)
            CancelAttack();
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

        // ask the plugged-in variant where we should be (flies toward the offset target)
        transform.position = movement.GetPosition(attackStart, attackPoint, attackTime);

        // HIT: the particle touched the player's head at any point during the attack
        float headDist = Vector3.Distance(transform.position, playerHead.position);
        if (headDist <= hitRadius)
        {
            isHit = true;
            Debug.Log("HIT");
            if (paranoia != null) paranoia.Add(paranoiaIncrease, true);   
            AudioManager.instance.playOneShot("clay_hurt");
            StopAttacking();
            return;
        }

        // DODGED: it reached its aim point without ever touching the head
        if (Vector3.Distance(transform.position, attackPoint) <= attackDistance)
        {
            isHit = false;
            Debug.Log($"DODGED (closest head distance at end: {headDist:F2})");
            AudioManager.instance.playOneShot("clay_dodge", transform.position);
            StopAttacking();
        }
    }

    void StopAttacking()
    {
        attackTarget = null;
        velocity = Vector3.zero;

        // teleport to a fresh random spot inside the room, then drift from there
        Bounds b = roomBounds.bounds;
        Vector3 respawnPos = new Vector3(
            Random.Range(b.min.x + padding, b.max.x - padding),
            Random.Range(b.min.y + padding, b.max.y - padding),
            Random.Range(b.min.z + padding, b.max.z - padding)
        );

        transform.position = respawnPos;   // actually move it there
        driftPos = respawnPos;             // drift starts from here
        PickNewTargetPosition();           // pick where to drift toward next
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