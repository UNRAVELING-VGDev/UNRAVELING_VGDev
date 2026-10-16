using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameManager : MonoBehaviour
{
    [Serializable]
    public class Checkpoint
    {
        public Transform point;
        public string sceneName;
        public float radius = 2f;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Checkpoint[] checkpoints;

    //made these static so keeps values even after scene changes
    private static int nextIndex = 0;
    private static Vector3 savedPosition;
    private static bool hasSavedPosition = false;
    
    void Start()
    {
        //puts player back to where they were after the minigame
        if (hasSavedPosition)
        {
            player.position = savedPosition;
        }
    }

    void Update()
    {
        if (nextIndex >= checkpoints.Length) return; //finish

        Checkpoint checkp = checkpoints[nextIndex];
        float dist = Vector3.Distance(player.position, checkp.point.position);

        if (dist <= checkp.radius)
        {
            savedPosition = player.position;
            hasSavedPosition = true;
            nextIndex++;
            SceneManager.LoadScene(checkp.sceneName);
        }
    }

    //method for reseting if we leave the game for example
    public static void ResetProgress()
    {
        nextIndex = 0;
        hasSavedPosition = false;
    }

    //for testing purposes (seeing the checkpoints)
    void OnDrawGizmosSelected()
    {
        if (checkpoints == null) return;
        Gizmos.color = Color.red;
        foreach (var checkp in checkpoints)
        {
            if (checkp.point != null)
                Gizmos.DrawWireSphere(checkp.point.position, checkp.radius);
        }
    }
}
