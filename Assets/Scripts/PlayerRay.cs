using UnityEngine;

public class PlayerRay : MonoBehaviour
{
    public ParanoiaMeter paranoia;
    public GameObject teacher;
    public Camera playerCamera;
    public TeacherState teacherState;

    public float paranoiaIncreaseRate = 20f;

    Ray ray;
    float maxDistance = 500f; 

    void Update()
    {
        ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        CheckForCollider();
    }

    void CheckForCollider()
    {
        RaycastHit hit;

        bool lookingAtTeacher = false;

        if (Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("ray hit: " + hit.collider.gameObject.name);
            if (hit.collider.transform.IsChildOf(teacher.transform) && teacherState.isFacingPlayer)
                lookingAtTeacher = true;
        }
        else
        {
            Debug.Log("ray hit NOTHING");
        }

        if (lookingAtTeacher)
            paranoia.Add(paranoiaIncreaseRate * Time.deltaTime);
    }
}