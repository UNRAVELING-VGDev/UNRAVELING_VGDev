using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameExit : MonoBehaviour
{
    [SerializeField] private string mainSceneName;
    [SerializeField] private string endSceneName;
    [SerializeField] private bool isFinalMinigame;

    public void ReturnToWalk()
    {
        if (isFinalMinigame)
        {
            MinigameManager.ResetProgress();
            SceneManager.LoadScene(endSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }
}