using System.Collections.Generic;
using UnityEngine;

public class ClassroomOnboarding : MonoBehaviour
{
    [SerializeField] OnboardingController _onboarding;
    [SerializeField] TeacherState _teacher;
    [SerializeField] PlayerRay _gaze;
    [SerializeField] LeanController _lean;
    [SerializeField] LoseState _loseState;
    [SerializeField] LevelClock _clock;
    [SerializeField] BookController _book;
    [SerializeField] HealingParticleManager _healingManager;
    [Min(0.01f)] [SerializeField] float _lookAwayDuration = 0.75f;
    [Min(0.01f)] [SerializeField] float _safeWatchingDuration = 2f;
    [Range(0.05f, 1f)] [SerializeField] float _requiredLeanChange = 0.35f;

    class AttackProgress
    {
        public float startingLean;
        public bool leaned;
    }

    readonly Dictionary<BadParticle, AttackProgress> _attacks = new Dictionary<BadParticle, AttackProgress>();
    bool _sawEyeContact;
    bool _dodgeIntroduced;
    bool _healingIntroduced;
    float _lookAwayTime;

    bool CanTeach => _onboarding != null && _onboarding.isActiveAndEnabled
        && _teacher != null && _teacher.isActiveAndEnabled
        && _gaze != null && _gaze.isActiveAndEnabled
        && _lean != null && _lean.isActiveAndEnabled
        && (_loseState == null || !_loseState.IsOver)
        && (_clock == null || _clock.stage < 4)
        && !SkillCheckHandler.SkillCheckRunning;

    void Awake()
    {
        if (_onboarding == null || _teacher == null || _gaze == null || _lean == null)
        {
            Debug.LogError("ClassroomOnboarding needs its controller, teacher, gaze, and lean references.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        BadParticle.AttackStarted += OnAttackStarted;
        BadParticle.AttackFinished += OnAttackFinished;
        SkillCheckHandler.OnHit += OnSkillCheckHit;
        if (_book != null)
            _book.LetterCaught += OnLetterCaught;
        if (_healingManager != null)
            _healingManager.ParticleSpawned += OnHealingParticleSpawned;
    }

    void OnDisable()
    {
        BadParticle.AttackStarted -= OnAttackStarted;
        BadParticle.AttackFinished -= OnAttackFinished;
        SkillCheckHandler.OnHit -= OnSkillCheckHit;
        if (_book != null)
            _book.LetterCaught -= OnLetterCaught;
        if (_healingManager != null)
            _healingManager.ParticleSpawned -= OnHealingParticleSpawned;
        _attacks.Clear();
        ResetLookAwayAttempt();
        HideLessons();
        if (_onboarding != null)
            _onboarding.SetLessonRelevant(OnboardingLesson.SkillCheck, false);
    }

    void LateUpdate()
    {
        if (_onboarding == null)
            return;

        bool alive = _loseState == null || !_loseState.IsOver;
        _onboarding.SetLessonRelevant(OnboardingLesson.SkillCheck,
            alive && SkillCheckHandler.SkillCheckRunning);

        if (!CanTeach)
        {
            ResetLookAwayAttempt();
            _attacks.Clear();
            HideLessons();
            return;
        }

        if (Time.timeScale <= 0f)
            return;

        bool watching = _teacher.IsWatchingClass;
        _onboarding.SetLessonRelevant(OnboardingLesson.LookAway, watching);
        _onboarding.SetLessonRelevant(OnboardingLesson.Dodge, _dodgeIntroduced && _teacher.IsBackTurned);
        _onboarding.SetLessonRelevant(OnboardingLesson.BookHealing,
            _healingIntroduced && _teacher.IsBackTurned && _attacks.Count == 0
            && _book != null && _book.isActiveAndEnabled);

        if (watching)
        {
            if (_gaze.IsLookingAtTeacher)
            {
                _sawEyeContact = true;
                _lookAwayTime = 0f;
            }
            else if (_onboarding.IsLessonVisible(OnboardingLesson.LookAway))
            {
                _lookAwayTime += Time.deltaTime;
                float requiredTime = _sawEyeContact ? _lookAwayDuration : _safeWatchingDuration;
                if (_lookAwayTime >= requiredTime)
                    _onboarding.CompleteLesson(OnboardingLesson.LookAway);
            }
        }
        else
        {
            ResetLookAwayAttempt();
        }

        foreach (AttackProgress progress in _attacks.Values)
            RecordLean(progress);
    }

    void OnAttackStarted(BadParticle particle)
    {
        if (!CanTeach || _gaze.playerCamera == null || particle.playerHead != _gaze.playerCamera.transform)
            return;

        _dodgeIntroduced = true;
        _attacks[particle] = new AttackProgress { startingLean = _lean.LeanAmount };
    }

    void OnAttackFinished(BadParticle particle, BadParticle.AttackOutcome outcome)
    {
        if (!_attacks.TryGetValue(particle, out AttackProgress progress))
            return;

        _attacks.Remove(particle);
        if (!CanTeach)
            return;

        RecordLean(progress);
        if (outcome == BadParticle.AttackOutcome.Dodged && progress.leaned)
            _onboarding.CompleteLesson(OnboardingLesson.Dodge);
    }

    void RecordLean(AttackProgress progress)
    {
        if (Mathf.Abs(_lean.LeanAmount - progress.startingLean) >= _requiredLeanChange)
            progress.leaned = true;
    }

    void OnHealingParticleSpawned(HealingParticle particle)
    {
        _healingIntroduced = true;
    }

    void OnLetterCaught(HealingParticle particle)
    {
        if (CanTeach)
            _onboarding.CompleteLesson(OnboardingLesson.BookHealing);
    }

    void OnSkillCheckHit(int hits)
    {
        if (_onboarding != null && SkillCheckHandler.SkillCheckRunning
            && (_loseState == null || !_loseState.IsOver))
            _onboarding.CompleteLesson(OnboardingLesson.SkillCheck);
    }

    void ResetLookAwayAttempt()
    {
        _sawEyeContact = false;
        _lookAwayTime = 0f;
    }

    void HideLessons()
    {
        if (_onboarding == null)
            return;

        _onboarding.SetLessonRelevant(OnboardingLesson.LookAway, false);
        _onboarding.SetLessonRelevant(OnboardingLesson.Dodge, false);
        _onboarding.SetLessonRelevant(OnboardingLesson.BookHealing, false);
    }
}
