using System.Collections;
using UnityEngine;

public class CentipedeBoss : MonoBehaviour
{
    [Header("Probability")]
    [SerializeField] float _pullbackProbability = 1f;
    [SerializeField] float _whipProbability = 1f;
    [SerializeField] float _acidProbability = 1f;

    [Header("Timing")]
    [SerializeField] float _timeBetweenAttacks = 1.75f;
    [SerializeField] float _timeVariance = 1f;

    [Header("Crawl")]
    [SerializeField] Transform _player;
    [SerializeField] float _crawlSpeed = 3f;
    [SerializeField] float _preferredDistance = 10f;
    [SerializeField] float _distanceSlack = 3f;
    [SerializeField] float _turnSpeed = 100f;
    [SerializeField] float _settleDuration = 0.5f;
    [SerializeField] float _weaveAngle = 7f;

    [SerializeField] PullbackAttack _pullback;
    [SerializeField] CentipedeAttack _whip;
    [SerializeField] CentipedeAttack _acid;

    void Awake()
    {
        if (_pullback == null)
            _pullback = GetComponent<PullbackAttack>();
        if (_whip == null)
            _whip = GetComponent<WhipAttack>();
        if (_acid == null)
            _acid = GetComponent<AcidAttack>();
        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _player = player.transform;
        }
    }

    void Start()
    {
        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return Crawl(GapDuration());

            CentipedeAttack attack = ChooseAttack();
            if (attack == null)
                continue;

            attack.Play();
            while (attack.IsPlaying)
                yield return null;
        }
    }

    float GapDuration()
    {
        float variance = Mathf.Max(0f, _timeVariance);
        float min = Mathf.Max(0.05f, _timeBetweenAttacks - variance);
        float max = Mathf.Max(min, _timeBetweenAttacks + variance);
        return Random.Range(min, max);
    }

    IEnumerator Crawl(float duration)
    {
        if (_player == null || duration <= 0f)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        float orbit = Random.value < 0.5f ? -1f : 1f;
        float settle = Mathf.Min(_settleDuration, duration * 0.45f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            float speedScale = 1f;
            if (settle > 0f && remaining < settle)
                speedScale = Mathf.SmoothStep(0f, 1f, remaining / settle);

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 toward = distance > 0.001f ? toPlayer / distance : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, toward) * orbit;
            float radial = 0f;
            if (_distanceSlack > 0.001f)
                radial = Mathf.Clamp((distance - _preferredDistance) / _distanceSlack, -1f, 1f);

            Vector3 wish = side + toward * radial;
            if (wish.sqrMagnitude > 0.001f)
                wish.Normalize();

            Vector3 next = transform.position + wish * (_crawlSpeed * speedScale * Time.deltaTime);
            next.y = transform.position.y;
            transform.position = next;

            float weave = Mathf.Sin(elapsed * 2.4f) * _weaveAngle * speedScale;
            Quaternion face = Quaternion.LookRotation(toward, Vector3.up) * Quaternion.Euler(0f, weave, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, face, _turnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    CentipedeAttack ChooseAttack()
    {
        float pullback = Mathf.Max(0f, _pullbackProbability);
        float whip = Mathf.Max(0f, _whipProbability);
        float acid = Mathf.Max(0f, _acidProbability);
        float total = pullback + whip + acid;
        if (total <= 0f)
            return null;

        // Scale each entry by the total so the three values do not have to sum to 1.
        float roll = Random.value * total;
        if (roll < pullback)
            return _pullback;
        if (roll < pullback + whip)
            return _whip;
        return _acid;
    }
}
