using UnityEngine;
using System;
using System.Collections;

public class LoseState : MonoBehaviour
{
    public static event Action onGameOver;
    public bool IsOver;
    public float resetSpeed = 2f;
    public ParanoiaMeter paranoia;
    public Camera cam;
    public PlayerLook look;
    public LeanController lean;
    public PlayerRay gaze;
    public LevelClock clock;
    public TeacherState teacher;
    public Badparticlemanager particles;

    void Awake()
    {
        if (paranoia == null) {
            paranoia  = FindFirstObjectByType<ParanoiaMeter>();
        }
        if (look == null) {
            look = FindFirstObjectByType<PlayerLook>();
        }
        if (lean == null) {
            lean = FindFirstObjectByType<LeanController>();
        }
        if (gaze == null) {
            gaze = FindFirstObjectByType<PlayerRay>();
        }
        if (clock == null) {
            clock = FindFirstObjectByType<LevelClock>();
        }
        if (teacher == null) {
            teacher = FindFirstObjectByType<TeacherState>();
        }
        if (particles == null) {
           particles = FindFirstObjectByType<Badparticlemanager>(); 
        }
        if (cam == null) {
            cam = Camera.main;
        }
        if (paranoia == null) {
            Debug.LogError("error");
        }
    }

    //losestate 1: paranoia is maxed
    void Update()
    {
        if (IsOver || paranoia == null) {
            return;
        }
        if (paranoia.paranoiaLevel >= paranoia.maxParanoia) {
            Lose("paranoia maxed");
        }
    }

    //idk what staredown currently does so just call this for a failedstaredown
    public void Lose(string why)
    {
        if (IsOver) {
            return;
        }

        IsOver = true;
        Debug.Log("GAME OVER: "+why);

        //sends out the event. reset can listen to this.
        if (onGameOver != null) {
            onGameOver();
        }

        //locks movement completely
        if (look != null) {
            look.enabled = false;
        }
        if (lean != null) {
            lean.enabled = false;
        }
        if (gaze != null) {
            gaze.enabled = false;
        }

        //stop the clock
        if (clock != null) {
            clock.enabled = false;
        }

        // stop the teacher turning and the particle attacks
        if (teacher != null) {
            teacher.StopAllCoroutines();
        }
        if (particles != null) {
            particles.StopAllCoroutines();
        }

        //I froze the particles instead of removing it.
        //probably not a good idea but idk.
        foreach (BadParticle p in FindObjectsByType<BadParticle>(FindObjectsSortMode.None))
            p.enabled = false;

        //give the mouse back
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ResetCamera());
    }

    //this just tweens the camera back to face the front.
    //also no resume yet, but end the courtine there when that is added ig.
    IEnumerator ResetCamera()
    {
        
        Quaternion bodyFrom = look.transform.localRotation;
        Quaternion camFrom = cam.transform.localRotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * resetSpeed;

            //honestly 
            look.transform.localRotation = Quaternion.Slerp(bodyFrom, Quaternion.identity, t);
            cam.transform.localRotation = Quaternion.Slerp(camFrom, Quaternion.identity, t);

            yield return null;
        }
    }
}

