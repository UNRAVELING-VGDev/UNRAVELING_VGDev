using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Minigame1 : MonoBehaviour {
    [SerializeField] float duration = 30f; //duration of the mini game
    [SerializeField] public float speed = 200f;
    [SerializeField] private float dashDistance = 20f;
    [SerializeField] public float delay = 1f; //delay between dashes
    [SerializeField] public float escalationvar = 0.05f; //this goes up to 2.5x for 30 seconds. This is a multiplier per second.
    //for example 1.05x for the first second, then 1.1x for the second second, then 1.15x and so on.
    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private Transform player;
    private float timer = 0f;
    private bool started = false;
    private bool hasHit = false;

    void Start() {
        startPos = transform.position;
        startRot = transform.rotation;
        player = GameObject.Find("Player").transform;
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
        //for testing
        if (Keyboard.current.tKey.wasPressedThisFrame){
            StartMinigame();
        }

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
            float theta = Random.Range(0f, 2f * Mathf.PI);
            Vector3 offset = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * dashDistance;

            Vector3 spawnPos = player.position + offset;
            endPos = player.position - offset;

            spawnPos.y = startPos.y;
            endPos.y = startPos.y;

            transform.position = spawnPos;
            transform.rotation = Quaternion.LookRotation(endPos - spawnPos);
            Debug.Log("hallmonitor: dash start " + spawnPos);

            yield return MoveTo(endPos);
            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator MoveTo(Vector3 target) {
        hasHit = false;
        while (transform.position != target) {
            //speed goes up based on escalationvar. it's linear
            transform.position = Vector3.MoveTowards(transform.position, target, speed*(1f + escalationvar*timer)*Time.deltaTime);
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