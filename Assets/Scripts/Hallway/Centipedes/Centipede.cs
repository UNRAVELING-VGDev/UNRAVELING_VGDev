using UnityEngine;
using UnityEngine.Pool;

public class Centipede : MonoBehaviour
{
  private enum CentipedeState
  {
    Crawl,
    WindUp,
    Leap,
  };

  [Header("Surface")]
  [SerializeField]
  private LayerMask _surfaceMask;

  [SerializeField]
  private float _probeHeight = 0.3f;

  [SerializeField]
  private float _probeDepth = 0.6f;

  [SerializeField]
  private float _maxStep = 0.05f;

  [SerializeField]
  private float _surfaceOffset = 0.05f;

  [SerializeField]
  private float _normalSmoothing = 12f;

  [Header("Crawl")]
  [SerializeField]
  private float _crawlSpeed = 2f;

  [SerializeField]
  private float _turnRate = 4f;

  [SerializeField]
  private float _wanderAngle = 45f;

  [SerializeField]
  private float _wanderFrequency = 0.5f;

  [Header("Body")]
  [SerializeField]
  private Transform[] _segments;

  // [SerializeField]
  // private Transform[] _legPivots;

  [SerializeField]
  private float _segmentSpacing = 0.25f;

  [SerializeField]
  private float _sampleSpacing = 0.05f;

  // [SerializeField]
  // private float _legSwingAngle = 20f;
  // [SerializeField]
  // private float _legSwingFrequency = 10f;
  // [SerializeField]
  // private float _legPhaseStep = 0.8f;

  [Header("Attack")]
  [SerializeField]
  private float _leapRange = 4f;

  [SerializeField]
  private float _windUpTime = 0.8f;

  [SerializeField]
  private float _rearHeight = 0.4f;

  [SerializeField]
  private float _rearPullback = 0.2f;

  [SerializeField]
  private float _windUpShake = 0.03f;

  [SerializeField]
  private float _leapFlightTime = 0.5f;

  [SerializeField]
  private float _gravity = 9.81f;

  [SerializeField]
  private float _hitRadius = 0.4f;

  [SerializeField]
  private float _attackCooldown = 2f;

  [SerializeField]
  private float _maxLeapTime = 2f;

  [Header("Tracking")]
  [SerializeField]
  private float _leadAmount = 1f;

  [SerializeField]
  private float _velocitySmoothing = 10f;

  [Header("Safety")]
  [SerializeField]
  private float _maxLifetime = 30f;

  private Transform _playerTarget;

  private IObjectPool<Centipede> _pool;
  private float _lifetime;

  private CentipedeState _state;
  private bool _released;
  private float _cooldown;
  private float _stateTimer;
  private Vector3 _windUpOrigin;
  private Vector3 _leapVelocity;

  private Vector3[] _pathPos;
  private Vector3[] _pathNormal;
  private int _pathNewest;
  private int _pathCount;
  private float _distSinceSample;

  private float _wanderSeed;
  private Vector3 _headPos;
  private Vector3 _headNormal;
  private Vector3 _smoothedNormal;
  private Vector3 _headForward;
  private Quaternion _headRotation;

  private Vector3 _playerVelocity;
  private Vector3 _lastPlayerPos;
  private bool _hasLastPlayerPos;

  public void SetPool(IObjectPool<Centipede> owningPool) => _pool = owningPool;

  public void Init(
    Transform target,
    Vector3 surfacePoint,
    Vector3 surfaceNormal,
    Vector3 initialForward
  )
  {
    _playerTarget = target;
    _state = CentipedeState.Crawl;
    _released = false;

    _wanderSeed = Random.Range(0f, 100f);
    _lifetime = 0f;
    _cooldown = 0f;
    _stateTimer = 0f;
    _leapVelocity = Vector3.zero;
    _playerVelocity = Vector3.zero;
    _hasLastPlayerPos = false;

    AttachTo(surfacePoint, surfaceNormal, initialForward);
  }

