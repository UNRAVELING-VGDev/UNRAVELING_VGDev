using System;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SkillCheckSettings
{
    public float spinnerSpeed;
    public float spinnerMult = 1.1f;
}

public class SkillCheckHandler : MonoBehaviour
{
    [SerializeField] private SkillCheckSettings skillCheckSettings;
    [Header("Objects")]
    [SerializeField] protected Vector2 canvasPosition;
    [SerializeField] protected RectTransform backgroundRT;
    [SerializeField] protected RectTransform zoneRT;
    [SerializeField] protected RectTransform spinnerRT;
    [SerializeField] protected float radiusOffSet;
    RectTransform rt;
    Material mat;

    // Measured in radians, from 0 to 2pi.Also assumes that the radius is zoneRT's width / 2.
    float position = 0;
    int currentMin;
    int currentMax;
    bool forward = true;
    Unity.Mathematics.Random rng;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        mat = zoneRT.GetComponent<RawImage>().material;

        if (rt == null || backgroundRT == null || zoneRT == null || spinnerRT == null) {
            return;
        }

        rt.anchoredPosition = canvasPosition;
        backgroundRT.anchoredPosition = Vector2.zero;
        zoneRT.anchoredPosition = Vector2.zero;

        int textureID = Shader.PropertyToID("_imageTexture");
        mat.SetTexture(textureID, zoneRT.GetComponent<RawImage>().texture);

        rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        currentMin = rng.NextInt(360);
        Debug.Log(currentMin);
        currentMax = (currentMin + 30 + 360) % 360;
    }

    private void Update()
    {
        visualUpdate();
        if (Input.GetKeyUp(KeyCode.Space)) {
            float calcPos = position * 180 / Mathf.PI;
            if (currentMin < currentMax)
            {    
                if (calcPos >= currentMin && calcPos <= currentMax)
                {
                    generateSkillZone();
                }
            }
            else if (calcPos >= currentMin || calcPos <= currentMax)
            {
                generateSkillZone();
            }
        }
    }

    private void visualUpdate()
    {
        float radius = zoneRT.rect.width / 2 + radiusOffSet;

        spinnerRT.anchoredPosition = radius * new Vector2(Mathf.Cos(position), Mathf.Sin(position));
        spinnerRT.rotation = Quaternion.Euler(0, 0, (float)(position * 180 / Math.PI));
        position += (forward ? 1 : -1) * skillCheckSettings.spinnerSpeed * Time.deltaTime;
        position = (position + Mathf.PI * 2) % (Mathf.PI * 2);

        int minID = Shader.PropertyToID("_minAngle");
        int maxID = Shader.PropertyToID("_maxAngle");
        mat.SetFloat(minID, currentMin);
        mat.SetFloat(maxID, currentMax);
    }

    private void generateSkillZone()
    {
        forward = !forward;
        skillCheckSettings.spinnerSpeed *= skillCheckSettings.spinnerMult;
        currentMin = rng.NextInt(360);
        currentMax = (currentMin + 30 + 360) % 360;
    }
}
