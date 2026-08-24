using UnityEngine;

public class PlayerGaze : MonoBehaviour
{
    public GameObject teacher;

    Ray ray;
    float maxDistance = 100f;

    void Update()
    {
        ray = new Ray(transform.position, transform.forward);
        CheckForCollider();
    }

    void CheckForCollider()
    {
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            Debug.Log("I hit: " + hit.collider.gameObject.name);

            if (hit.collider.gameObject == teacher)
            {
                Debug.Log("HIT THE TEACHER!");
            }
        }
    }
}