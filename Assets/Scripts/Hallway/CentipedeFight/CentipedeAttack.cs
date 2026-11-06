using System.Collections;
using UnityEngine;

public abstract class CentipedeAttack : MonoBehaviour
{
    public bool IsPlaying { get; private set; }

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
