using UnityEngine;
using UnityEngine.Pool;

public class FireflyParticle : MonoBehaviour
{
  // states that the fireflies are in
  // changes each frame
  private enum FireflyState
  {
    Approach,
    Hover,
    Attack,
  };

  // setting most to all variables to private and only changing it to
  // public when it is nessessary
  [Header("Firefly Movement Settings")]
  [SerializeField]
  private float _moveSpeed = 3f;

  [SerializeField]
  private float _hoverRange = 4f;

  [SerializeField]
  private float _laneSmoothTime = 0.5f;

  [Header("Hover Settings")]
  [SerializeField]
  private float _hoveringTime = 3f;

  [SerializeField]
  private float _minHoverDistance = 1.5f;

  [SerializeField]
  private float _maxHoverDistance = 3f;

  [SerializeField]
  private float _bobAmp = 0.15f;

  [SerializeField]
  private float _bobFreq = 1f;

  [SerializeField]
  private float _smoothDriftTime = 0.4f;

  [Header("Lunge Settings")]
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
  private float _warningTime = 1f;

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

  [Header("Safety")]
  [SerializeField]
  private float _maxLifetime = 20f;

  private IObjectPool<FireflyParticle> _pool;
  private bool _released;

  private Transform _playerTarget;
  private Transform _hallway;
  private Vector2 _lane;
  private FireflyState _state;

  // variables needed for the approach state
  private Vector2 _laneVelocity;
  private float _hoverTime;
  private float _hoverDistance;
  private float _hoverSide;
  private float _bobSeed;
  private Vector3 _lungeTarget;
  private float _lifetime;

  // variables needed for the hover state
  private float _zOffset;
  private float _zOffsetVelocity;
  private Vector2 _hoverXY;

  // variables needed for glow/lights
  private MaterialPropertyBlock _propertyBlock;
  private int _emissonId;
  private float _blinkPeriod;
  private float _blinkPhase;

  public void SetPool(IObjectPool<FireflyParticle> owningPool) => _pool = owningPool;

  public void init(Vector2 assignedLane, Transform target, Transform hallwayRef)
  {
    _lane = assignedLane;
    _playerTarget = target;
    _hallway = hallwayRef;

    _state = FireflyState.Approach;
    _released = false;
    _laneVelocity = Vector2.zero;
    _hoverTime = 0f;
    _lifetime = 0f;
    _bobSeed = Random.Range(0f, 100f);

    _blinkPeriod = Random.Range(_minBlinkPeriod, _maxBlinkPeriod);
    _blinkPhase = Random.value;
    SetGlow(_minGlow);
  }

  private void Awake()
  {
    _propertyBlock = new MaterialPropertyBlock();
    _emissonId = Shader.PropertyToID(_emissionProperty);

    if (_glowRenderer == null)
      _glowRenderer = GetComponentInChildren<Renderer>();

    if (_glowLight == null)
      _glowLight = GetComponentInChildren<Light>();
    if (_glowLight != null)
      _glowLight.color = _glowColor;

    // if (_glowRenderer == null)
    // {
    //   Debug.LogWarning($"{name}: no glow renderer found");
    // }
    // else
    // {
    //   Material mat = _glowRenderer.sharedMaterial;
    //   Debug.Log(
    //     $"{name}: renderer={_glowRenderer.name}, "
    //       + $"shader={(mat != null ? mat.shader.name : "none")}, "
    //       + $"hasProperty={(mat != null && mat.HasProperty(_emissonId))}, "
    //       + $"emissionKeyword={(mat != null && mat.IsKeywordEnabled("_EMISSION"))}"
    //   );
    // }
  }

  public void Despawn()
  {
    if (_released)
      return;
    _released = true;

    if (_pool != null)
      _pool.Release(this);
    else
      Destroy(gameObject);
  }

  private void Update()
  {
    if (_playerTarget == null || _hallway == null)
      return;

    UpdateGlow();

    _lifetime += Time.deltaTime;
    if (_lifetime >= _maxLifetime)
    {
      Despawn();
      return;
    }

    switch (_state)
    {
      case FireflyState.Approach:
        UpdateApproach();
        break;
      case FireflyState.Attack:
        UpdateAttack();
        break;
      case FireflyState.Hover:
        UpdateHover();
        break;
    }
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
      bool warning = _state == FireflyState.Hover && _hoverTime <= _warningTime;
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

  private void UpdateApproach()
  {
    Vector3 localPosition = _hallway.InverseTransformPoint(transform.position);
    float playerZ = _hallway.InverseTransformPoint(_playerTarget.position).z;

    if (Mathf.Abs(playerZ - localPosition.z) <= _hoverRange)
    {
      EnterHover();
      return;
    }

    Vector2 xy = Vector2.SmoothDamp(
      new Vector2(localPosition.x, localPosition.y),
      _lane,
      ref _laneVelocity,
      _laneSmoothTime
    );
    float z = Mathf.MoveTowards(localPosition.z, playerZ, _moveSpeed * Time.deltaTime);

    transform.position = _hallway.TransformPoint(new Vector3(xy.x, xy.y, z));
  }

  private void EnterHover()
  {
    _state = FireflyState.Hover;
    _hoverTime = _hoveringTime;
    _hoverDistance = Random.Range(_minHoverDistance, _maxHoverDistance);

    Vector3 localPos = _hallway.InverseTransformPoint(transform.position);
    float playerZ = _hallway.InverseTransformPoint(_playerTarget.position).z;

    _zOffset = localPos.z - playerZ;
    _hoverSide = Mathf.Sign(_zOffset);
    _hoverXY = new Vector2(localPos.x, localPos.y);

    _zOffsetVelocity = 0f;
    _laneVelocity = Vector2.zero;
  }

  private void UpdateHover()
  {
    float playerZ = _hallway.InverseTransformPoint(_playerTarget.position).z;

    _zOffset = Mathf.SmoothDamp(
      _zOffset,
      _hoverSide * _hoverDistance,
      ref _zOffsetVelocity,
      _smoothDriftTime
    );

    _hoverXY = Vector2.SmoothDamp(_hoverXY, _lane, ref _laneVelocity, _smoothDriftTime);

    Vector3 localSpot = new Vector3(_hoverXY.x, _hoverXY.y, playerZ + _zOffset);
    transform.position = _hallway.TransformPoint(localSpot) + GetBobOffset();

    _hoverTime -= Time.deltaTime;
    if (_hoverTime <= 0)
    {
      _lungeTarget = _playerTarget.position;
      _state = FireflyState.Attack;
    }
  }

  private Vector3 GetBobOffset()
  {
    float y_coord = Time.time * _bobFreq;

    float x = Mathf.PerlinNoise(_bobSeed, y_coord) * 2f - 1f;
    float y = Mathf.PerlinNoise(_bobSeed + 701.901f, y_coord) * 2f - 1f;
    float z = Mathf.PerlinNoise(_bobSeed + 673.941f, y_coord) * 2f - 1f;

    return new Vector3(x, y, z) * _bobAmp;
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
      Despawn();
      return;
    }

    if ((_lungeTarget - transform.position).sqrMagnitude < 0.0001f)
    {
      Debug.Log($"{name} missed");
      Despawn();
    }
  }
}
