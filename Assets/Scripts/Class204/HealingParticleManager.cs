using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class HealingParticleManager : MonoBehaviour
{
    public BoxCollider roomBounds;
    public float minPadding = 0.5f;
    public float maxPadding = 1.5f;

    public GameObject healingParticle;
    public Transform target;

    public float minSpawnDelay = 8f;
    public float maxSpawnDelay = 12f;

    void Start()
    {
        StartCoroutine(HealingLoop());
    }

    Vector3 GetRandomPos()
    {
        Bounds b = roomBounds.bounds;

        switch(Random.Range(1, 4))
        {
            case 1:
                return new Vector3(
                    Random.Range(b.min.x + minPadding, b.min.x + maxPadding),
                    Random.Range(b.min.y + minPadding, b.max.y - minPadding),
                    Random.Range(b.min.z + minPadding, b.max.z - minPadding)
                );
            case 2:
                return new Vector3(
                    Random.Range(b.max.x - minPadding, b.max.x - maxPadding),
                    Random.Range(b.min.y + minPadding, b.max.y - minPadding),
                    Random.Range(b.min.z + minPadding, b.max.z - minPadding)
                );
            case 3:
                return new Vector3(
                    Random.Range(b.min.x + minPadding, b.max.x - minPadding),
                    Random.Range(b.max.y - minPadding, b.max.y - maxPadding),
                    Random.Range(b.min.z + minPadding, b.max.z - minPadding)
                );
            default:
                return new Vector3(
                    Random.Range(b.min.x + minPadding, b.max.x - minPadding),
                    Random.Range(b.min.y + minPadding, b.max.y - minPadding),
                    Random.Range(b.max.z - minPadding, b.max.z - maxPadding)
                );
        }
    }

    IEnumerator HealingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));

            GameObject newHealingParticle = Instantiate(healingParticle);
            newHealingParticle.transform.position = GetRandomPos();

            HealingParticle healingParticleScript = newHealingParticle.GetComponent<HealingParticle>();
            healingParticleScript.SetTarget(target);
        }
    }
}
