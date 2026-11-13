using UnityEngine;

public class HallwayLights : MonoBehaviour
{
    public GameObject lightPrefab;
    public int numberOfLights = 10;
    public float distanceBetweenLights = 5f;
    public Vector3 spawnDirection = new Vector3(0, 0, 1);
    
    [ContextMenu("Place Lights")]
    public void PlaceLights()
    {
        ClearLights();
        for (int i = 0; i < numberOfLights; i++)
        {
            Vector3 position = transform.position + (spawnDirection * i * distanceBetweenLights);
            GameObject newLight = Instantiate(lightPrefab, position, transform.rotation);
            newLight.transform.SetParent(transform);
            newLight.name = "Light_" + i;
        }
    }

    [ContextMenu("Clear Lights")]
    public void ClearLights()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
