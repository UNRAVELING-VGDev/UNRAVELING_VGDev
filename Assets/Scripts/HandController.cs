using UnityEngine;
public class HandController : MonoBehaviour
{
    [Header("Inertia")]
    [SerializeField] float stiffness = 60f; // pull strength
    [SerializeField] float damping = 10f; // drag
    [Header("Reach")]
    [SerializeField] Camera playerCamera;
    [SerializeField] float handDepth = 3.5f; // distance in front of cam
    [Header("Swat")]
    [SerializeField] float hitRadius = 0.4f;
    [SerializeField] float swatCooldown = 0.75f;

    [Header("Stats")]
    public int hits;
    Vector3 velocity;
    float cooldown;
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        // Start at the target so hand doesn't move eradically during game start
        transform.position = GetTarget();
    }
    // so the hand can't read the camera before it has turned
    void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        // physics formula for hand 
        Vector3 acceleration = stiffness * (GetTarget() - transform.position) - damping * velocity; 
        velocity += acceleration * dt;
        transform.position += velocity * dt;
        cooldown -= dt;
          
        if (Input.GetMouseButtonDown(0) && cooldown <= 0f)
        {
            Debug.Log("MOUSE CLICKED");
            cooldown = swatCooldown;
            Swat();
        }
    }

    void Swat()
    {
        Collider[] found = Physics.OverlapSphere(transform.position, hitRadius);
        Debug.Log("Colliders found: " + found.Length);

        foreach (Collider c in found)
        {
            // dont detect the hand itself
            if (c.transform == transform)
                continue;
            Debug.Log("Found collider: " + c.name);
            
           if (c.TryGetComponent(out FireflyParticle f))
            {
                Debug.Log("FOUND FIREFLY!");
                hits++;
                f.Despawn();
                Debug.Log("HIT " + hits);
            }
        }
    }

    // Helper method that works out where the hand wants to be
    Vector3 GetTarget()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(-playerCamera.transform.forward,
                                playerCamera.transform.position + playerCamera.transform.forward * handDepth);
        return plane.Raycast(ray, out float dist) 
            ? ray.GetPoint(dist) 
            : transform.position;
    }
    //to visually show the hit radius when gizmos is turned on
    void OnDrawGizmos()
    {
        Debug.Log("gizmo drawing");
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
