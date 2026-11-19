using System;
using System.Collections.Generic;
using UnityEngine;

public class OnboardingController : MonoBehaviour
{
    sealed class LessonSettings
    {
        public readonly OnboardingLesson lesson;
        public readonly string prompt;
        public readonly int priority;
        public readonly float minimumReadTime = 2f;

        public LessonSettings(OnboardingLesson lesson, string prompt, int priority)
        {
            this.lesson = lesson;
            this.prompt = prompt;
            this.priority = priority;
        }
    }

    [SerializeField] OnboardingView _view;
    readonly LessonSettings[] _lessons =
    {
        new LessonSettings(OnboardingLesson.LookAway, "Look away from the teacher.", 30),
        new LessonSettings(OnboardingLesson.Dodge, "Hold A or D to dodge.", 20),
        new LessonSettings(OnboardingLesson.FreeMovement, "Use WASD to move.", 10),
        new LessonSettings(OnboardingLesson.BookHealing, "Hold E to open the book. Left-click to catch a letter.", 15),
        new LessonSettings(OnboardingLesson.SkillCheck, "Press Space when the marker reaches the highlighted zone.", 100),
        new LessonSettings(OnboardingLesson.Duck, "Press Shift to duck.", 9),
        new LessonSettings(OnboardingLesson.Dash, "Press Space to dash.", 8),
        new LessonSettings(OnboardingLesson.Flashlight, "Hold left-click to charge and fire the flashlight beam.", 7)
    };

    readonly HashSet<OnboardingLesson> _relevant = new HashSet<OnboardingLesson>();
    readonly HashSet<OnboardingLesson> _completed = new HashSet<OnboardingLesson>();
    LessonSettings _current;
    float _readTime;

    public event Action<OnboardingLesson> LessonCompleted;

    void Awake()
    {
        if (_view == null)
        {
            Debug.LogError("OnboardingController needs an OnboardingView.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (_current != null && _view.IsShowing(_current.prompt))
            _readTime += Time.deltaTime;

        LessonSettings next = SelectLesson();
        if (next == _current)
            return;

        _current = next;
        _readTime = 0f;

        if (_current == null)
            _view.HidePrompt();
        else
            _view.ShowPrompt(_current.prompt);
    }

    public void SetLessonRelevant(OnboardingLesson lesson, bool relevant)
    {
        if (relevant)
            _relevant.Add(lesson);
        else
            _relevant.Remove(lesson);
    }

    public void CompleteLesson(OnboardingLesson lesson)
    {
        if (_completed.Add(lesson))
            LessonCompleted?.Invoke(lesson);
    }

    public bool IsLessonCompleted(OnboardingLesson lesson)
    {
        return _completed.Contains(lesson);
    }

    public bool IsLessonVisible(OnboardingLesson lesson)
    {
        return isActiveAndEnabled && _current != null && _current.lesson == lesson
            && _view != null && _view.IsShowing(_current.prompt);
    }

    public void ResetLesson(OnboardingLesson lesson)
    {
        _completed.Remove(lesson);
        _relevant.Remove(lesson);
        if (_current != null && _current.lesson == lesson)
            ClearPrompt();
    }

    public void ResetAllLessons()
    {
        _completed.Clear();
        _relevant.Clear();
        ClearPrompt();
    }

    LessonSettings SelectLesson()
    {
        LessonSettings selected = null;
        foreach (LessonSettings settings in _lessons)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.prompt)
                || !_relevant.Contains(settings.lesson))
                continue;

            if (_completed.Contains(settings.lesson)
                && (settings != _current || _readTime >= settings.minimumReadTime))
                continue;

            if (selected == null || settings.priority > selected.priority)
                selected = settings;
        }

        return selected;
    }

    void OnDisable()
    {
        ClearPrompt();
    }

    void ClearPrompt()
    {
        _current = null;
        _readTime = 0f;
        if (_view != null)
            _view.HidePrompt();
    }
}
