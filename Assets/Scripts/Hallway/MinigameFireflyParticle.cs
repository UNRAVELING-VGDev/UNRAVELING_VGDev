using UnityEngine;

public class MinigameFireflyParticle : MonoBehaviour
{
    // states
    private enum FireflyState
    {
        Drift,
        Windup,
        Attack,
    };

    [Header("Drift Settings")]
    [SerializeField]
    private float _driftSpeed = 1f;

    [SerializeField]
    private int _targetAttempts = 10;

    [SerializeField]
    private float _bobAmp = 0.15f;

    [SerializeField]
    private float _bobFreq = 1f;

    [Header("Attack Settings")]
    [SerializeField]
    private float _windupTime = 1f;

    [SerializeField]
    private float _lungeSpeed = 12f;

    [SerializeField]
    private float _hitRadius = 0.4f;

    [Header("Glow")]
    [SerializeField]
    private Renderer _glowRenderer;

    [SerializeField]
    private Color _glowColor = new Color(1f, 0.85f, 0.3f);

    [SerializeField]
    private float _glowIntensity = 6f;

    [SerializeField]
    private float _minGlow = 0.5f;

    [SerializeField]
    private float _minBlinkPeriod = 1.2f;

    [SerializeField]
    private float _maxBlinkPeriod = 2.5f;

    [SerializeField]
    private float _flashDuration = 0.35f;

    [SerializeField]
    private float _warningPeriod = 0.25f;

    [SerializeField]
    private string _emissionProperty = "_EmissionColor";

    [Header("Light")]
    [SerializeField]
    private float _lightIntensity = 2f;

    [SerializeField]
    private float _lightOffThreshold = 0.02f;

    [SerializeField]
    private Light _glowLight;

    // set by manager
    private BoxCollider _bounds;
    private Transform _playerTarget;
    private float _avoidRadius;
    private float _padding;

    // state
    private FireflyState _state;
    private Vector3 _driftPos;
    private Vector3 _segStart;
    private Vector3 _segEnd;
    private float _segTime;
    private float _segDuration;
    private bool _hasTarget;
    private Vector3 _lungeTarget;
    private float _windupLeft;
    private float _bobSeed;

    // glow
    private MaterialPropertyBlock _propertyBlock;
    private int _emissonId;
    private float _blinkPeriod;
    private float _blinkPhase;

    public bool IsIdle => _state == FireflyState.Drift;

    public void init(BoxCollider bounds, Transform target, float avoidRadius, float padding)
    {
        _bounds = bounds;
        _playerTarget = target;
        _avoidRadius = avoidRadius;
        _padding = padding;

        _state = FireflyState.Drift;
        _bobSeed = Random.Range(0f, 100f);
        _blinkPeriod = Random.Range(_minBlinkPeriod, _maxBlinkPeriod);
        _blinkPhase = Random.value;
        SetGlow(_minGlow);

        _driftPos = RandomPoint();
        transform.position = _driftPos;
        PickTarget();
    }

    private void Awake()
    {
        // no physics
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        rb.isKinematic = true;
        foreach (Collider col in GetComponentsInChildren<Collider>())
        col.enabled = false;

        _propertyBlock = new MaterialPropertyBlock();
        _emissonId = Shader.PropertyToID(_emissionProperty);

        if (_glowRenderer == null)
        _glowRenderer = GetComponentInChildren<Renderer>();

        if (_glowLight == null)
        _glowLight = GetComponentInChildren<Light>();
        if (_glowLight != null)
        _glowLight.color = _glowColor;
    }

    // called by manager
    public void Attack()
    {
        if (_state != FireflyState.Drift)
        return;

        _state = FireflyState.Windup;
        _windupLeft = _windupTime;
    }

    private void Update()
    {
        if (_bounds == null || _playerTarget == null)
        return;

        UpdateGlow();

        switch (_state)
        {
        case FireflyState.Drift:
            Drift();
            break;
        case FireflyState.Windup:
            UpdateWindup();
            break;
        case FireflyState.Attack:
            UpdateAttack();
            break;
        }
    }

    private void Drift()
    {
        // retry pick
        if (!_hasTarget)
        PickTarget();

        if (_hasTarget)
        {
        _segTime += Time.deltaTime;
        float t = Mathf.Clamp01(_segTime / _segDuration);
        _driftPos = Vector3.Lerp(_segStart, _segEnd, Mathf.SmoothStep(0f, 1f, t));

        if (t >= 1f)
            PickTarget();
        }

        transform.position = PushOut(_driftPos + GetBobOffset());
    }

    private void UpdateWindup()
    {
        Drift();

        _windupLeft -= Time.deltaTime;
        if (_windupLeft <= 0f)
        {
        // lock target
        _lungeTarget = _playerTarget.position;
        _state = FireflyState.Attack;
        }
    }

