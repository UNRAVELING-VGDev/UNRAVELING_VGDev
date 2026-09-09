using UnityEngine;
using TMPro;

/// Holds the player's paranoia, 0 to maxParanoia. Driven by the player:
/// eye contact and creature hits raise it (Add with a positive), healing
/// letters lower it (Add with a negative).
///
/// This is the PARANOIA axis only. It drives the visual/audio effects.
/// It has NOTHING to do with the teacher's escalation - that runs on the
/// separate time-based level clock.
///
/// VFX scripts read `Normalized` (0 to 1) every frame to scale their effects.
public class ParanoiaMeter : MonoBehaviour
{
    public float paranoiaLevel = 0f;
    public float maxParanoia = 200f;

    public TextMeshProUGUI paranoiaText;   // optional debug readout

    // 0 to 1, for effect scripts that want to scale smoothly with paranoia.
    public float Normalized => paranoiaLevel / maxParanoia;

    // True once paranoia is maxed. The lose/faint system watches this.
    public bool IsMaxed => paranoiaLevel >= maxParanoia;

    public void Add(float amount)
    {
        paranoiaLevel = Mathf.Clamp(paranoiaLevel + amount, 0f, maxParanoia);
        ParanoiaVFXFeature.SetTension(paranoiaLevel / maxParanoia);
    }

    void Update()
    {
        if (paranoiaText != null)
            paranoiaText.text = "Paranoia: " + paranoiaLevel.ToString("F0");
    }
}