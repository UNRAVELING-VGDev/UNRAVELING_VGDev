using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] CentipedeBoss _boss;
    [SerializeField] Image _fill;
    [SerializeField] TMP_Text _label;

    void Awake()
    {
        if (_boss == null || _fill == null)
        {
            Debug.LogError("BossHealthBar needs a boss and fill image.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (_boss == null || _fill == null)
            return;

        _fill.sprite = null;
        _fill.overrideSprite = null;
        _fill.material = null;
        _fill.type = Image.Type.Simple;
        _boss.HealthChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (_boss != null)
            _boss.HealthChanged -= Refresh;
    }

    void Refresh()
    {
        float amount = Mathf.Clamp01(_boss.NormalizedHealth);
        _fill.rectTransform.localScale = new Vector3(amount, 1f, 1f);
        _fill.color = Color.red;
        if (_label != null)
            _label.text = $"Centipede  {Mathf.CeilToInt(_boss.Health)} / {Mathf.CeilToInt(_boss.MaxHealth)}";
    }
}
