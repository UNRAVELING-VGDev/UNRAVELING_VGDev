using System.Collections;
using UnityEngine;

public abstract class CentipedeAttack : MonoBehaviour
{
    [Min(0f)] [SerializeField] float _paranoiaPerHit = 10f;

    public bool IsPlaying { get; private set; }

    protected void MoveBoss(Vector3 position)
    {
        CentipedeBoss boss = GetComponent<CentipedeBoss>();
        transform.position = boss != null ? boss.ConstrainPosition(position) : position;
    }

    protected void ReportPlayerHit(Collider player)
    {
        ReportPlayerHit(player, _paranoiaPerHit);
    }

    protected void ReportPlayerHit(Collider player, float paranoiaAmount)
    {
        PlayerControl movement = player.GetComponentInParent<PlayerControl>();
        if (movement != null && movement.IsInvulnerable)
            return;

        ParanoiaMeter paranoia = player.GetComponentInParent<ParanoiaMeter>();
        if (paranoia != null)
            paranoia.Add(paranoiaAmount, true);
    }

    public void Play()
    {
        if (IsPlaying)
            return;

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        IsPlaying = true;
        yield return Perform();
        IsPlaying = false;
    }

    protected abstract IEnumerator Perform();

    protected void MarkStopped()
    {
        IsPlaying = false;
    }
}
