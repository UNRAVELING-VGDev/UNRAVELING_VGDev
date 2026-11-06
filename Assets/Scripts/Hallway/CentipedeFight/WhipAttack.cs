using System.Collections;
using UnityEngine;

public class WhipAttack : CentipedeAttack
{
    [SerializeField] Transform _head;
    [SerializeField] Transform _segment1;
    [SerializeField] Transform _segment2;
    [SerializeField] Transform _segment3;
    [SerializeField] Transform _segment4;
    [SerializeField] Transform _segment5;
    [SerializeField] Transform _segment6;
    [SerializeField] Transform _tail;

    [Header("Timing")]
    [SerializeField] float _coilDuration = 0.7f;
    [SerializeField] float _holdDuration = 0.18f;
    [SerializeField] float _whipDuration = 2.8f;
    [SerializeField] float _recoverDuration = 0.55f;

    [Header("Plant")]
    [SerializeField] float _headDrop = 0.55f;
    [SerializeField] float _headPitch = 28f;
    [SerializeField] float _frontDrop = 0.32f;
    [SerializeField] float _plantPitch = 22f;

    [Header("Whip")]
    [SerializeField] float _coilAngle = -50f;
    [SerializeField] float _coilCurl = 18f;
    [SerializeField] float _coilLift = 0.45f;
    [SerializeField] float _whipLift = 1.5f;
    [SerializeField] float _tailHeight = 1.3f;

    [Header("Hit")]
    [SerializeField] Vector3 _segmentHalfExtents = new Vector3(0.8f, 1.2f, 0.8f);

    struct Pose
    {
        public Transform Segment;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    Pose _headPose;
    Pose _plantPose;
    Pose[] _bodyPoses;
    Vector3 _restPivot;
    float _innerRadius;
    float _clearRadius;

    void Awake()
    {
        if (_head == null)
            _head = transform.Find("Head");
        if (_segment1 == null)
            _segment1 = transform.Find("Segment 1");
        if (_segment2 == null)
            _segment2 = transform.Find("Segment 2");
        if (_segment3 == null)
            _segment3 = transform.Find("Segment 3");
        if (_segment4 == null)
            _segment4 = transform.Find("Segment 4");
        if (_segment5 == null)
            _segment5 = transform.Find("Segment 5");
        if (_segment6 == null)
            _segment6 = transform.Find("Segment 6");
        if (_tail == null)
            _tail = transform.Find("TailPivot");

        if (_head == null || _segment1 == null || _segment6 == null)
        {
            Debug.LogError("WhipAttack needs the head, the front segment, and the rear body.", this);
            enabled = false;
            return;
        }

        _headPose = Capture(_head);
        _plantPose = Capture(_segment1);
        _bodyPoses = new[]
        {
            Capture(_segment2),
            Capture(_segment3),
            Capture(_segment4),
            Capture(_segment5),
            Capture(_segment6),
            Capture(_tail)
        };
        _restPivot = _plantPose.Position;

        Vector3 inner = _bodyPoses[0].Position - _restPivot;
        inner.y = 0f;
        _innerRadius = inner.magnitude;

        Vector3 headOffset = _headPose.Position - _restPivot;
        headOffset.y = 0f;
        float headRadius = 0.5f * Mathf.Max(_head.lossyScale.x, _head.lossyScale.y, _head.lossyScale.z);
        float halfLength = _segment2 != null ? _segment2.lossyScale.y : 1.15f;
        _clearRadius = headOffset.magnitude + headRadius + halfLength + 0.2f;
    }

    protected override IEnumerator Perform()
    {
        yield return Blend(0f, 1f, 0f, _coilAngle, 0f, _coilCurl, 0f, _coilLift, _coilDuration);
        Apply(1f, _coilAngle, _coilCurl, _coilLift);

        // Keep turning past the coil until the body is back on its original line.
        float sweepEnd = _coilAngle <= 0f ? 360f : Mathf.Ceil(_coilAngle / 360f) * 360f;
        float span = Mathf.Max(1f, sweepEnd - _coilAngle);
        float releaseDegrees = ReleaseDegrees(span);
        yield return OpenCoil(releaseDegrees, span);

        float angleStart = _coilAngle + releaseDegrees;
        float whipTime = Mathf.Max(0.01f, _whipDuration);
        float startSpeed = _holdDuration > 0.0001f ? 2f * releaseDegrees / _holdDuration : 0f;
        float slope = startSpeed * whipTime / Mathf.Max(0.001f, sweepEnd - angleStart);
        bool loggedHit = false;
        float elapsed = 0f;
        while (elapsed < whipTime)
        {
            elapsed += Time.deltaTime;
            float v = Mathf.Clamp01(elapsed / whipTime);
            float travel = EaseFromSlope(v, slope);
            float angle = Mathf.Lerp(angleStart, sweepEnd, travel);
            float swing = Mathf.Clamp01((angle - _coilAngle) / span);
            float settle = Mathf.SmoothStep(0.9f, 1f, swing);
            float lift = Mathf.Lerp(_whipLift, 0f, settle);
            Apply(1f, angle, 0f, lift, swing);

            if (!loggedHit && v < 0.85f && BodyHitsPlayer())
            {
                loggedHit = true;
                Debug.Log("Whip hit");
            }

            yield return null;
        }

        yield return Blend(1f, 0f, sweepEnd, sweepEnd, 0f, 0f, 0f, 0f, _recoverDuration);
        Apply(0f, 0f, 0f, 0f);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        MarkStopped();
        Apply(0f, 0f, 0f, 0f);
    }

