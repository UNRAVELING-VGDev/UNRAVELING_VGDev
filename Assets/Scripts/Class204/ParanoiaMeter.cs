using UnityEngine;
using TMPro;

public class ParanoiaMeter : MonoBehaviour
{
    public float paranoiaLevel;
    public float maxParanoia = 200f;

    public TextMeshProUGUI paranoiaText;

    public ParanoiaUI paranoiaUI;

    public bool IsLocked { get; private set; } = false;

    public float Normalized => paranoiaLevel / maxParanoia;

    public bool IsMaxed => paranoiaLevel >= maxParanoia;

    void OnEnable()
    {
        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void Start()
    {
        ParanoiaVFXFeature.SetTension(paranoiaLevel / maxParanoia);
        if (paranoiaUI != null) paranoiaUI.UpdateParanoiaUI();
    }

    void OnStageChange(int newStage)
    {
        if (newStage >= 4)
            IsLocked = true;
    }

    public void Add(float amount, bool ignoreLock = false)
    {
        if (IsLocked && amount > 0f && !ignoreLock) return;

        paranoiaLevel = Mathf.Clamp(paranoiaLevel + amount, 0f, maxParanoia);
        ParanoiaVFXFeature.SetTension(paranoiaLevel / maxParanoia);
        if (paranoiaUI != null) paranoiaUI.UpdateParanoiaUI();
    }

    void Update()
    {
        if (paranoiaText != null)
            paranoiaText.text = "Paranoia: " + paranoiaLevel.ToString("F0");
    }

    private void OnDestroy()
    {
        ParanoiaVFXFeature.SetTension(0f);
    }
}