  private void Update()
  {
    if (_playerTarget == null)
      return;

    _lifetime += Time.deltaTime;
    if (_lifetime >= _maxLifetime)
    {
      Despawn();
      return;
    }

    _cooldown -= Time.deltaTime;
    SmoothNormal();
    TrackPlayerVelocity();

    switch (_state)
    {
      case CentipedeState.Crawl:
        UpdateCrawl();
        break;
      case CentipedeState.WindUp:
        UpdateWindUp();
        break;
      case CentipedeState.Leap:
        UpdateLeap();
        break;
    }

    if (_released)
      return;

    ApplyHeadTransform();
    UpdateBody();
    // UpdateLegs();
  }

  private void Awake()
  {
    int segmentCount = _segments != null ? _segments.Length : 0;
    float bodyLength = (segmentCount + 1) * _segmentSpacing;
    int capacity = Mathf.CeilToInt(bodyLength / _sampleSpacing) + 4;

    _pathPos = new Vector3[capacity];
    _pathNormal = new Vector3[capacity];
  }

  private void AttachTo(Vector3 point, Vector3 normal, Vector3 forward)
  {
    _headPos = point;
    _headNormal = normal;
    _smoothedNormal = normal;

    _headForward = Vector3.ProjectOnPlane(forward, normal);
    if (_headForward.sqrMagnitude < 0.0001f)
      _headForward = Vector3.Cross(normal, Vector3.right);
    if (_headForward.sqrMagnitude < 0.0001f)
      _headForward = Vector3.Cross(normal, Vector3.forward);
    _headForward.Normalize();

    _headRotation = SafeLookRotation(_headForward, _smoothedNormal);
    ApplyHeadTransform();
    ResetPath(point, normal);
    UpdateBody();
  }

  private void SmoothNormal()
  {
    float t = 1f - Mathf.Exp(-_normalSmoothing * Time.deltaTime);
    _smoothedNormal = Vector3.Slerp(_smoothedNormal, _headNormal, t).normalized;
  }

  private void ApplyHeadTransform()
  {
    Vector3 pos = _headPos + _smoothedNormal * _surfaceOffset;
    transform.SetPositionAndRotation(pos, _headRotation);
  }

  private void MoveAlongSurface(float distance)
  {
    int steps = Mathf.Max(1, Mathf.CeilToInt(distance / _maxStep));
    float step = distance / steps;

    for (int i = 0; i < steps; i++)
    {
      Vector3 before = _headPos;
      if (!StepAlongSurface(step))
      {
        _headForward = -_headForward;
        break;
      }
      RecordPath(Vector3.Distance(before, _headPos));
    }
  }

  private bool StepAlongSurface(float step)
  {
    Vector3 up = _headNormal;
    Vector3 fwd = _headForward;
    RaycastHit hit;

    // Concave corner or bump: a surface directly ahead (floor meeting a wall)
    Vector3 lowOrigin = _headPos + up * (_probeHeight * 0.5f);
    if (Physics.Raycast(lowOrigin, fwd, out hit, step + _probeHeight * 0.5f, _surfaceMask))
    {
      SetSurface(hit.point, hit.normal);
      return true;
    }

    // Normal case: surface below the point just ahead
    Vector3 ahead = _headPos + fwd * step;
    if (
      Physics.Raycast(
        ahead + up * _probeHeight,
        -up,
        out hit,
        _probeHeight + _probeDepth,
        _surfaceMask
      )
    )
    {
      SetSurface(hit.point, hit.normal);
      return true;
    }

    // Convex edge: the surface dropped away, so look back underneath the edge
    Vector3 under = ahead - up * _probeDepth;
    if (Physics.Raycast(under, -fwd, out hit, step + _probeDepth, _surfaceMask))
    {
      SetSurface(hit.point, hit.normal);
      return true;
    }

    return false;
  }

  private void SetSurface(Vector3 point, Vector3 normal)
  {
    Vector3 newForward = Vector3.ProjectOnPlane(_headForward, normal);

    if (newForward.sqrMagnitude < 0.01f)
    {
      float sign = Vector3.Dot(normal, _headForward) < 0f ? 1f : -1f;
      newForward = Vector3.ProjectOnPlane(_headNormal * sign, normal);
    }

    if (newForward.sqrMagnitude > 0.0001f)
      _headForward = newForward.normalized;

    _headNormal = normal;
    _headPos = point;
  }

  private Quaternion SafeLookRotation(Vector3 forward, Vector3 up)
  {
    return SafeLookRotation(forward, up, transform.up);
  }

