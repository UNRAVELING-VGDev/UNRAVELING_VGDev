using UnityEngine;
using System;

public class LevelClock : MonoBehaviour
{
    // 4 is staredown; 5 is level complete
    public int stage { get; private set; }
    public static event Action<int> onStageChange;

    // Time is measured in seconds
    [SerializeField] float stage1Duration;
    [SerializeField] float stage2Duration;
    [SerializeField] float stage3Duration;
    [SerializeField] float staredownDuration;

    private float staredownStartTime;

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
        if (stage < 5 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration + stage3Duration + staredownDuration)
        {
            stage = 5;
            onStageChange?.Invoke(5);
            Debug.Log("stage is " + stage);

            AudioManager.instance.triggerEnd("Teacher Stare"); // End?
            AudioManager.instance.triggerEnd("Level 01 Music");
        }

        if (stage == 4)
        {
            AudioManager.instance.setGlobalParameter("Teacher Stare Level", (Time.timeSinceLevelLoad - staredownStartTime) / staredownDuration);
        }
    }
}
