using UnityEngine;

public class LightBeam : MonoBehaviour
{
    [Header("Beam Properties")]
    public Material beamMaterial;
    public Transform lightSource;
    public float beamThickness = 0.2f;
    public float cooldown = 3f;
    public float maxDistance = 30f;

    private float timer = 0f;
    private bool canShoot = true;

    void GenerateBeam(Vector3 origin, Vector3 direction, float length) // Assuming direction is normalized
    {
        Vector3 endPosition = origin + direction * length;

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.GetComponent<MeshRenderer>().material = beamMaterial;
        beam.transform.position = (origin + endPosition)/2;
        beam.transform.up = direction;
        beam.transform.localScale = new Vector3(beamThickness, length/2, beamThickness); // Local scale of cylinder prefab is 2 units tall for 1.0 y scale.
    }

    // Update is called once per frame
    void Update()
    {
        if (!canShoot) 
        {
            timer += Time.deltaTime;

            if (timer >= cooldown) 
            {
                canShoot = true;
            }
        }

        if (canShoot && Input.GetMouseButtonDown(0)) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f)) 
            {
                float totalDistance = 0;
                canShoot = false;
                timer = 0f;

                Vector3 origin = lightSource.position;
                Vector3 direction = (hit.point - origin).normalized;
                float distance = Vector3.Distance(origin, hit.point);

                while (true) 
                {
                    GenerateBeam(origin, direction, Mathf.Min(distance, maxDistance - totalDistance));
                    totalDistance += distance;

                    if (totalDistance >= maxDistance) break;

                    direction -= 2 * Vector3.Dot(direction, hit.normal) * hit.normal;
                    origin = hit.point;
                    if (Physics.Raycast(origin, direction, out hit, 100f)) 
                        distance = Vector3.Distance(origin, hit.point);
                    else
                        distance = maxDistance - totalDistance;
                }
            }
        }
    }
}
