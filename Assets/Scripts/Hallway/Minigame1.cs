using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class Minigame1 : MonoBehaviour {
    [SerializeField] float duration = 30f;
    public float speed = 5f;
    public float dashDistance = 20f;
    public float delay = 1f; 
    public float warningDelay = 1f;
    public float overshoot = 6f;
    [SerializeField] Transform center;
    [SerializeField] float spawnRadius = 8f;
    [SerializeField] Transform player;

    [Header("Finish")]
    [SerializeField] int dashesToFinish = 8;              
    [SerializeField] string returnScene = "HallwayP1";  
    [SerializeField] float returnDelay = 1f;             

    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private float timer = 0f;
    private bool started = false;
    private bool finished = false;
    private bool visible = true;
    private int dashCount = 0;
    private Renderer[] renderers;
    protected bool hasHit = false;

    public int DashCount => dashCount;


    void Start() {
        startPos = transform.position;
        startRot = transform.rotation;
        endPos = startPos + Vector3.back * dashDistance;
        if (player == null) {
            Debug.LogWarning("Minigame1: no player assigned!", this);
        }
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    //start the minigame by calling this
    public void StartMinigame() {
        StopAllCoroutines();
        started = true;
        finished = false;
        timer = 0f;
        dashCount = 0;
        transform.position = startPos;
        transform.rotation = startRot;
        StartCoroutine(DashLoop());
    }

    void Update() {
        if (finished) {
            return;   //don't restart once the minigame is over
        }

        if (!started) {
            StartMinigame();
            return;
        }

        timer += Time.deltaTime;
        if (duration > 0f && timer >= duration) {
            FinishMinigame();
        }
    }

    IEnumerator DashLoop() {
        while (true) {
            Teleport();
            yield return new WaitForSeconds(warningDelay);
            yield return MoveTo(GetDashTarget());
            SetVisible(false);

            dashCount++;
            if (dashesToFinish > 0 && dashCount >= dashesToFinish) {
                FinishMinigame();
                yield break;
            }

            yield return new WaitForSeconds(delay);
        }
    }

    //ends the minigame and returns to the hallway
    void FinishMinigame() {
        if (finished) {
            return;
        }
        finished = true;
        started = false;
        StopAllCoroutines();
        transform.position = startPos;
        transform.rotation = startRot;
        SetVisible(false);
        Debug.Log($"minigame over after {dashCount} dashes");
        StartCoroutine(ReturnToHallway());
    }

    IEnumerator ReturnToHallway() {
        yield return new WaitForSeconds(returnDelay);
        SceneManager.LoadScene(returnScene);
    }

    protected virtual IEnumerator MoveTo(Vector3 target) {
        hasHit = false;
        while (transform.position != target) {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    protected virtual void Teleport()
    {
        Vector3 c = center != null ? center.position : startPos;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        //teleport somewhere on circumference of a circle around the center
        transform.position = new Vector3(c.x + Mathf.Cos(angle) * spawnRadius, startPos.y, c.z + Mathf.Sin(angle) * spawnRadius);
        if (player != null)
        {
            FaceTarget(player.position);
        }
        SetVisible(true);
    }

    void SetVisible(bool value)
    {
        visible = value;
        if (renderers == null)
        {
            return;
        }
        foreach (Renderer r in renderers)
        {
            r.enabled = value;
        }
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    Vector3 GetDashTarget()
    {
        Vector3 p = player.position;
        Vector3 flat = new Vector3(p.x, transform.position.y, p.z);
        Vector3 dir = (flat - transform.position).normalized;
        return flat + dir * overshoot;
    }

    //not in use currently
    protected virtual IEnumerator TurnAround() {
        Quaternion target = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        while (transform.rotation != target) {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 360f* Time.deltaTime);
            yield return null;
        }
    }

    void OnTriggerStay(Collider other)
    {
        //made it only trigger during the minigame
        if (!started || hasHit || !visible) {
            return;
        }

        if (player != null && other.transform.IsChildOf(player)) {
            hasHit = true;
            Debug.Log("hallmonitor: hit (minigame1)"+timer);
        }
    }
}