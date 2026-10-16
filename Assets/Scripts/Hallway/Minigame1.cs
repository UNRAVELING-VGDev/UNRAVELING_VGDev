using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Minigame1 : MonoBehaviour {
    [SerializeField] float duration = 30f; //duration of the mini game
    public float speed = 5f;
    public float dashDistance = 20f;
    public float delay = 1f; //delay between dashes
    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private float timer = 0f;
    private bool started = false;
    private bool hasHit = false;

    void Start() {
        startPos = transform.position;
        startRot = transform.rotation;
        endPos = startPos + Vector3.back * dashDistance;
    }

    //start the minigame by calling this
    public void StartMinigame() {
        StopAllCoroutines();
        started = true;
        timer = 0f;
        transform.position = startPos;
        transform.rotation = startRot;
        StartCoroutine(DashLoop());
    }

    void Update() {
        // for testing
        //if (Keyboard.current.tKey.wasPressedThisFrame){
        //    StartMinigame();
        //}
        
        if (!started) {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= duration) {
            started = false;
            StopAllCoroutines();
            transform.position = startPos;
            transform.rotation = startRot;
            Debug.Log("minigame over");
        }
    }

    IEnumerator DashLoop() {
        while (true) {
            yield return MoveTo(endPos);
            yield return TurnAround();
            yield return MoveTo(startPos);
            yield return TurnAround();
            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator MoveTo(Vector3 target) {
        hasHit = false;
        while (transform.position != target) {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator TurnAround() {
        Quaternion target = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        while (transform.rotation != target) {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 360f* Time.deltaTime);
            yield return null;
        }
    }

    void OnTriggerStay(Collider other)
    {
        //made it only trigger during the minigame
        if (!started || hasHit) {
            return;
        }

        if (other.name== "Player") {
            hasHit = true;
            Debug.Log("hallmonitor: hit (minigame1)"+timer);
        }
    }
}