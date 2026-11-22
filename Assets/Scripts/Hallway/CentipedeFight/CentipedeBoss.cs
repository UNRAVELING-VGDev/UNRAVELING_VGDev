using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Random=UnityEngine.Random;

public class CentipedeBoss : MonoBehaviour
{
    [Header("Probability")]
    [SerializeField] float _pullbackProbability = 1f;
    [SerializeField] float _whipProbability = 1f;
    [SerializeField] float _acidProbability = 1f;
    [Range(0f, 1f)] [SerializeField] float _lungeSpinProbability = 0.5f;
    [Min(0f)] [SerializeField] float _lungeSpinDelay = 0.12f;
    [Min(0f)] [SerializeField] float _spinAttackRange = 18f;

    [Header("Timing")]
    [SerializeField] float _timeBetweenAttacks = 1.25f;
    [SerializeField] float _timeVariance = 0.4f;

    [Header("Crawl")]
    [SerializeField] Transform _player;
    [SerializeField] float _crawlSpeed = 12f;
    [SerializeField] float _preferredDistance = 7f;
    [SerializeField] float _distanceSlack = 3f;
    [SerializeField] float _turnSpeed = 180f;
    [SerializeField] float _settleDuration = 0.5f;
    [SerializeField] float _weaveAngle = 7f;

    [SerializeField] PullbackAttack _pullback;
    [SerializeField] CentipedeAttack _whip;
    [SerializeField] CentipedeAttack _acid;

    [Header("Arena")]
    [SerializeField] Collider _arenaBounds;
    [Min(0f)] [SerializeField] float _wallClearance = 0.5f;
    Renderer[] _bodyRenderers;

    [Header("Health")]
    [SerializeField] float health = 500f;

    public float Health => health;
    public float MaxHealth { get; private set; }
    public float NormalizedHealth => MaxHealth > 0f ? health / MaxHealth : 0f;
    public event Action HealthChanged;

    void Awake()
    {
        MaxHealth = Mathf.Max(0f, health);
        health = MaxHealth;
        HealthChanged?.Invoke();

        if (_pullback == null)
            _pullback = GetComponent<PullbackAttack>();
        if (_whip == null)
            _whip = GetComponent<WhipAttack>();
        if (_acid == null)
            _acid = GetComponent<AcidAttack>();
        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _player = player.transform;
        }
    }

    void Start()
    {
        _bodyRenderers = GetComponentsInChildren<Renderer>(true);
        StartCoroutine(AttackLoop());
    }

    void LateUpdate()
    {
        transform.position = ConstrainPosition(transform.position);
    }

    public Vector3 ConstrainPosition(Vector3 position)
    {
        if (_arenaBounds == null || _bodyRenderers == null)
            return position;

        Bounds body = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer renderer in _bodyRenderers)
        {
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                body.Encapsulate(renderer.bounds);
        }

        Bounds arena = _arenaBounds.bounds;
        Vector3 offset = position - transform.position;
        float minX = arena.min.x + _wallClearance - body.min.x;
        float maxX = arena.max.x - _wallClearance - body.max.x;
        float minZ = arena.min.z + _wallClearance - body.min.z;
        float maxZ = arena.max.z - _wallClearance - body.max.z;
        offset.x = minX <= maxX ? Mathf.Clamp(offset.x, minX, maxX) : (minX + maxX) * 0.5f;
        offset.z = minZ <= maxZ ? Mathf.Clamp(offset.z, minZ, maxZ) : (minZ + maxZ) * 0.5f;
        return transform.position + offset;
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return Crawl(GapDuration());

            CentipedeAttack attack = ChooseAttack();
            if (attack == null)
                continue;

            attack.Play();
            while (attack.IsPlaying)
                yield return null;

            if (attack == _pullback && _whip is WhipAttack spin && spin.isActiveAndEnabled
                && Random.value < _lungeSpinProbability)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, _lungeSpinDelay));
                spin.PlayFollowup();
                while (spin.IsPlaying)
                    yield return null;
            }
        }
    }

    float GapDuration()
    {
        float variance = Mathf.Max(0f, _timeVariance);
        float min = Mathf.Max(0.05f, _timeBetweenAttacks - variance);
        float max = Mathf.Max(min, _timeBetweenAttacks + variance);
        return Random.Range(min, max);
    }

    IEnumerator Crawl(float duration)
    {
        if (_player == null || duration <= 0f)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        float orbit = Random.value < 0.5f ? -1f : 1f;
        float settle = Mathf.Min(_settleDuration, duration * 0.45f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            float speedScale = 1f;
            if (settle > 0f && remaining < settle)
                speedScale = Mathf.SmoothStep(0f, 1f, remaining / settle);

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 toward = distance > 0.001f ? toPlayer / distance : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, toward) * orbit;
            float radial = 0f;
            if (_distanceSlack > 0.001f)
                radial = Mathf.Clamp((distance - _preferredDistance) / _distanceSlack, -1f, 1f);

            Vector3 wish = side + toward * radial;
            if (wish.sqrMagnitude > 0.001f)
                wish.Normalize();

            Vector3 next = transform.position + wish * (_crawlSpeed * speedScale * Time.deltaTime);
            next.y = transform.position.y;
            transform.position = ConstrainPosition(next);

            float weave = Mathf.Sin(elapsed * 2.4f) * _weaveAngle * speedScale;
            Quaternion face = Quaternion.LookRotation(toward, Vector3.up) * Quaternion.Euler(0f, weave, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, face, _turnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    bool IsPlayerInSpinRange()
    {
        if (_player == null)
            return false;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float threshold = Mathf.Max(0f, _spinAttackRange);
        return toPlayer.sqrMagnitude < threshold * threshold;
    }

    CentipedeAttack ChooseAttack()
    {
        float pullback = _pullback != null && _pullback.isActiveAndEnabled
            ? Mathf.Max(0f, _pullbackProbability) : 0f;
        float whip = _whip != null && _whip.isActiveAndEnabled && IsPlayerInSpinRange()
            ? Mathf.Max(0f, _whipProbability) : 0f;
        float acid = _acid != null && _acid.isActiveAndEnabled
            ? Mathf.Max(0f, _acidProbability) : 0f;
        float total = pullback + whip + acid;
        if (total <= 0f)
            return null;

        float roll = Random.value * total;
        if (roll < pullback)
            return _pullback;
        if (roll < pullback + whip)
            return _whip;
        return _acid;
    }

    public void OnHit(float damage)
    {
        if (damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage) || health <= 0f)
            return;

        health = Mathf.Max(0f, health - damage);
        HealthChanged?.Invoke();

        if (health <= 0)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