    IEnumerator Blend(
        float groundFrom,
        float groundTo,
        float angleFrom,
        float angleTo,
        float curlFrom,
        float curlTo,
        float liftFrom,
        float liftTo,
        float duration)
    {
        if (duration <= 0f)
        {
            Apply(groundTo, angleTo, curlTo, liftTo);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float t = u * u * u * (u * (u * 6f - 15f) + 10f);
            Apply(
                Mathf.Lerp(groundFrom, groundTo, t),
                Mathf.Lerp(angleFrom, angleTo, t),
                Mathf.Lerp(curlFrom, curlTo, t),
                Mathf.Lerp(liftFrom, liftTo, t));
            yield return null;
        }

        Apply(groundTo, angleTo, curlTo, liftTo);
    }

    float ReleaseDegrees(float span)
    {
        const float exitSlope = 1.35f;
        float hold = Mathf.Max(0f, _holdDuration);
        float whip = Mathf.Max(0.01f, _whipDuration);
        float release = exitSlope * hold * span / (2f * whip + exitSlope * hold);
        return Mathf.Clamp(release, 0f, span * 0.22f);
    }

    IEnumerator OpenCoil(float releaseDegrees, float span)
    {
        if (_holdDuration <= 0f)
        {
            Apply(1f, _coilAngle, _coilCurl, _coilLift);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _holdDuration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / _holdDuration);
            float open = u * u;
            float shape = u * u * (3f - 2f * u);
            float angle = _coilAngle + releaseDegrees * open;
            float curl = Mathf.Lerp(_coilCurl, 0f, shape);
            float lift = Mathf.Lerp(_coilLift, _whipLift, shape);
            float swing = Mathf.Clamp01((angle - _coilAngle) / span);
            Apply(1f, angle, curl, lift, swing);
            yield return null;
        }

        float opened = Mathf.Clamp01(releaseDegrees / span);
        Apply(1f, _coilAngle + releaseDegrees, 0f, _whipLift, opened);
    }

    static float EaseFromSlope(float v, float slope)
    {
        float m = Mathf.Clamp(slope, 0f, 2.5f);
        float v2 = v * v;
        return (m * v) + ((3f - 2f * m) * v2) + ((m - 2f) * v * v2);
    }

    void Apply(float grounded, float angle, float curl, float lift, float swing = 0f)
    {
        if (_headPose.Segment != null)
        {
            _headPose.Segment.localPosition = _headPose.Position + Vector3.down * (_headDrop * grounded);
            _headPose.Segment.localRotation = Quaternion.Euler(_headPitch * grounded, 0f, 0f)
                * Quaternion.Euler(0f, angle, 0f)
                * _headPose.Rotation;
        }

        Vector3 pivot = _restPivot + Vector3.down * (_frontDrop * grounded);
        if (_plantPose.Segment != null)
        {
            _plantPose.Segment.localPosition = pivot;
            _plantPose.Segment.localRotation = Quaternion.Euler(_plantPitch * grounded, 0f, 0f)
                * Quaternion.Euler(0f, angle, 0f)
                * _plantPose.Rotation;
        }

        if (_bodyPoses == null)
            return;

        int count = _bodyPoses.Length;
        for (int i = 0; i < count; i++)
        {
            Pose pose = _bodyPoses[i];
            if (pose.Segment == null)
                continue;

            float along = count <= 1 ? 1f : i / (float)(count - 1);
            float pass = Mathf.Sin(Mathf.Clamp01(swing) * Mathf.PI);
            float yaw = angle - (pass * 26f * along) + curl * along;
            Quaternion bend = Quaternion.Euler(0f, yaw, 0f);
            Vector3 offset = pose.Position - _restPivot;
            Vector3 spun = bend * offset;
            Vector3 flat = new Vector3(spun.x, 0f, spun.z);
            float naturalRadius = flat.magnitude;
            float extra = Mathf.Max(0f, _clearRadius - _innerRadius) * pass;
            if (naturalRadius > 0.001f)
                flat = flat.normalized * (naturalRadius + extra);
            spun.x = flat.x;
            spun.z = flat.z;
            float raised = Mathf.Clamp01(lift / Mathf.Max(_whipLift, 0.01f)) * along;
            spun.y = Mathf.Lerp(spun.y, _tailHeight - pivot.y, raised);
            pose.Segment.localPosition = pivot + spun;
            pose.Segment.localRotation = bend * pose.Rotation;
        }
    }

    bool BodyHitsPlayer()
    {
        if (_bodyPoses == null)
            return false;

        foreach (Pose pose in _bodyPoses)
        {
            if (pose.Segment == null || pose.Segment == _tail)
                continue;

            Collider[] hits = Physics.OverlapBox(
                pose.Segment.position,
                _segmentHalfExtents,
                pose.Segment.rotation);

            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                    return true;
            }
        }

        return false;
    }

    static Pose Capture(Transform segment)
    {
        Pose pose = new Pose();
        if (segment == null)
            return pose;

        pose.Segment = segment;
        pose.Position = segment.localPosition;
        pose.Rotation = segment.localRotation;
        return pose;
    }
}
