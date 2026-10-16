using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;

public class HealingParticle : MonoBehaviour {
    // idle
    public Vector3 amplitudes = new Vector3(1f, 0.3f, 1f);
    public float bobFrequency = 1f;
    private Vector3 position;
    private Vector3 bobOffset;

    // time in idle
    public float minIdleTime = 4f;
    public float maxIdleTime = 8f;
    private float idleTime;
    private bool onAttack = false;

    // move
    public float moveSpeed = 3f;
    public float decayRadius = 3f;
    public float deathRadius = 0.5f;
    public float curveAmplitude = 0.5f;
    private float timer = 0;
    private bool inDecayZone = false;
    private Vector3 originalScale;

    // targeting slop
    private Transform attackTarget;
    private float curvingPeriod = 0;
    private Vector3 curvingVector;

    // book catching stuff
    public float decreaseAmount = 10f;
    public float suckSpeed = 12f;
    private bool caught = false;
    private float suckTime = 0f;

    public bool IsCaught => caught;
    public event System.Action<HealingParticle> Caught;
    public static event System.Action<HealingParticle> Unregistered;

    void OnDisable() {
        Unregistered?.Invoke(this);
    }

    public bool TryCatch(Transform catchTarget, ParanoiaMeter meter) {
        if (meter == null || !BeginCatch(catchTarget)) {
            return false;
        }

        meter.Add(-Mathf.Max(0f, decreaseAmount));
        Caught?.Invoke(this);
        return true;
    }

    // pull the letter in from where it is now so it doesn't jump back
    bool BeginCatch(Transform catchTarget) {
        if (caught || !isActiveAndEnabled || catchTarget == null) {
            return false;
        }

        caught = true;
        position = transform.position;
        originalScale = transform.localScale;
        attackTarget = catchTarget;
        timer = 0f;
        suckTime = Mathf.Max(0.01f, Vector3.Distance(position, attackTarget.position)
            / Mathf.Max(0.01f, suckSpeed));
        foreach (Collider particleCollider in GetComponentsInChildren<Collider>()) {
            particleCollider.enabled = false;
        }

        return true;
    }

    public void SetTarget(Transform target) {
        attackTarget = target;
    }

    void Start() {
        if (caught) {
            return;
        }

        position = transform.position;
        bobOffset = new Vector3(Random.Range(0f, Mathf.PI * 2f),Random.Range(0f, Mathf.PI * 2f),Random.Range(0f, Mathf.PI * 2f));
        idleTime = Random.Range(minIdleTime, maxIdleTime);
        originalScale = transform.localScale;
    }

    float EasingFunc(float t) {
        return 2.8f*t*t*t-1.8f*t*t;
    }

    public void OnAttack(Transform newTarget) {
        if (inDecayZone) {
            BeginCatch(newTarget);
        }
    }

    void Update() {
        timer += Time.deltaTime;

        // once caught, go straight into the book instead of going back to floating around
        if (caught) {
            if (attackTarget == null) {
                Destroy(gameObject);
                return;
            }

            position = Vector3.MoveTowards(position, attackTarget.position,
                Mathf.Max(0.01f, suckSpeed) * Time.deltaTime);
            transform.position = position;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / suckTime);
            if (timer >= suckTime) {
                Destroy(gameObject);
            }

            return;
        }
        if (attackTarget == null) {
            return;
        }

        if (timer < idleTime && !onAttack) { // floating around
            float bobX = Mathf.Sin(timer * bobFrequency + bobOffset.x) * amplitudes.x;
            float bobY = Mathf.Sin(3 * timer * bobFrequency + bobOffset.y) * amplitudes.y;
            float bobZ = Mathf.Sin(2 * timer * bobFrequency + bobOffset.z) * amplitudes.z;

            transform.position = position + new Vector3(bobX, bobY, bobZ);
        }
        else { // heading toward the player
            if (!onAttack) {
                position = transform.position;
                onAttack = true;
                Vector3 targetDirection = (attackTarget.position - transform.position).normalized;
            
                // curvy stuff
                if (targetDirection == Vector3.zero) {
                    Destroy(gameObject);
                    return;
                }
                Vector3 p = Vector3.zero;
                while (p == Vector3.zero) {
                    p = Vector3.Cross(targetDirection, new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)));
                }
                curvingVector = p.normalized;
                curvingPeriod = 2 * Mathf.PI * moveSpeed/Vector3.Distance(transform.position, attackTarget.position);
                timer = 0;
            }
            position = Vector3.MoveTowards(position, attackTarget.position, moveSpeed * Time.deltaTime);
            transform.position = position + curvingVector * Mathf.Sin(curvingPeriod * timer) * curveAmplitude;

            float distance = Vector3.Distance(position, attackTarget.position);

            if (distance <= deathRadius) {
                inDecayZone = false;
                Destroy(gameObject);
                Debug.Log("No Heals for you");
            }
            else if (distance <= decayRadius) {
                float t = 1 - (distance - deathRadius)/(decayRadius - deathRadius);
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, EasingFunc(t));

                if (!inDecayZone) inDecayZone = true;
            }
        }
    }
}
