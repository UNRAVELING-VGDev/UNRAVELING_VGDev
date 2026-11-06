using UnityEngine;

// Randomizes each butterfly's flap animation speed/start time and adds a gentle wobble.
public class ButterflyVariation : MonoBehaviour
{
    public float minSpeed = 0.7f;
    public float maxSpeed = 1.3f;
    public float wobble = 15f;       // max degrees of tilt
    public float wobbleSpeed = 1f;

    Quaternion startRotation;
    float offset;

    void Start()
    {
        Animator anim = GetComponentInChildren<Animator>();
        anim.speed = Random.Range(minSpeed, maxSpeed);
        anim.Play(0, 0, Random.value);

        startRotation = transform.localRotation;
        offset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float t = Time.time * wobbleSpeed + offset;
        float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * wobble;
        float z = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * wobble;
        transform.localRotation = startRotation * Quaternion.Euler(x, 0f, z);
    }
}