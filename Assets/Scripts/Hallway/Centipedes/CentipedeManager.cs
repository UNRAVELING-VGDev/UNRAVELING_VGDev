using UnityEngine;
using UnityEngine.Pool;

public class CentipedeManager : MonoBehaviour
{
  [Header("References")]
  [SerializeField]
  private Centipede _centipedePrefab;

  [Tooltip("Box covering the room. Centipedes spawn on surfaces inside it.")]
  [SerializeField]
  private BoxCollider _spawnBounds;

  [SerializeField]
  private Transform _playerTarget;

  [SerializeField]
  private LayerMask _surfaceMask;

  [Header("Spawning")]
  [SerializeField]
  private bool _automaticSpawning = true;

  [SerializeField]
  private float _spawnInterval = 3f;

  [Tooltip("Don't spawn closer than this to the player.")]
  [SerializeField]
  private float _minDistanceFromPlayer = 3f;

  [Tooltip("How many random spots to try per spawn before giving up.")]
  [SerializeField]
  private int _spawnAttempts = 10;

  [Header("Pool")]
  [SerializeField]
  private int _defaultCapacity = 8;

  [SerializeField]
  private int _maxPoolSize = 32;

  private ObjectPool<Centipede> _pool;
  private float _spawnTimer;

  private void Awake()
  {
    _pool = new ObjectPool<Centipede>(
      CreateCentipede,
      c => c.gameObject.SetActive(true),
      c => c.gameObject.SetActive(false),
      c => Destroy(c.gameObject),
      collectionCheck: true,
      defaultCapacity: _defaultCapacity,
      maxSize: _maxPoolSize
    );
  }

  private Centipede CreateCentipede()
  {
    Centipede c = Instantiate(_centipedePrefab);
    c.gameObject.SetActive(false);
    c.SetPool(_pool);
    return c;
  }

  private void Update()
  {
    if (!_automaticSpawning)
      return;

    _spawnTimer -= Time.deltaTime;
    if (_spawnTimer <= 0f)
    {
      _spawnTimer = _spawnInterval;
      SpawnCentipede();
    }
  }

  public void SpawnBesideBoss(Transform boss, float sideOffset)
  {
    if (boss == null || _centipedePrefab == null || _playerTarget == null)
      return;

    SpawnAtSide(boss, -Mathf.Max(0.5f, sideOffset));
    SpawnAtSide(boss, Mathf.Max(0.5f, sideOffset));
  }

  private void SpawnAtSide(Transform boss, float signedOffset)
  {
    Vector3 origin = boss.position + boss.right * signedOffset + Vector3.up * 3f;
    RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 12f, _surfaceMask,
      QueryTriggerInteraction.Ignore);
    int nearest = -1;
    for (int i = 0; i < hits.Length; i++)
    {
      Collider surface = hits[i].collider;
      if (hits[i].normal.y < 0.5f || surface.GetComponentInParent<CentipedeBoss>() != null
        || surface.GetComponentInParent<Centipede>() != null
        || surface.GetComponentInParent<CharacterController>() != null)
        continue;

      if (nearest < 0 || hits[i].distance < hits[nearest].distance)
        nearest = i;
    }

    if (nearest < 0)
      return;

    RaycastHit hit = hits[nearest];
    Centipede centipede = _pool.Get();
    centipede.Init(_playerTarget, hit.point, hit.normal, _playerTarget.position - hit.point);
  }

  private void SpawnCentipede()
  {
    if (_centipedePrefab == null || _spawnBounds == null || _playerTarget == null)
      return;

    if (!FindRandomSurface(out RaycastHit hit))
    {
      Debug.LogWarning($"{name}: couldn't find a surface inside the spawn bounds");
      return;
    }

    Vector3 toPlayer = _playerTarget.position - hit.point;

    Centipede c = _pool.Get();
    c.Init(_playerTarget, hit.point, hit.normal, toPlayer);
  }

  private bool FindRandomSurface(out RaycastHit result)
  {
    Bounds b = _spawnBounds.bounds;
    float maxDistance = b.size.magnitude;

    for (int i = 0; i < _spawnAttempts; i++)
    {
      Vector3 origin = new Vector3(
        Random.Range(b.min.x, b.max.x),
        Random.Range(b.min.y, b.max.y),
        Random.Range(b.min.z, b.max.z)
      );

      Vector3 dir = Random.onUnitSphere;

      if (
        Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, _surfaceMask, QueryTriggerInteraction.Ignore)
        && b.Contains(hit.point)
        && Vector3.Distance(hit.point, _playerTarget.position) >= _minDistanceFromPlayer
      )
      {
        result = hit;
        return true;
      }
    }

    result = default;
    return false;
  }
}
