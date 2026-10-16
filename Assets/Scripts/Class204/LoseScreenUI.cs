using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Timeline.DirectorControlPlayable;

public class LoseScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject losePanel;

    private void OnEnable()
    {
        LoseState.onGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        LoseState.onGameOver -= OnGameOver;
    }

    private void OnGameOver()
    {
        losePanel.SetActive(true);
    }

    public void RetryButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public void MainMenuButton()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }
}
