using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;        // the panel with the buttons
    [SerializeField] private float showDelay = 1.5f;      // wait for the camera to turn back first
    [SerializeField] private string mainMenuScene = "";   // leave empty to quit the game instead

    void Awake()
    {
        if (menuPanel != null) menuPanel.SetActive(false);   // hidden until you lose
    }

    void OnEnable()
    {
        LoseState.onGameOver += ShowMenu;
    }

    void OnDisable()
    {
        LoseState.onGameOver -= ShowMenu;
    }

    void ShowMenu()
    {
        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);
        if (menuPanel != null) menuPanel.SetActive(true);

        // make sure the mouse is usable for the buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void MainMenu()
    {
        Time.timeScale = 1f;                      
        ParanoiaVFXFeature.SetTension(0f);    
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    // hook this to the Restart button
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // hook this to the Exit button
    public void Exit()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuScene)) {
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // stops Play mode in the editor
#else
        Application.Quit();                                // closes the built game
#endif
    }
}