  private Quaternion SafeLookRotation(Vector3 forward, Vector3 up, Vector3 fallbackUp)
  {
    if (forward.sqrMagnitude < 0.000001f)
      return Quaternion.LookRotation(transform.forward, up);

    if (Mathf.Abs(Vector3.Dot(forward.normalized, up.normalized)) > 0.999f)
      up = fallbackUp;

    return Quaternion.LookRotation(forward, up);
  }

  private void UpdateCrawl()
  {
    Vector3 toPlayer = _playerTarget.position - _headPos;

    if (_cooldown <= 0f && toPlayer.sqrMagnitude <= _leapRange * _leapRange)
    {
      EnterWindUp();
      return;
    }

    Vector3 desired = Vector3.ProjectOnPlane(toPlayer, _headNormal);
    if (desired.sqrMagnitude < 0.0001f)
      desired = _headForward;
    desired.Normalize();

    float wander =
      (Mathf.PerlinNoise(_wanderSeed, Time.time * _wanderFrequency) * 2f - 1f) * _wanderAngle;
    desired = Quaternion.AngleAxis(wander, _headNormal) * desired;

    _headForward = Vector3.RotateTowards(_headForward, desired, _turnRate * Time.deltaTime, 0f);
    Vector3 projected = Vector3.ProjectOnPlane(_headForward, _headNormal);
    if (projected.sqrMagnitude > 0.0001f)
      _headForward = projected.normalized;

    MoveAlongSurface(_crawlSpeed * Time.deltaTime);
    _headRotation = SafeLookRotation(_headForward, _smoothedNormal);
  }

  private void ResetPath(Vector3 point, Vector3 normal)
  {
    for (int i = 0; i < _pathPos.Length; i++)
    {
      _pathPos[i] = point;
      _pathNormal[i] = normal;
    }
    _pathNewest = 0;
    _pathCount = _pathPos.Length;
    _distSinceSample = 0f;
  }

  private void RecordPath(float distance)
  {
    _distSinceSample += distance;
    if (_distSinceSample < _sampleSpacing)
      return;
    _distSinceSample = 0f;

    _pathNewest = (_pathNewest + 1) % _pathPos.Length;
    _pathPos[_pathNewest] = _headPos;
    _pathNormal[_pathNewest] = _smoothedNormal;
    if (_pathCount < _pathPos.Length)
      _pathCount++;
  }

  private void UpdateBody()
  {
    if (_segments == null || _segments.Length == 0)
      return;

    int cap = _pathPos.Length;
    Vector3 prevPoint = _headPos;
    Vector3 prevNormal = _smoothedNormal;
    Vector3 aheadPos = _headPos;
    float walked = 0f;
    float targetDist = _segmentSpacing;
    int seg = 0;

    for (int i = 0; i < _pathCount && seg < _segments.Length; i++)
    {
      int idx = (_pathNewest - i + cap) % cap;
      Vector3 p = _pathPos[idx];
      Vector3 n = _pathNormal[idx];
      float d = Vector3.Distance(prevPoint, p);

      while (seg < _segments.Length && walked + d >= targetDist)
      {
        float t = d > 0f ? (targetDist - walked) / d : 0f;
        Vector3 pos = Vector3.Lerp(prevPoint, p, t);
        Vector3 nrm = Vector3.Slerp(prevNormal, n, t).normalized;

        PlaceSegment(seg, pos, nrm, aheadPos);
        aheadPos = pos;
        seg++;
        targetDist += _segmentSpacing;
      }

      walked += d;
      prevPoint = p;
      prevNormal = n;
    }

    for (; seg < _segments.Length; seg++)
    {
      PlaceSegment(seg, prevPoint, prevNormal, aheadPos);
      aheadPos = prevPoint;
    }
  }

  private void PlaceSegment(int index, Vector3 pos, Vector3 normal, Vector3 aheadPos)
  {
    Transform segment = _segments[index];
    if (segment == null)
      return;

    Vector3 facing = aheadPos - pos;
    if (facing.sqrMagnitude < 0.000001f)
      facing = segment.forward;

    Vector3 worldPos = pos + normal * _surfaceOffset;
    segment.SetPositionAndRotation(worldPos, SafeLookRotation(facing, normal, segment.up));
  }

