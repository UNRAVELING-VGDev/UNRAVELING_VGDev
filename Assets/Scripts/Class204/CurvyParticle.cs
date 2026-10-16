using UnityEngine;

public class CurvyParticle : MonoBehaviour, IAttackMovement
{
    public float speed = 6f;
    public float acceleration = 1f;
    public float bendStrength;
    public float minStrength = 1.5f;
    public float maxStrength = 3f;
    public float bendAngle;
    //How many times the particle weaves back and forth
    public float frequency;
    public float minFrequency = 1f;
    public float maxFrequency = 3f;

    //Implementing a sin wave that can curve multiple times over the flight
    public Vector3 GetPosition(Vector3 start, Vector3 target, float timeElapsed)
    {
        //Randomized Values
        if (timeElapsed <= 0.01f)
        {   
        bendStrength = Random.Range(minStrength, maxStrength);
        bendAngle = Random.Range(-90f, 90f); //restricted to upper half since cant go under
        frequency = Random.Range(minFrequency, maxFrequency);
        }

        //normalizes the time to [0, 1] so we don't overshoot
        float distanceTotal = Vector3.Distance(start, target);
        float distanceTraveled = 0.5f * acceleration * timeElapsed * timeElapsed;
        float timeNormalized = Mathf.Clamp01(distanceTraveled / distanceTotal);

        //Getting the flight direction of the particle
        Vector3 direction = (target - start).normalized;
        Vector3 sideways = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 upward = Vector3.Cross(sideways, direction).normalized;

        //Rotates upward around the bend angle where 0 = up, -90 = left, 90 = right
        Quaternion rotation = Quaternion.AngleAxis(bendAngle, direction);
        Vector3 bendDirection = rotation * upward;

        //Base Position moving straight from start to target
        Vector3 basePosition = Vector3.Lerp(start, target, timeNormalized);

        //fadeCurve is needed to make sure our particle starts at start and ends at target no Matter the freq
        float fadeCurve = Mathf.Sin(timeNormalized * Mathf.PI);
        float wave = Mathf.Sin(timeNormalized * frequency * Mathf.PI * 2f) * bendStrength * fadeCurve;

        Vector3 result = basePosition + bendDirection * wave;
        return result;
    }
}
