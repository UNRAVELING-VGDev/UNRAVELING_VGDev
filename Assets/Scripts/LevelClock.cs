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
        }
        if (stage < 3 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration)
        {
            stage = 3;
            onStageChange?.Invoke(stage);
        }
        if (stage < 4 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration + stage3Duration)
        {
            stage = 4;
            onStageChange?.Invoke(stage);
        }
        if (stage < 5 && Time.timeSinceLevelLoad >= stage1Duration + stage2Duration + stage3Duration + staredownDuration)
        {
            stage = 5;
            onStageChange?.Invoke(5);
        }
    }
}
