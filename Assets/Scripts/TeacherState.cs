using UnityEngine;
using System.Collections;

public class TeacherState : MonoBehaviour
{
    public enum State
    {
        FacingBoard = 0,
        TurningToPlayer = 1,
        FacingPlayer = 2,
        TurningToBoard = 3,
        WalkingToPlayer = 4,
        InFrontOfPlayer = 5,
    }
    
    public Animator animator;
    public State state;
    public bool isFacingPlayer
    {
        get
        {
            switch (state)
            {
                case State.FacingPlayer:
                case State.WalkingToPlayer:
                case State.InFrontOfPlayer: 
                    return true;
                case State.FacingBoard:
                case State.TurningToPlayer:
                case State.TurningToBoard:
                default:
                    return false;
            }
        }
    }


    [Header("Turning")]
    public float turnDuration = 1.5f;

    [Header("Timing")]
    public float minBoardTime = 3f;
    public float maxBoardTime = 10f;
    public float minClassTime = 2f;
    public float maxClassTime = 6f;

    [Header("Staredown walk")]
    public Transform player;
    public float facePlayerAngle = 90f;
    public float walkSpeed = 1.5f;
    public float stopDistance = 2f;
    public float leftOffset = 0.3f;    
    public float stepDownAmount = 0.3f;  
    public float stepDownAfter = 1f;

    private bool levelComplete = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void Start()
    {
        StartCoroutine(TurnAround());
    }

    void OnStageChange(int newStage)
    {
        if (newStage == 4)
        {
            levelComplete = true;
            StopAllCoroutines();
            StartCoroutine(WalkToPlayer());
        }
    }

    IEnumerator WalkToPlayer()
    {
        ChangeState(State.WalkingToPlayer);
        transform.rotation = Quaternion.Euler(0f, facePlayerAngle, 0f);
        
        float startY = transform.position.y;
        float elapsed = 0f;
 
        Vector3 finalTarget = player.position + player.right * -leftOffset;
 
        while (true)
        {
            elapsed += Time.deltaTime;
 
            Vector3 target = new Vector3(finalTarget.x, transform.position.y, finalTarget.z);
            float dist = Vector3.Distance(transform.position, target);
 
            if (dist <= stopDistance) break;
 
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            float animTime = info.normalizedTime * info.length;
 
            float speed = (animTime >= 190f / 24f && animTime <= 350f / 24f)
                ? walkSpeed * 3.5f
                : walkSpeed;
 
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
 
            if (elapsed > stepDownAfter)
            {
                Vector3 p = transform.position;
                float goalY = startY - stepDownAmount;
                p.y = Mathf.Lerp(p.y, goalY, Time.deltaTime * 2f);
                transform.position = p;
            }
 
            yield return null;
        }
        ChangeState(State.InFrontOfPlayer);
    }

    IEnumerator TurnAround()
    {
        while (!levelComplete)
        {
            ChangeState(State.FacingBoard);

            yield return new WaitForSeconds(
                Random.Range(minBoardTime, maxBoardTime)
            );

            if (levelComplete)
                yield break;

            ChangeState(State.TurningToPlayer);

            yield return RotateOver(
                Quaternion.Euler(0f, 270f, 0f)
            );

            if (levelComplete)
                yield break;

            ChangeState(State.FacingPlayer);

            yield return new WaitForSeconds(
                Random.Range(minClassTime, maxClassTime)
            );

            if (levelComplete)
                yield break;

            ChangeState(State.TurningToBoard);

            yield return RotateOver(
                Quaternion.Euler(0f, 90f, 0f)
            );
        }
    }

    void ChangeState(State state)
    {
        animator.SetInteger("State", (int)state);
        this.state = state;
    }

    IEnumerator RotateOver(Quaternion target)
    {
        Quaternion start = transform.rotation;
        float t = 0f;

        while (t < turnDuration)
        {
            t += Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                start,
                target,
                t / turnDuration
            );

            yield return null;
        }

        transform.rotation = target;
    }
}