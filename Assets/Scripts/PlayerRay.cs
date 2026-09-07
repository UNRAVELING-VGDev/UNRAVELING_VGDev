using UnityEngine;

public class PlayerRay : MonoBehaviour
{
    public SuspicionMeter suspicion;
    public GameObject teacher;
    public Camera playerCamera;
    public TeacherState teacherState;

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

        bool lookingAtTeacher = false;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.gameObject == teacher && !teacherState.isFacingBoard)
                lookingAtTeacher = true;
        }

        if (lookingAtTeacher)
            suspicion.Add(paranoiaIncreaseRate * Time.deltaTime);
        else
            suspicion.Add(-paranoiaDecreaseRate * Time.deltaTime);
    }
}