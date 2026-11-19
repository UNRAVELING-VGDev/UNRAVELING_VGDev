using UnityEngine;

public class HallwayOnboarding : MonoBehaviour
{
    [SerializeField] OnboardingController _onboarding;
    [SerializeField] DuckController _duck;
    [SerializeField] PlayerControl _player;
    [SerializeField] LightBeamAdjustment _flashlight;

    void Awake()
    {
        if (_onboarding == null)
        {
            Debug.LogError("HallwayOnboarding needs an OnboardingController.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (_duck != null)
            _duck.DuckCompleted += OnDuckCompleted;
        if (_player != null)
            _player.DashCompleted += OnDashCompleted;
        if (_flashlight != null)
            _flashlight.BeamFired += OnBeamFired;
    }

    void LateUpdate()
    {
        _onboarding.SetLessonRelevant(OnboardingLesson.Duck, _duck != null && _duck.isActiveAndEnabled);
        _onboarding.SetLessonRelevant(OnboardingLesson.Dash, _player != null && _player.isActiveAndEnabled);
        _onboarding.SetLessonRelevant(OnboardingLesson.Flashlight, _flashlight != null && _flashlight.isActiveAndEnabled);
    }

    void OnDuckCompleted()
    {
        if (_onboarding.isActiveAndEnabled)
            _onboarding.CompleteLesson(OnboardingLesson.Duck);
    }

    void OnDashCompleted()
    {
        if (_onboarding.isActiveAndEnabled)
            _onboarding.CompleteLesson(OnboardingLesson.Dash);
    }

    void OnBeamFired()
    {
        if (_onboarding.isActiveAndEnabled)
            _onboarding.CompleteLesson(OnboardingLesson.Flashlight);
    }

    void OnDisable()
    {
        if (_duck != null)
            _duck.DuckCompleted -= OnDuckCompleted;
        if (_player != null)
            _player.DashCompleted -= OnDashCompleted;
        if (_flashlight != null)
            _flashlight.BeamFired -= OnBeamFired;
        if (_onboarding == null)
            return;

        _onboarding.SetLessonRelevant(OnboardingLesson.Duck, false);
        _onboarding.SetLessonRelevant(OnboardingLesson.Dash, false);
        _onboarding.SetLessonRelevant(OnboardingLesson.Flashlight, false);
    }
}