  // private void UpdateLegs()
  // {
  //   if (_legPivots == null)
  //   return;
  //
  //   for (int i = 0; i < _legPivots.Length; i++)
  //   {
  //     if (_legPivots[i] == null)
  //       continue;
  //
  //     float amount = _state == CentipedeState.Crawl ? 1f : 0f;
  //     float swing = Mathf.Sin(Time.time * _legSwingFrequency - i * _legPhaseStep) * _legSwingAngle * amount;
  //     _legPivots[i].localRotation = Quaternion.AngleAxis(swing, Vector3.up);
  //   }
  // }

  private void EnterWindUp()
  {
    _state = CentipedeState.WindUp;
    _stateTimer = _windUpTime;
    _windUpOrigin = _headPos;

    Vector3 toPlayer = Vector3.ProjectOnPlane(_playerTarget.position - _headPos, _headNormal);
    if (toPlayer.sqrMagnitude > 0.0001f)
      _headForward = toPlayer.normalized;
  }

  private void UpdateWindUp()
  {
    _stateTimer -= Time.deltaTime;
    float progress = 1f - Mathf.Clamp01(_stateTimer / _windUpTime);
    float rear = Mathf.SmoothStep(0f, 1f, progress);

    _headPos =
      _windUpOrigin
      + _headNormal * (_rearHeight * rear)
      - _headForward * (_rearPullback * rear)
      + Random.insideUnitSphere * (_windUpShake * rear);

    Vector3 toPlayer = PredictPlayerPosition(_leapFlightTime) - _headPos;
    _headRotation = SafeLookRotation(toPlayer, _smoothedNormal);

    if (_stateTimer <= 0f)
      EnterLeap();
  }

  private void EnterLeap()
  {
    _state = CentipedeState.Leap;
    _stateTimer = 0f;

    Vector3 gravity = Vector3.down * _gravity;
    Vector3 start = _headPos;
    float t = _leapFlightTime;
    Vector3 target = PredictPlayerPosition(t);

    _leapVelocity = (target - start - 0.5f * gravity * t * t) / t;
  }

  private void UpdateLeap()
  {
    _stateTimer += Time.deltaTime;

    if (_stateTimer >= _maxLeapTime)
    {
      Despawn();
      return;
    }

    Vector3 prev = _headPos;
    _leapVelocity += Vector3.down * _gravity * Time.deltaTime;
    Vector3 next = prev + _leapVelocity * Time.deltaTime;

    if ((_playerTarget.position - next).sqrMagnitude <= _hitRadius * _hitRadius)
    {
      Debug.Log($"{name} hit the player");
      Despawn();
      return;
    }

    Vector3 move = next - prev;
    if (Physics.Raycast(prev, move.normalized, out RaycastHit hit, move.magnitude, _surfaceMask))
    {
      Debug.Log($"{name} missed and landed");
      _headForward = _leapVelocity;
      SetSurface(hit.point, hit.normal);
      _state = CentipedeState.Crawl;
      _cooldown = _attackCooldown;
      RecordPath(Vector3.Distance(prev, _headPos));
      _headRotation = SafeLookRotation(_headForward, _smoothedNormal);
      return;
    }

    _headPos = next;
    RecordPath(move.magnitude);
    _headRotation = SafeLookRotation(_leapVelocity, _smoothedNormal);
  }

  private void TrackPlayerVelocity()
  {
    Vector3 pos = _playerTarget.position;

    if (_hasLastPlayerPos && Time.deltaTime > 0f)
    {
      Vector3 frameVelocity = (pos - _lastPlayerPos) / Time.deltaTime;
      float t = 1f - Mathf.Exp(-_velocitySmoothing * Time.deltaTime);
      _playerVelocity = Vector3.Lerp(_playerVelocity, frameVelocity, t);
    }

    _lastPlayerPos = pos;
    _hasLastPlayerPos = true;
  }

  private Vector3 PredictPlayerPosition(float time)
  {
    return _playerTarget.position + _playerVelocity * (time * _leadAmount);
  }

  private void Despawn()
  {
    if (_released)
      return;
    _released = true;

    if (_pool != null)
      _pool.Release(this);
    else
      Destroy(gameObject);
  }
}
