using UnityEngine;

public class ParticlePlaceholder : MonoBehaviour, IAttackMovement
{
    public float speed = 6f;
    public float acceleration = 6f;

    public Vector3 GetPosition(Vector3 start, Vector3 target, float timeElapsed)
    {
        // accelerating straight line from start toward the locked target
        float distance = 0.5f * acceleration * timeElapsed * timeElapsed;  // simple accel
        return Vector3.MoveTowards(start, target, distance);
    }
}
