using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidAttack : CentipedeAttack
{
    [SerializeField] Transform _head;
    [SerializeField] Transform _frontSegment;
    [SerializeField] Transform _secondSegment;
    [SerializeField] Transform _thirdSegment;
    [SerializeField] Transform _fourthSegment;
    [SerializeField] Transform _fifthSegment;
    [SerializeField] Transform _sixthSegment;
    [SerializeField] Transform _mouth;
    [SerializeField] Transform _player;

    [Header("Timing")]
    [SerializeField] float _windupDuration = 0.55f;
    [SerializeField] float _holdDuration = 0.22f;
    [SerializeField] float _spitDuration = 0.36f;
    [SerializeField] float _recoverDuration = 0.5f;

    [Header("Coil")]
    [SerializeField] float _bodyPullback = 2f;
    [SerializeField] float _headPitch = -100f;
    [SerializeField] float _headBack = 3.6f;
    [SerializeField] float _headLift = 2.6f;
    [SerializeField] float _frontPitch = -75f;
    [SerializeField] float _frontBack = 2.4f;
    [SerializeField] float _frontLift = 1.4f;
    [SerializeField] float _secondPitch = -20f;
    [SerializeField] float _secondBack = 0.2f;
    [SerializeField] float _secondLift = -0.45f;
    [SerializeField] float _thirdPitch = 15f;
    [SerializeField] float _thirdBack = -0.4f;
    [SerializeField] float _thirdLift = -0.2f;
    [SerializeField] float _fourthPitch = 50f;
    [SerializeField] float _fourthBack = 0.6f;
    [SerializeField] float _fourthLift = 0.8f;
    [SerializeField] float _fifthPitch = 75f;
    [SerializeField] float _fifthBack = 1.1f;
    [SerializeField] float _fifthLift = 1.5f;
    [SerializeField] float _sixthPitch = 95f;
    [SerializeField] float _sixthBack = 1.3f;
    [SerializeField] float _sixthLift = 2.1f;

    [Header("Spit")]
    [SerializeField] float _spitForward = 0.7f;
    [SerializeField] float _spitLift = -0.12f;
    [SerializeField] float _spitPitch = 42f;

    [Header("Shot")]
    [SerializeField] float _shotVelocity = 28f;
    [SerializeField] float _ballSize = 0.55f;

    [Header("Puddle")]
    [SerializeField] float _puddleDuration = 6f;
    [SerializeField] float _puddleRadius = 1.7f;

    Quaternion _headBaseRot;
    Quaternion _frontBaseRot;
    Quaternion _secondBaseRot;
    Quaternion _thirdBaseRot;
    Quaternion _fourthBaseRot;
    Quaternion _fifthBaseRot;
    Quaternion _sixthBaseRot;
    Vector3 _headBasePos;
    Vector3 _frontBasePos;
    Vector3 _secondBasePos;
    Vector3 _thirdBasePos;
    Vector3 _fourthBasePos;
    Vector3 _fifthBasePos;
    Vector3 _sixthBasePos;
    Vector3 _positionAtStart;
    Material _acidMaterial;
    bool _ready;
    readonly List<GameObject> _spawned = new List<GameObject>();

    void Awake()
    {
        if (_head == null)
            _head = transform.Find("Head");
        if (_mouth == null && _head != null)
            _mouth = _head.Find("Mouth");
        if (_frontSegment == null)
            _frontSegment = transform.Find("Segment 1");
        if (_secondSegment == null)
            _secondSegment = transform.Find("Segment 2");
        if (_thirdSegment == null)
            _thirdSegment = transform.Find("Segment 3");
        if (_fourthSegment == null)
            _fourthSegment = transform.Find("Segment 4");
        if (_fifthSegment == null)
            _fifthSegment = transform.Find("Segment 5");
        if (_sixthSegment == null)
            _sixthSegment = transform.Find("Segment 6");
        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _player = player.transform;
        }

        if (_head == null || _mouth == null || _player == null)
        {
            Debug.LogError("AcidAttack needs a Head, a Mouth, and a Player.", this);
            enabled = false;
            return;
        }

        _headBaseRot = _head.localRotation;
        _headBasePos = _head.localPosition;
        CacheSegment(_frontSegment, ref _frontBaseRot, ref _frontBasePos);
        CacheSegment(_secondSegment, ref _secondBaseRot, ref _secondBasePos);
        CacheSegment(_thirdSegment, ref _thirdBaseRot, ref _thirdBasePos);
        CacheSegment(_fourthSegment, ref _fourthBaseRot, ref _fourthBasePos);
        CacheSegment(_fifthSegment, ref _fifthBaseRot, ref _fifthBasePos);
        CacheSegment(_sixthSegment, ref _sixthBaseRot, ref _sixthBasePos);
        _ready = true;
    }

    protected override IEnumerator Perform()
    {
        _positionAtStart = transform.position;

        yield return BlendCoil(0f, 1f, _windupDuration);
        ApplyPose(1f, 0f);
        yield return new WaitForSeconds(_holdDuration);
        yield return Spit();
        yield return BlendCoil(1f, 0f, _recoverDuration);
    }

    void OnDisable()
    {
        if (!_ready)
            return;

        bool interrupted = IsPlaying;
        StopAllCoroutines();
        MarkStopped();
        ApplyPose(0f, 0f);
        if (interrupted)
            transform.position = _positionAtStart;

        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i]);
        }

        _spawned.Clear();
        if (_acidMaterial != null)
            Destroy(_acidMaterial);
    }

    IEnumerator BlendCoil(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            ApplyPose(to, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float t = u * u * u * (u * (u * 6f - 15f) + 10f);
            ApplyPose(Mathf.Lerp(from, to, t), 0f);
            yield return null;
        }

        ApplyPose(to, 0f);
    }

    IEnumerator Spit()
    {
        bool fired = false;
        float elapsed = 0f;
        while (elapsed < _spitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _spitDuration);
            float spit = Mathf.Sin(t * Mathf.PI);
            ApplyPose(1f, spit);
            if (!fired && t >= 0.5f)
            {
                fired = true;
                Launch();
            }

            yield return null;
        }

        ApplyPose(1f, 0f);
    }

    void ApplyPose(float coil, float spit)
    {
        PoseSegment(_head, _headBasePos, _headBaseRot, _headBack, _headLift, _headPitch, coil);
        PoseSegment(_frontSegment, _frontBasePos, _frontBaseRot, _frontBack, _frontLift, _frontPitch, coil);
        PoseSegment(_secondSegment, _secondBasePos, _secondBaseRot, _secondBack, _secondLift, _secondPitch, coil);
        PoseSegment(_thirdSegment, _thirdBasePos, _thirdBaseRot, _thirdBack, _thirdLift, _thirdPitch, coil);
        PoseSegment(_fourthSegment, _fourthBasePos, _fourthBaseRot, _fourthBack, _fourthLift, _fourthPitch, coil);
        PoseSegment(_fifthSegment, _fifthBasePos, _fifthBaseRot, _fifthBack, _fifthLift, _fifthPitch, coil);
        PoseSegment(_sixthSegment, _sixthBasePos, _sixthBaseRot, _sixthBack, _sixthLift, _sixthPitch, coil);

        if (_head != null && spit > 0f)
        {
            _head.localPosition += new Vector3(0f, _spitLift, _spitForward) * spit;
            _head.localRotation = Quaternion.Euler(_spitPitch * spit, 0f, 0f) * _head.localRotation;
        }

        if (_frontSegment != null && spit > 0f)
        {
            _frontSegment.localPosition += new Vector3(0f, _spitLift * 0.35f, _spitForward * 0.4f) * spit;
            _frontSegment.localRotation = Quaternion.Euler(_spitPitch * 0.45f * spit, 0f, 0f) * _frontSegment.localRotation;
        }

        if (IsPlaying)
            transform.position = _positionAtStart - transform.forward * (_bodyPullback * coil);
    }

    void Launch()
    {
        Vector3 start = _mouth.position;
        Vector3 end = _player.position;
        end.y = 0.03f;
        Vector3 toEnd = end - start;
        float distance = toEnd.magnitude;
        Vector3 direction = distance > 0.001f ? toEnd / distance : transform.forward;

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "AcidBall";
        ball.transform.position = start;
        ball.transform.localScale = Vector3.one * _ballSize;
        Collider ballCollider = ball.GetComponent<Collider>();
        if (ballCollider != null)
            ballCollider.isTrigger = true;
        Paint(ball);
        _spawned.Add(ball);
        StartCoroutine(Fly(ball, start, direction, distance, end));
    }

    IEnumerator Fly(GameObject ball, Vector3 start, Vector3 direction, float distance, Vector3 end)
    {
        float traveled = 0f;
        bool logged = false;
        float radius = _ballSize * 0.5f;
        float speed = Mathf.Max(0.01f, _shotVelocity);
        while (traveled < distance && ball != null)
        {
            traveled = Mathf.Min(distance, traveled + speed * Time.deltaTime);
            Vector3 pos = start + direction * traveled;
            ball.transform.position = pos;

            if (!logged && OverlapsPlayer(pos, radius))
            {
                logged = true;
                Debug.Log("Acid hit");
            }

            yield return null;
        }

        if (ball != null)
            Retire(ball);

        SpawnPuddle(end);
    }

    void SpawnPuddle(Vector3 point)
    {
        GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        puddle.name = "AcidPuddle";
        puddle.transform.position = point;
        puddle.transform.localScale = new Vector3(_puddleRadius * 2f, 0.03f, _puddleRadius * 2f);
        Collider puddleCollider = puddle.GetComponent<Collider>();
        if (puddleCollider != null)
            puddleCollider.isTrigger = true;
        Paint(puddle);
        _spawned.Add(puddle);
        StartCoroutine(PuddleLife(puddle));
    }

    IEnumerator PuddleLife(GameObject puddle)
    {
        float elapsed = 0f;
        bool logged = false;
        Vector3 halfExtents = new Vector3(_puddleRadius, 0.6f, _puddleRadius);
        while (elapsed < _puddleDuration && puddle != null)
        {
            elapsed += Time.deltaTime;
            if (!logged && OverlapsPlayer(puddle.transform.position, halfExtents))
            {
                logged = true;
                Debug.Log("Acid puddle");
            }

            yield return null;
        }

        if (puddle != null)
            Retire(puddle);
    }

    void Retire(GameObject spawned)
    {
        _spawned.Remove(spawned);
        Destroy(spawned);
    }

    void Paint(GameObject shape)
    {
        MeshRenderer renderer = shape.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        Material material = AcidMaterial();
        if (material != null)
            renderer.sharedMaterial = material;
    }

    Material AcidMaterial()
    {
        if (_acidMaterial != null)
            return _acidMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        _acidMaterial = new Material(shader);
        Color acid = new Color(0.42f, 0.85f, 0.12f);
        if (_acidMaterial.HasProperty("_BaseColor"))
            _acidMaterial.SetColor("_BaseColor", acid);
        if (_acidMaterial.HasProperty("_Color"))
            _acidMaterial.SetColor("_Color", acid);
        return _acidMaterial;
    }

    static bool OverlapsPlayer(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
                return true;
        }

        return false;
    }

    static bool OverlapsPlayer(Vector3 center, Vector3 halfExtents)
    {
        Collider[] hits = Physics.OverlapBox(center, halfExtents);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
                return true;
        }

        return false;
    }

    static void PoseSegment(Transform segment, Vector3 basePos, Quaternion baseRot, float back, float lift, float pitch, float amount)
    {
        if (segment == null)
            return;

        segment.localPosition = basePos + new Vector3(0f, lift * amount, -back * amount);
        segment.localRotation = Quaternion.Euler(pitch * amount, 0f, 0f) * baseRot;
    }

    static void CacheSegment(Transform segment, ref Quaternion rotation, ref Vector3 position)
    {
        if (segment == null)
            return;

        rotation = segment.localRotation;
        position = segment.localPosition;
    }
}
