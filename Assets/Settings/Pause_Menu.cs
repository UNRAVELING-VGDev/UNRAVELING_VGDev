using UnityEngine;

public class Pause_Menu : MonoBehaviour
{
    public GameObject Container;
    public static bool PauseActive = false;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            // if pause is active, set to unpaused. if pause is not active, set to paused.
            SetPaused(!PauseActive);
        }
    }

    public void ResumeButton()
    {
        SetPaused(false);
    }

    public void MainMenuButton()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PauseActive = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    /* If setting to paused: set the time scale to 0, show/unlock the cursor, and toggle the audio.
       If setting to unpaused: set the time scale to 1, hide/lock the cursor, and toggle the audio. */
    public void SetPaused(bool isPaused)
    {
        Container.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
        PauseActive = isPaused;
        AudioManager.instance.togglePause();
    }
}
