using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FireflyManager : MonoBehaviour
{
  [Header("References")]
  [SerializeField]
  private FireflyParticle _fireflyPrefab;

  [SerializeField]
  private Transform _spawnPoint;

  [SerializeField]
  private Transform _playerTarget;

  [SerializeField]
  private Transform _hallway;

  [Header("Lane Area (hallway's x/y)")]
  [SerializeField]
  private Vector2 _laneMin = new Vector2(-2f, 0.8f);

  [SerializeField]
  private Vector2 _laneMax = new Vector2(2f, 2.5f);

  [Header("Spawning")]
  [SerializeField]
  private float _spawnInterval = 1.5f;

  [SerializeField]
  private float _minLaneSpacing = 0.6f;

  [SerializeField]
  private int _recentLanesToAvoid = 5;

  [SerializeField]
  private int _laneAttempts = 8;

  [Header("Pool")]
  [SerializeField]
  private int _defaultCapacity = 16;

  [SerializeField]
  private int _maxPoolSize = 64;

  [Header("Gizmos")]
  [SerializeField]
  private float _gizmoLength = 20f;

  private readonly List<Vector2> _recentLanes = new List<Vector2>();
  private float _spawnTimer;
  private ObjectPool<FireflyParticle> _pool;

  private void Awake()
  {
    _pool = new ObjectPool<FireflyParticle>(
      CreateFirefly,
      OnGetFirefly,
      OnReleaseFirefly,
      OnDestroyFirefly,
      collectionCheck: true,
      defaultCapacity: _defaultCapacity,
      maxSize: _maxPoolSize
    );
  }

  private void Update()
  {
    _spawnTimer -= Time.deltaTime;
    if (_spawnTimer <= 0)
    {
      _spawnTimer = _spawnInterval;
      SpawnFirefly();
    }
  }

  private void OnGetFirefly(FireflyParticle f) => f.gameObject.SetActive(true);

  private void OnReleaseFirefly(FireflyParticle f) => f.gameObject.SetActive(false);

  private void OnDestroyFirefly(FireflyParticle f) => Destroy(f.gameObject);

  private FireflyParticle CreateFirefly()
  {
    FireflyParticle f = Instantiate(_fireflyPrefab);
    f.gameObject.SetActive(false);
    f.SetPool(_pool);
    return f;
  }

  private void SpawnFirefly()
  {
    if (_fireflyPrefab == null || _spawnPoint == null || _playerTarget == null || _hallway == null)
      return;

    Vector2 lane = PickLane();
    FireflyParticle f = _pool.Get();
    f.transform.SetPositionAndRotation(_spawnPoint.position, Quaternion.identity);
    f.init(lane, _playerTarget, _hallway);
  }

  private Vector2 PickLane()
  {
    Vector2 best = RandomLane();

    for (int i = 0; i < _laneAttempts; ++i)
    {
      Vector2 candidate = RandomLane();
      if (IsFarFromRecent(candidate))
      {
        best = candidate;
        break;
      }
    }

    _recentLanes.Add(best);
    if (_recentLanes.Count > _recentLanesToAvoid)
      _recentLanes.RemoveAt(0);

    return best;
  }

  private Vector2 RandomLane()
  {
    return new Vector2(Random.Range(_laneMin.x, _laneMax.x), Random.Range(_laneMin.y, _laneMax.y));
  }

  private bool IsFarFromRecent(Vector2 candidate)
  {
    float minSq = _minLaneSpacing * _minLaneSpacing;
    foreach (Vector2 lane in _recentLanes)
    {
      if ((lane - candidate).sqrMagnitude < minSq)
        return false;
    }
    return true;
  }

  private void OnDrawGizmosSelected()
  {
    if (_hallway == null)
      return;

    Gizmos.matrix = _hallway.localToWorldMatrix;
    Gizmos.color = Color.yellow;

    Vector2 center = (_laneMin + _laneMax) * 0.5f;
    Vector2 size = _laneMax - _laneMin;
    Gizmos.DrawWireCube(
      new Vector3(center.x, center.y, 0f),
      new Vector3(size.x, size.y, _gizmoLength)
    );
  }
}
