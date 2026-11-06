using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    [SerializeField] private string nextScene = "HallwayP1";  
    [SerializeField] private float delay = 1.5f;            

    private bool loading = false;

    void OnEnable()
    {
        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void OnStageChange(int stage)
    {
        if (stage != 5 || loading) return;

        LoseState lose = FindFirstObjectByType<LoseState>();
        if (lose != null && lose.IsOver) return;

        loading = true;

        SkillCheckHandler skillCheck = FindFirstObjectByType<SkillCheckHandler>();
        if (skillCheck != null) skillCheck.StopSkillCheck();

        StartCoroutine(LoadNext());
    }

    IEnumerator LoadNext()
    {
        yield return new WaitForSecondsRealtime(delay);

        ParanoiaVFXFeature.SetTension(0f);
        Time.timeScale = 1f;

        SceneManager.LoadScene(nextScene);
    }
}