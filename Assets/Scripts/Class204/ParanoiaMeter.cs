using UnityEngine;
using TMPro;

public class ParanoiaMeter : MonoBehaviour
{
    public float paranoiaLevel;
    public float maxParanoia = 200f;

    public TextMeshProUGUI paranoiaText;   
    
    public ParanoiaUI paranoiaUI;

    public float Normalized => paranoiaLevel / maxParanoia;

    public bool IsMaxed => paranoiaLevel >= maxParanoia;

    public void Add(float amount)
    {
        paranoiaLevel = Mathf.Clamp(paranoiaLevel + amount, 0f, maxParanoia);
        ParanoiaVFXFeature.SetTension(paranoiaLevel / maxParanoia);
        paranoiaUI.UpdateParanoiaUI();
    }

    void Update()
    {
        Debug.Log("Paranoia: " + paranoiaLevel.ToString("F0") + " / " + maxParanoia + "  (normalized: " + Normalized.ToString("F2") + ")");

        if (paranoiaText != null)
            paranoiaText.text = "Paranoia: " + paranoiaLevel.ToString("F0");
    }
}