using UnityEngine;
using System;

public class LevelClock : MonoBehaviour
{
    // 4 is staredown; 5 is level complete
    public int stage { get; private set; }
    public static event Action<int> onStageChange;

    // fires once, after the teacher's walk-up, to start the skill check
    public static event Action OnStaredownReady;

    // Time is measured in seconds
    [SerializeField] float stage1Duration;
    [SerializeField] float stage2Duration;
    [SerializeField] float stage3Duration;
    [SerializeField] float staredownDuration;

    // how long the teacher's walk-up takes before the skill check starts
    [SerializeField] float walkUpDuration = 2f;

    private float staredownStartTime;
    private bool staredownStarted = false;

    void OnEnable()
    {
        SkillCheckHandler.OnSkillCheckPassed += CompleteLevel;
    }

    void OnDisable()
    {
        SkillCheckHandler.OnSkillCheckPassed -= CompleteLevel;
    }

    void Start()
    {
        stage = 1;
        onStageChange?.Invoke(stage);
    }

    void Update()
    {
        if (stage < 2 && Time.timeSinceLevelLoad >= stage1Duration)
        {
            stage = 2;
            onStageChange?.Invoke(stage);
            Debug.Log("stage is " + stage);
        }
        if (stage < 3 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration)
        {
            stage = 3;
            onStageChange?.Invoke(stage);
            Debug.Log("stage is " + stage);
        }
        if (stage < 4 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration + stage3Duration)
        {
            stage = 4;
            onStageChange?.Invoke(stage);
            Debug.Log("stage is " + stage);
            staredownStartTime = Time.timeSinceLevelLoad;
        }

        if (stage == 4)
        {
            float stareLevel = staredownDuration > 0f
                ? Mathf.Clamp01((Time.timeSinceLevelLoad - staredownStartTime) / staredownDuration)
                : 1f;
            AudioManager.instance.setGlobalParameter("Teacher Stare Level", stareLevel);

            // after the walk-up finishes, start the skill check (once)
            if (!staredownStarted && Time.timeSinceLevelLoad - staredownStartTime >= walkUpDuration)
            {
                staredownStarted = true;
                OnStaredownReady?.Invoke();
            }
        }
    }

    // called when the skill check is passed: stage 5 = level complete
    public void CompleteLevel()
    {
        if (stage >= 5) return;

        stage = 5;
        onStageChange?.Invoke(5);
        Debug.Log("stage is " + stage);

        AudioManager.instance.triggerEnd("Teacher Stare"); // End?
        AudioManager.instance.triggerEnd("Level 01 Music");
    }
}