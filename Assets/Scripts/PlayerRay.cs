using UnityEngine;

public class PlayerRay: MonoBehaviour
{
    public GameObject teacher;
    public Camera playerCamera;
    public TeacherState teacherState;
    public float paranoiaLevel = 0f;
    public float paranoiaIncreaseRate = 20f;
    public float paranoiaDecreaseRate = 10f;

    Ray ray;
    float maxDistance = 100f;

    void Update()
    {
        ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        CheckForCollider();
    }

    void CheckForCollider()
    {
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.gameObject == teacher && !teacherState.isFacingBoard)
            {
                Debug.Log("HIT THE TEACHER!");
                paranoiaLevel += paranoiaIncreaseRate * Time.deltaTime;
                paranoiaLevel = Mathf.Clamp(paranoiaLevel, 0f, 100f);
                Debug.Log("Paranoia: " + paranoiaLevel);
            }
        }
    }
    void CheckSuspicionLevel(){
        if (paranoiaLevel >= 90f)
        {
          Debug.Log("GAME OVER");
        }
        else if (paranoiaLevel >= 70f)
        {
            Debug.Log("SUSPICION LEVEL 3");
        }
        else if (paranoiaLevel >= 40f)
        {
            Debug.Log("SUSPICION LEVEL 2");
        }
        else if (paranoiaLevel >= 20f)
        {
            Debug.Log("SUSPICION LEVEL 1");
        }
        else
        {
            Debug.Log("NORMAL");
        }
    }
}