
using UnityEngine;
using TMPro;
 
public class SuspicionMeter : MonoBehaviour
{
    public float paranoiaLevel = 0f;
    public float maxParanoia = 200f;
 
    // Stage boundaries as fractions of maxParanoia, so changing the max
    // rescales everything instead of making stage 4 unreachable.
    public float[] stageThresholds = { 0.25f, 0.5f, 0.75f, 1f };
 
    // How far paranoia must fall below a threshold before dropping out of
    // that stage. Stops the teacher flickering between behaviours when the
    // value hovers on a boundary.
    public float hysteresis = 15f;
 
    public float logInterval = 3f;
 
    public TextMeshProUGUI paranoiaText;
 
    public int Level { get; private set; }
 
    private int lastLevel = -1;
    private float logTimer;
 
    public void Add(float amount)
    {
        paranoiaLevel = Mathf.Clamp(paranoiaLevel + amount, 0f, maxParanoia);
        ParanoiaVFXFeature.SetTension(paranoiaLevel / maxParanoia);
    }
 
    void Update()
    {
        Level = CalculateLevel();
 
        logTimer -= Time.deltaTime;
        if (logTimer <= 0f)
        {
            logTimer = logInterval;
            Debug.Log("Paranoia: " + paranoiaLevel.ToString("F0"));
        }
 
        if (Level != lastLevel)
        {
            lastLevel = Level;
            OnLevelChanged(Level);
        }
        if (paranoiaText != null){
        paranoiaText.text = "Paranoia: " + paranoiaLevel.ToString("F0");
        }
    }
 
    int CalculateLevel()
    {
        // Stage 4 is the faint — once you're there you stay there.
        if (lastLevel >= stageThresholds.Length) return stageThresholds.Length;
 
        int current = 0;
 
        for (int i = 0; i < stageThresholds.Length; i++)
        {
            float bar = stageThresholds[i] * maxParanoia;
 
            // Already in this stage or higher? Make it harder to fall out of.
            if (lastLevel > i) bar -= hysteresis;
 
            if (paranoiaLevel >= bar) current = i + 1;
        }
 
        return current;
    }
 
    void OnLevelChanged(int level)
    {
        Debug.Log("Suspicion level: " + level);
 
        if (level == stageThresholds.Length)
            Debug.Log("GAME OVER");
    }
}