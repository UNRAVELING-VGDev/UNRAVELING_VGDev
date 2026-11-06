using System.Collections;
using UnityEngine;

public class PullbackAttack : CentipedeAttack
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
    [SerializeField] float _windupDuration = 0.5f;
    [SerializeField] float _holdDuration = 0.28f;
    [SerializeField] float _launchDuration = 0.6f;

    [Header("Lurch")]
    [SerializeField] float _bodyPullback = 2.2f;
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

    [Header("Launch")]
    [SerializeField] float _stopDistance = 4.5f;

    [Header("Hit")]
    [SerializeField] float _hitReach = 1.8f;
    [SerializeField] Vector3 _hitSize = new Vector3(2.8f, 2.6f, 3.2f);

    Quaternion _headBaseRot;
    Quaternion _frontBaseRot;
    Quaternion _secondBaseRot;
    Quaternion _thirdBaseRot;
    Vector3 _headBasePos;
    Vector3 _frontBasePos;
    Vector3 _secondBasePos;
    Vector3 _thirdBasePos;
    Quaternion _fourthBaseRot;
    Quaternion _fifthBaseRot;
    Quaternion _sixthBaseRot;
    Vector3 _fourthBasePos;
    Vector3 _fifthBasePos;
    Vector3 _sixthBasePos;
    Vector3 _positionAtStart;
    GameObject _hitVolume;

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
            Debug.LogError("PullbackAttack needs a Head, a Mouth, and a Player.", this);
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

        _hitVolume = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _hitVolume.name = "PullbackHitbox";
        _hitVolume.transform.SetParent(transform, false);
        _hitVolume.transform.localScale = _hitSize;

        BoxCollider box = _hitVolume.GetComponent<BoxCollider>();
        box.isTrigger = true;

        MeshRenderer bodyRenderer = _head.GetComponent<MeshRenderer>();
        MeshRenderer hitRenderer = _hitVolume.GetComponent<MeshRenderer>();
        if (bodyRenderer != null)
            hitRenderer.sharedMaterial = bodyRenderer.sharedMaterial;

        _hitVolume.SetActive(false);
    }

    protected override IEnumerator Perform()
    {
        _positionAtStart = transform.position;

        yield return Lurch(0f, 1f, _windupDuration);
        ApplyLurch(1f);
        yield return new WaitForSeconds(_holdDuration);

        Vector3 launchStart = transform.position;
        Vector3 destination = LaunchDestination();
        _hitVolume.SetActive(true);

        float elapsed = 0f;
        bool loggedHit = false;
        while (elapsed < _launchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _launchDuration);
            // Fast out of the coil, then a heavier arrival.
            float travel = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(launchStart, destination, travel);

            float coil = Mathf.Clamp01(1f - (t / 0.45f));
            ApplyCoil(coil);
            PlaceHitbox();

            if (!loggedHit && HitsPlayer())
            {
                loggedHit = true;
                Debug.Log("Pullback hit");
            }

            yield return null;
        }

        transform.position = destination;
        ApplyCoil(0f);
        _hitVolume.SetActive(false);
    }

    void OnDisable()
    {
        bool interrupted = IsPlaying;
        StopAllCoroutines();
        MarkStopped();
        ApplyCoil(0f);
        if (interrupted)
            transform.position = _positionAtStart;
        if (_hitVolume != null)
            _hitVolume.SetActive(false);
    }

    IEnumerator Lurch(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            ApplyLurch(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Starts fast so the front yanks backward instead of leaning slowly.
            float amount = Mathf.Lerp(from, to, 1f - Mathf.Pow(1f - t, 3f));
            ApplyLurch(amount);
            yield return null;
        }

        ApplyLurch(to);
    }

    void ApplyLurch(float amount)
    {
        ApplyCoil(amount);
        transform.position = _positionAtStart - transform.forward * (_bodyPullback * amount);
    }

    void ApplyCoil(float amount)
    {
        PoseSegment(_head, _headBasePos, _headBaseRot, _headBack, _headLift, _headPitch, amount);
        PoseSegment(_frontSegment, _frontBasePos, _frontBaseRot, _frontBack, _frontLift, _frontPitch, amount);
        PoseSegment(_secondSegment, _secondBasePos, _secondBaseRot, _secondBack, _secondLift, _secondPitch, amount);
        PoseSegment(_thirdSegment, _thirdBasePos, _thirdBaseRot, _thirdBack, _thirdLift, _thirdPitch, amount);
        PoseSegment(_fourthSegment, _fourthBasePos, _fourthBaseRot, _fourthBack, _fourthLift, _fourthPitch, amount);
        PoseSegment(_fifthSegment, _fifthBasePos, _fifthBaseRot, _fifthBack, _fifthLift, _fifthPitch, amount);
        PoseSegment(_sixthSegment, _sixthBasePos, _sixthBaseRot, _sixthBack, _sixthLift, _sixthPitch, amount);
    }

    static void PoseSegment(Transform segment, Vector3 basePos, Quaternion baseRot, float back, float lift, float pitch, float amount)
    {
        if (segment == null)
            return;

        segment.localPosition = basePos + new Vector3(0f, lift * amount, -back * amount);
        segment.localRotation = Quaternion.Euler(pitch * amount, 0f, 0f) * baseRot;
    }

    Vector3 LaunchDestination()
    {
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance <= _stopDistance)
            return transform.position;

        return transform.position + toPlayer.normalized * (distance - _stopDistance);
    }

    void PlaceHitbox()
    {
        _hitVolume.transform.position = _mouth.position + transform.forward * _hitReach;
        _hitVolume.transform.rotation = transform.rotation;
    }

    bool HitsPlayer()
    {
        Vector3 halfExtents = _hitVolume.transform.lossyScale * 0.5f;
        Collider[] hits = Physics.OverlapBox(
            _hitVolume.transform.position,
            halfExtents,
            _hitVolume.transform.rotation);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
                return true;
        }

        return false;
    }

    static void CacheSegment(Transform segment, ref Quaternion rotation, ref Vector3 position)
    {
        if (segment == null)
            return;

        rotation = segment.localRotation;
        position = segment.localPosition;
    }
}
