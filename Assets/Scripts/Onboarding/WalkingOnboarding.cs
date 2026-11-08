using UnityEngine;
using UnityEngine.InputSystem;

public class WalkingOnboarding : MonoBehaviour
{
    [SerializeField] OnboardingController _onboarding;
    [SerializeField] MinigameMovement _minigameMovement;
    [SerializeField] PlayerControl _playerControl;
    [Min(0.1f)] [SerializeField] float _requiredWalkDistance = 1.5f;

    float _walkedDistance;

    void Awake()
    {
        if (_onboarding == null || (_minigameMovement == null) == (_playerControl == null))
        {
            Debug.LogError("WalkingOnboarding needs a controller and exactly one movement component.", this);
            enabled = false;
        }
    }

    void LateUpdate()
    {
        bool available = _minigameMovement != null
            ? _minigameMovement.isActiveAndEnabled
            : _playerControl != null && _playerControl.isActiveAndEnabled;
        _onboarding.SetLessonRelevant(OnboardingLesson.FreeMovement, available);

        if (!available || !_onboarding.isActiveAndEnabled || Time.timeScale <= 0f
            || _onboarding.IsLessonCompleted(OnboardingLesson.FreeMovement) || !HasWalkInput())
            return;

        _walkedDistance += _minigameMovement != null
            ? _minigameMovement.WalkingDistanceThisFrame
            : _playerControl.WalkingDistanceThisFrame;

        if (_walkedDistance >= _requiredWalkDistance)
            _onboarding.CompleteLesson(OnboardingLesson.FreeMovement);
    }

    bool HasWalkInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        int forward = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        int sideways = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
        return forward != 0 || sideways != 0;
    }

    void OnDisable()
    {
        if (_onboarding != null)
            _onboarding.SetLessonRelevant(OnboardingLesson.FreeMovement, false);
    }
}
