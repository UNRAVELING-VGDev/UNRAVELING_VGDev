using UnityEngine;
using UnityEngine.UI;

public class ParanoiaUI : MonoBehaviour
{
    public Image baseImage;
    public Image overlayImage;
    public ParanoiaMeter paranoiaMeter;
    public Sprite[] paranoiaSprites; // Array of sprites for different paranoia levels

    public void UpdateParanoiaUI()
    {
        float normalizedParanoia = paranoiaMeter.Normalized;

        // Determine which overlay sprite to use based on paranoia level
        int baseIndex = Mathf.FloorToInt(normalizedParanoia * (paranoiaSprites.Length - 1));
        int overlayIndex = Mathf.Clamp(baseIndex + 1, 0, paranoiaSprites.Length - 1);

        float fadeAlpha = normalizedParanoia * (paranoiaSprites.Length - 1) - baseIndex;
        
        baseImage.sprite = paranoiaSprites[baseIndex];

        if (baseIndex == overlayIndex)
        {
            overlayImage.enabled = false;
        }
        else
        {
            overlayImage.sprite = paranoiaSprites[overlayIndex];
            overlayImage.enabled = true;
            overlayImage.color = new Color(1f, 1f, 1f, fadeAlpha);
        }
    }
}
