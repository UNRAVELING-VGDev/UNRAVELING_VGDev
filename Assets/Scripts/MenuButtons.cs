using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject levelsPanel;
    private GameObject currentPanel;

    public void Start()
    {
        currentPanel = mainPanel;
    }

    public void PlayLevel1()
    {
        SceneManager.LoadScene("Class204");
    }

    public void PlayLevel2()
    {
        SceneManager.LoadScene("HallwayP1");
    }

    public void ShowPanel(GameObject panel)
    {
        mainPanel.SetActive(false);
        panel.SetActive(true);
        currentPanel = panel;
    }

    public void BackToMenu()
    {
        currentPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void Quit()
    {
        Debug.Log("Quit button pressed.");
        Application.Quit();
    }
}
