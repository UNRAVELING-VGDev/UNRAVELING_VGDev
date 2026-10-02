using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }
    
    [Header("General")]

    [SerializeField] private EventReference clayDodgeSFX;
    [SerializeField] private EventReference clayHurtSFX;

    [SerializeField] private EventReference pauseSnapshot;

    [Header("Level 01")]
    [SerializeField] private EventReference level01Music;

    [SerializeField] private EventReference teacherStaredownSFX;
    [SerializeField] private EventReference teacherChalkWriteSFX;



    private EventInstance level01MusicInstance;
    private EventInstance teacherStaredownSFXInstance;


    private EventInstance pauseSnapshotInstance;
    private bool isPauseSnapshotActive;

    void Awake()
    {
        // Who up singling their ton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        LevelClock.onStageChange += OnStageChange;
        level01MusicInstance = CreateInstance(level01Music);
        teacherStaredownSFXInstance = CreateInstance(teacherStaredownSFX);


        pauseSnapshotInstance = CreateInstance(pauseSnapshot);
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
        level01MusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        if (pauseSnapshotInstance.isValid())
        {
            pauseSnapshotInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            pauseSnapshotInstance.release();
            pauseSnapshotInstance.clearHandle();
        }
        isPauseSnapshotActive = false;
    }


    public void playOneShot(string sound, Vector3? worldPos = null)
    {
        switch (sound)
        {
            case "clay_dodge":
                playOneShot(clayDodgeSFX, worldPos);
                break;

            case "clay_hurt":
                playOneShot(clayHurtSFX);
                break;

            case "teacher_chalk":
                playOneShot(teacherChalkWriteSFX, worldPos);
                break;

            default:
                return;
        }
    }

    public void playOneShot(EventReference sound, Vector3? worldPos = null)
    {
        Vector3 position = worldPos ?? Camera.main.transform.position;
        RuntimeManager.PlayOneShot(sound, position);
    }

    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference); 
        return eventInstance;
    }

    public void setGlobalParameter(string parameter, int value)
    {
        RuntimeManager.StudioSystem.setParameterByName(parameter, value);
    }

    public void setGlobalParameter(string parameter, float value)
    {
        RuntimeManager.StudioSystem.setParameterByName(parameter, value);
    }

    public void triggerEnd(string eventName)
    {
        setGlobalParameter(eventName + " End", 1);
    }

    public void togglePause()
    {
        FMOD.RESULT result = isPauseSnapshotActive
            ? pauseSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
            : pauseSnapshotInstance.start();
        isPauseSnapshotActive = !isPauseSnapshotActive;
    }


    
    void OnStageChange(int stage)
    {
        if (stage == 1)
        {
            Debug.Log("i think the game started.. maybe just me tho");
            level01MusicInstance.start();
        }
        else if (stage == 4)
        {
            teacherStaredownSFXInstance.start();
        }

        Debug.Log("[AudioManager] Switching to Stage " + stage);
        setGlobalParameter("Level 01 Suspicion Intensity", stage-1);
    }
    
}
