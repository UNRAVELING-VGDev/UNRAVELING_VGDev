using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinigameFireflyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MinigameFireflyParticle _fireflyPrefab;

    [SerializeField]
    private Transform _playerTarget;

    [SerializeField]
    private BoxCollider _bounds;

    [Header("Spawning")]
    [SerializeField]
    private int _fireflyCount = 8;

    [SerializeField]
    private float _safeRadius = 2f;

    [SerializeField]
    private float _edgeMargin = 0.75f;

    [SerializeField]
    private float _padding = 0.5f;

    [Header("Attacking")]
    [SerializeField]
    private float _minAttackDelay = 3f;

    [SerializeField]
    private float _maxAttackDelay = 8f;

    [Header("Gizmos")]
    [SerializeField]
    private int _circleSegments = 32;

    private readonly List<MinigameFireflyParticle> _fireflies = new List<MinigameFireflyParticle>();
    private readonly List<MinigameFireflyParticle> _idle = new List<MinigameFireflyParticle>();

    private void Start()
    {
        if (_fireflyPrefab == null || _playerTarget == null || _bounds == null)
        return;

        for (int i = 0; i < _fireflyCount; ++i)
        {
        MinigameFireflyParticle f = Instantiate(_fireflyPrefab);
        f.init(_bounds, _playerTarget, _safeRadius + _edgeMargin, _padding);
        _fireflies.Add(f);
        }

        StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
        yield return new WaitForSeconds(Random.Range(_minAttackDelay, _maxAttackDelay));

        MinigameFireflyParticle attacker = PickIdle();
        if (attacker != null)
            attacker.Attack();
        }
    }

    private MinigameFireflyParticle PickIdle()
    {
        _idle.Clear();
        foreach (MinigameFireflyParticle f in _fireflies)
        {
        if (f.IsIdle)
            _idle.Add(f);
        }

        return _idle.Count == 0 ? null : _idle[Random.Range(0, _idle.Count)];
    }

    private void OnDrawGizmosSelected()
    {
        if (_bounds == null)
        return;

        Bounds b = _bounds.bounds;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size);

        // safe zone
        Gizmos.color = Color.red;
        DrawCircle(new Vector3(b.center.x, b.min.y, b.center.z), _safeRadius);
        DrawCircle(new Vector3(b.center.x, b.max.y, b.center.z), _safeRadius);

        // firefly limit
        Gizmos.color = new Color(1f, 0.5f, 0f);
        DrawCircle(new Vector3(b.center.x, b.min.y, b.center.z), _safeRadius + _edgeMargin);
        DrawCircle(new Vector3(b.center.x, b.max.y, b.center.z), _safeRadius + _edgeMargin);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= _circleSegments; ++i)
        {
        float a = i * Mathf.PI * 2f / _circleSegments;
        Vector3 next = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        Gizmos.DrawLine(prev, next);
        prev = next;
        }
    }
}