    private void UpdateAttack()
    {
        transform.position = Vector3.MoveTowards(
        transform.position,
        _lungeTarget,
        _lungeSpeed * Time.deltaTime
        );

        if ((_playerTarget.position - transform.position).sqrMagnitude <= _hitRadius * _hitRadius)
        {
        Debug.Log($"{name} hit the player");
        Respawn();
        return;
        }

        if ((_lungeTarget - transform.position).sqrMagnitude < 0.0001f)
        {
        Debug.Log($"{name} missed");
        Respawn();
        }
    }

    private void Respawn()
    {
        _driftPos = RandomPoint();
        transform.position = _driftPos;
        _state = FireflyState.Drift;
        PickTarget();
    }

    // straight hop
    private void PickTarget()
    {
        _hasTarget = false;

        for (int i = 0; i < _targetAttempts; ++i)
        {
        Vector3 candidate = RandomPoint();
        if (PathBlocked(_driftPos, candidate))
            continue;

        _segStart = _driftPos;
        _segEnd = candidate;
        _segTime = 0f;
        _segDuration = Mathf.Max(Vector3.Distance(_segStart, _segEnd) / _driftSpeed, 0.1f);
        _hasTarget = true;
        return;
        }
    }

    private Vector3 RandomPoint()
    {
        Bounds b = _bounds.bounds;
        Vector3 p = b.center;

        for (int i = 0; i < 20; ++i)
        {
        p = new Vector3(
            Random.Range(b.min.x + _padding, b.max.x - _padding),
            Random.Range(b.min.y + _padding, b.max.y - _padding),
            Random.Range(b.min.z + _padding, b.max.z - _padding)
        );
        if (!InSafeZone(p))
            return p;
        }
        return PushOut(p);
    }

    private bool InSafeZone(Vector3 p)
    {
        Vector3 c = _bounds.bounds.center;
        float dx = p.x - c.x;
        float dz = p.z - c.z;
        return dx * dx + dz * dz < _avoidRadius * _avoidRadius;
    }

    // safety net
    private Vector3 PushOut(Vector3 p)
    {
        Vector3 c = _bounds.bounds.center;
        Vector2 flat = new Vector2(p.x - c.x, p.z - c.z);

        if (flat.sqrMagnitude >= _avoidRadius * _avoidRadius)
        return p;

        flat = flat.sqrMagnitude < 0.0001f ? Vector2.right : flat.normalized;
        flat *= _avoidRadius;
        return new Vector3(c.x + flat.x, p.y, c.z + flat.y);
    }

    // segment vs circle
    private bool PathBlocked(Vector3 from, Vector3 to)
    {
        Vector3 center = _bounds.bounds.center;
        Vector2 c = new Vector2(center.x, center.z);
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 ab = new Vector2(to.x, to.z) - a;

        float lenSq = ab.sqrMagnitude;
        float t = lenSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(c - a, ab) / lenSq) : 0f;

        return (a + ab * t - c).sqrMagnitude < _avoidRadius * _avoidRadius;
    }

    private Vector3 GetBobOffset()
    {
        float y_coord = Time.time * _bobFreq;

        float x = Mathf.PerlinNoise(_bobSeed, y_coord) * 2f - 1f;
        float y = Mathf.PerlinNoise(_bobSeed + 701.901f, y_coord) * 2f - 1f;
        float z = Mathf.PerlinNoise(_bobSeed + 673.941f, y_coord) * 2f - 1f;

        return new Vector3(x, y, z) * _bobAmp;
    }

    private void UpdateGlow()
    {
        float brightness;

        if (_state == FireflyState.Attack)
        {
        brightness = 1f;
        }
        else
        {
        // fast blink
        bool warning = _state == FireflyState.Windup;
        float period = warning ? _warningPeriod : _blinkPeriod;

        _blinkPhase += Time.deltaTime / period;
        _blinkPhase -= Mathf.Floor(_blinkPhase);

        float blinkTime = Mathf.Min(_flashDuration / period, 0.5f);
        brightness = _blinkPhase < blinkTime ? Mathf.Sin(Mathf.PI * _blinkPhase / blinkTime) : 0f;
        }

        SetGlow(Mathf.Lerp(_minGlow, 1f, brightness));
    }

    private void SetGlow(float amount)
    {
        if (_glowLight != null)
        {
        bool lightOn = amount > _lightOffThreshold;
        if (_glowLight.enabled != lightOn)
            _glowLight.enabled = lightOn;
        if (lightOn)
            _glowLight.intensity = _lightIntensity * amount;
        }

        if (_glowRenderer == null)
        return;

        _glowRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(_emissonId, _glowColor * (_glowIntensity * amount));
        _glowRenderer.SetPropertyBlock(_propertyBlock);
    }
}