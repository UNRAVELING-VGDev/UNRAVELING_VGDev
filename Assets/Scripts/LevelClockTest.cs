using UnityEngine;

public class LevelClockTest : MonoBehaviour
{
    private void OnEnable()
    {
        LevelClock.onStageChange += getStageChange;
    }

    private void OnDisable()
    {
        LevelClock.onStageChange += getStageChange;
    }

    void getStageChange(int stage)
    {
        if (stage == 1)
        {
            gotStage1();
        }
        if (stage == 2)
        {
            gotStage2();
        }
        if (stage == 3)
        {
            gotStage3();
        }
        if (stage == 4)
        {
            gotStaredown();
        }
        if (stage == 5)
        {
            gotLevelComplete();
        }
    }

    void gotStage1()
    {
        print("got stage 1");
    }

    void gotStage2()
    {
        print("got stage 2");
    }

    void gotStage3()
    {
        print("got stage 3");
    }

    void gotStaredown()
    {
        print("got staredown");
    }

    void gotLevelComplete()
    {
        print("got level complete");
    }

}
