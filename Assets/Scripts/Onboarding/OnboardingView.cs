using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class OnboardingView : MonoBehaviour
{
    [SerializeField] TMP_Text _promptText;
    [Min(0f)] [SerializeField] float _fadeDuration = 0.3f;

    CanvasGroup _canvasGroup;
    string _requestedPrompt;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_promptText == null)
        {
            Debug.LogError("OnboardingView needs a TextMeshPro text component.", this);
            enabled = false;
            return;
        }

        _promptText.raycastTarget = false;
        _promptText.text = string.Empty;
    }

    void Update()
    {
        bool changingText = _requestedPrompt != null && _promptText.text != _requestedPrompt;
        float targetAlpha = _requestedPrompt != null && !changingText ? 1f : 0f;
        _canvasGroup.alpha = _fadeDuration <= 0f
            ? targetAlpha
            : Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime / _fadeDuration);

        if (_canvasGroup.alpha <= 0f)
            _promptText.text = _requestedPrompt ?? string.Empty;
    }

    public void ShowPrompt(string prompt)
    {
        _requestedPrompt = string.IsNullOrWhiteSpace(prompt) ? null : prompt;
    }

    public void HidePrompt()
    {
        _requestedPrompt = null;
    }

    public bool IsShowing(string prompt)
    {
        return isActiveAndEnabled && _canvasGroup != null && _promptText != null
            && _requestedPrompt == prompt && _promptText.text == prompt
            && _canvasGroup.alpha >= 1f;
    }

    void OnDisable()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        if (_promptText != null)
            _promptText.text = string.Empty;
    }
}
