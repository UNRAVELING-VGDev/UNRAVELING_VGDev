using System.Collections.Generic;
using UnityEngine;

public class LightBeamAdjustment : MonoBehaviour
{
    [Header("Flashlight")]
    public float defaultSpotAngle;
    public float minIntensity;

    [Header("Beam")]
    public Material beamMaterial;
    public Transform lightSource;
    [Min(0.001f)] public float beamThickness = 0.2f;
    [Min(0.01f)] public float maxDistance = 30f;
    public CentipedeBoss centipede;
    [Min(0f)] public float beamDamage = 1f;
    [Min(0.01f)] public float damageInterval = 0.1f;
    [Range(0, 1)] public int maxReflections = 1;

    struct BeamTarget
    {
        public CentipedeBoss boss;
        public Centipede small;
    }

    readonly List<GameObject> _beams = new List<GameObject>();
    RaycastHit[] _hits = new RaycastHit[64];
    readonly List<BeamTarget> _damageTargets = new List<BeamTarget>(2);
    readonly HashSet<Centipede> _segmentHits = new HashSet<Centipede>();
    int _hitCount;
    Light _handLight;
    Transform _owner;
    float _damageTimer;
    bool _isFiring;

    public event System.Action BeamFired;

    void Start()
    {
        _handLight = GetComponent<Light>();
        CharacterController owner = GetComponentInParent<CharacterController>();
        _owner = owner != null ? owner.transform : transform;
        if (_handLight != null)
        {
            _handLight.spotAngle = Mathf.Clamp(defaultSpotAngle, 1f, 179f);
            _handLight.innerSpotAngle = _handLight.spotAngle;
            _handLight.intensity = minIntensity;
        }
    }

    void LateUpdate()
    {
        _damageTimer = Mathf.Max(-Time.deltaTime, _damageTimer - Time.deltaTime);
        Camera aimCamera = Camera.main;
        if (!Input.GetMouseButton(0) || Time.timeScale <= 0f || lightSource == null || aimCamera == null)
        {
            StopFiring();
            return;
        }

        Ray aim = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = TryRaycast(aim, maxDistance, out RaycastHit aimHit)
            ? aimHit.point
            : aim.GetPoint(maxDistance);
        Vector3 direction = aimPoint - lightSource.position;
        if (direction.sqrMagnitude <= 0.000001f)
        {
            StopFiring();
            return;
        }

        int count = TraceBeam(lightSource.position, direction.normalized);
        ApplyBeamDamage();
        HideBeamsFrom(count);
        if (_handLight != null)
            _handLight.enabled = false;
        if (!_isFiring && count > 0)
        {
            _isFiring = true;
            BeamFired?.Invoke();
        }
    }

    int TraceBeam(Vector3 origin, Vector3 direction)
    {
        _damageTargets.Clear();
        float remaining = Mathf.Max(0f, maxDistance);
        int count = 0;
        int reflections = Mathf.Clamp(maxReflections, 0, 1);
        for (int bounce = 0; bounce <= reflections && remaining > 0.001f; bounce++)
        {
            bool hitSomething = TryRaycast(new Ray(origin, direction), remaining, out RaycastHit hit);
            float length = hitSomething ? hit.distance : remaining;
            if (length <= 0.001f)
                break;

            DrawBeam(count++, origin, direction, length);
            RecordSmallTargets(length);
            remaining -= length;
            if (!hitSomething)
                break;

            CentipedeBoss target = hit.collider.GetComponentInParent<CentipedeBoss>();
            if (target != null)
            {
                if (centipede == null || target == centipede)
                    _damageTargets.Add(new BeamTarget { boss = target });
                break;
            }

            direction = Vector3.Reflect(direction, hit.normal).normalized;
            float offset = Mathf.Min(0.01f, remaining);
            origin = hit.point + direction * offset;
            remaining -= offset;
        }
        return count;
    }

    void RecordSmallTargets(float length)
    {
        _segmentHits.Clear();
        for (int i = 0; i < _hitCount; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.collider == null || hit.distance > length
                || (_owner != null && hit.collider.transform.IsChildOf(_owner)))
                continue;

            Centipede small = hit.collider.GetComponentInParent<Centipede>();
            if (small != null && _segmentHits.Add(small))
                _damageTargets.Add(new BeamTarget { small = small });
        }
    }

    void ApplyBeamDamage()
    {
        if (_damageTimer > 0f || _damageTargets.Count == 0)
            return;

        float damage = Mathf.Max(0f, beamDamage);
        foreach (BeamTarget target in _damageTargets)
        {
            if (target.boss != null)
                target.boss.OnHit(damage);
            else if (target.small != null)
                target.small.OnHit(damage);
        }
        _damageTimer += Mathf.Max(0.01f, damageInterval);
    }

    bool TryRaycast(Ray ray, float distance, out RaycastHit nearest)
    {
        nearest = default;
        float closestDistance = float.PositiveInfinity;
        while (true)
        {
            _hitCount = Physics.RaycastNonAlloc(ray, _hits, Mathf.Max(0f, distance),
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (_hitCount < _hits.Length)
                break;
            System.Array.Resize(ref _hits, _hits.Length * 2);
        }
        for (int i = 0; i < _hitCount; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.collider == null || (_owner != null && hit.collider.transform.IsChildOf(_owner)))
                continue;
            if (hit.collider.GetComponentInParent<Centipede>() != null)
                continue;
            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<CentipedeBoss>() == null)
                continue;
            if (hit.distance < closestDistance)
            {
                nearest = hit;
                closestDistance = hit.distance;
            }
        }
        return closestDistance < float.PositiveInfinity;
    }

    void DrawBeam(int index, Vector3 origin, Vector3 direction, float length)
    {
        if (index == _beams.Count)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "LaserBeam";
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                visualCollider.enabled = false;
                Destroy(visualCollider);
            }
            visual.layer = 2;
            visual.GetComponent<MeshRenderer>().sharedMaterial = beamMaterial;
            _beams.Add(visual);
        }

        GameObject beam = _beams[index];
        beam.transform.position = origin + direction * (length * 0.5f);
        beam.transform.up = direction;
        beam.transform.localScale = new Vector3(beamThickness, length * 0.5f, beamThickness);
        beam.SetActive(true);
    }

    void HideBeamsFrom(int index)
    {
        for (int i = index; i < _beams.Count; i++)
            _beams[i].SetActive(false);
    }

    void StopFiring()
    {
        HideBeamsFrom(0);
        _damageTargets.Clear();
        _isFiring = false;
        if (_handLight != null)
            _handLight.enabled = true;
    }

    void OnDisable()
    {
        StopFiring();
    }

    void OnDestroy()
    {
        foreach (GameObject beam in _beams)
        {
            if (beam != null)
                Destroy(beam);
        }
        _beams.Clear();
    }
}
