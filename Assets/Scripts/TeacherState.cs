using UnityEngine;
using System.Collections;

public class TeacherState : MonoBehaviour
{
    public Animator animator;
    public bool isFacingBoard = true;

    [Header("Turning")]
    [Tooltip("Seconds the physical 180 turn takes. Tune this live in play mode.")]
    public float turnDuration = 1.5f;

    [Header("Timing")]
    public float minBoardTime = 3f;
    public float maxBoardTime = 10f;
    public float minClassTime = 2f;
    public float maxClassTime = 6f;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        StartCoroutine(TurnAround());
    }

    IEnumerator TurnAround()
    {
        while (true)
        {
            // facing the board, player is safe
            animator.SetInteger("State", 0);
            isFacingBoard = true;
            yield return new WaitForSeconds(Random.Range(minBoardTime, maxBoardTime));

            // turning toward the class, danger starts here
            isFacingBoard = false;
            animator.SetInteger("State", 1);
            yield return RotateOver(Quaternion.Euler(0f, 270f, 0f));

            // facing the class
            animator.SetInteger("State", 2);
            yield return new WaitForSeconds(Random.Range(minClassTime, maxClassTime));

            // turning back to the board
            animator.SetInteger("State", 3);
            yield return RotateOver(Quaternion.Euler(0f, 90f, 0f));
        }
    }

    IEnumerator RotateOver(Quaternion target)
    {
        Quaternion start = transform.rotation;
        float t = 0f;

        while (t < turnDuration)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, target, t / turnDuration);
            yield return null;
        }

        transform.rotation = target;
    }
}