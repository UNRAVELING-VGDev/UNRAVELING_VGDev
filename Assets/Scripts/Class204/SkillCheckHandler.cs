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

    float position = 0;
    int currentMin;
    int currentMax;
    bool forward = true;
    Unity.Mathematics.Random rng;

    private bool isActive = false;

    private void OnEnable()
    {
        // start the skill check the moment the teacher finishes walking up
        TeacherState.OnArrivedAtPlayer += StartSkillCheck;
    }

    private void OnDisable()
    {
        TeacherState.OnArrivedAtPlayer -= StartSkillCheck;
    }

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
        currentMax = (currentMin + 30 + 360) % 360;

        // hide the skill-check visuals until the teacher arrives
        SetVisualsVisible(false);
    }

    // called when the teacher has walked up to the player
    public void StartSkillCheck()
    {
        isActive = true;
        SetVisualsVisible(true);
    }

    private void SetVisualsVisible(bool visible)
    {
        if (backgroundRT != null) backgroundRT.gameObject.SetActive(visible);
        if (zoneRT != null) zoneRT.gameObject.SetActive(visible);
        if (spinnerRT != null) spinnerRT.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (!isActive) return;

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