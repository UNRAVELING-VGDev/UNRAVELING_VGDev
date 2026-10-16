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

    void OnEnable()
    {
        // losestate 2: failed the staredown skill check (3 misses)
        SkillCheckHandler.OnSkillCheckFailed += OnStaredownFailed;
    }

    void OnDisable()
    {
        SkillCheckHandler.OnSkillCheckFailed -= OnStaredownFailed;
    }

    void OnStaredownFailed()
    {
        Lose("failed staredown");
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

    // called for any loss (paranoia maxed or failed staredown)
    public void Lose(string why)
    {
        if (IsOver) {
            return;
        }

        IsOver = true;
        Debug.Log("GAME OVER: "+why);

        //sends out the event. the game over menu listens to this.
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


        foreach (BadParticle p in FindObjectsByType<BadParticle>(FindObjectsSortMode.None))
            p.enabled = false;


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ResetCamera());
    }

    IEnumerator ResetCamera()
    {
        if (look == null || cam == null) yield break;  

        Quaternion bodyFrom = look.transform.localRotation;
        Quaternion camFrom = cam.transform.localRotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * resetSpeed;

            look.transform.localRotation = Quaternion.Slerp(bodyFrom, Quaternion.identity, t);
            cam.transform.localRotation = Quaternion.Slerp(camFrom, Quaternion.identity, t);

            yield return null;
        }
    }
}