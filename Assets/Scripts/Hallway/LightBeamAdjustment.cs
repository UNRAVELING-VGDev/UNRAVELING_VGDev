using UnityEngine;
using UnityEngine.InputSystem;

public class LightBeamAdjustment : MonoBehaviour
{
    [Header("Flashlight Properties")]
    [SerializeField] public float defaultSpotAngle;

    // Intensity of flashlight normally
    [SerializeField] public float minIntensity;
    // Intensity of the flashlight when it is fully compressed (right before the beam shoots)
    [SerializeField] public float maxIntensity;
    
    // If you want the transition between flashlight mode and beam mode to be instant, set these values to 0
    [SerializeField] public float timeToBeam;
    [SerializeField] public float timeToFlashlight;

    private Light handLight;

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
        beam.transform.position = (origin + endPosition) / 2;
        beam.transform.up = direction;
        beam.transform.localScale = new Vector3(beamThickness, length / 2, beamThickness); // Local scale of cylinder prefab is 2 units tall for 1.0 y scale.
    }

    void Start()
    {
        handLight = GetComponent<Light>();
        handLight.spotAngle = defaultSpotAngle;
        handLight.innerSpotAngle = defaultSpotAngle;
        handLight.intensity = minIntensity;
    }

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

        if (Input.GetMouseButton(0) && canShoot) {
            if (handLight.spotAngle > 1) {
                if (timeToBeam == 0) {
                    handLight.spotAngle = 0;
                    handLight.innerSpotAngle = 0;
                } else {
                    handLight.spotAngle -= (defaultSpotAngle / timeToBeam) * Time.deltaTime;
                    handLight.innerSpotAngle -= (defaultSpotAngle / timeToBeam) * Time.deltaTime;
                    handLight.intensity += ((maxIntensity - minIntensity) / timeToBeam) * Time.deltaTime;
                }
            } else {
                handLight.enabled = false;
                if (canShoot)
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
        } else {
            if (handLight.spotAngle < defaultSpotAngle) {
                handLight.enabled = true;
                if (timeToFlashlight == 0) {
                    handLight.spotAngle = defaultSpotAngle;
                    handLight.innerSpotAngle = defaultSpotAngle;
                } else {
                    handLight.spotAngle += (defaultSpotAngle / timeToFlashlight) * Time.deltaTime;
                    handLight.innerSpotAngle += (defaultSpotAngle / timeToFlashlight) * Time.deltaTime;
                    handLight.intensity -= ((maxIntensity - minIntensity) / timeToFlashlight) * Time.deltaTime;
                }
                
            } 
        }
    }
}
