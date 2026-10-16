using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BookState {
    Stowed,
    Drawing,
    Held,
    Closing,
    Cooldown,
    Stowing
}

public class BookController : MonoBehaviour {
    public InputActionReference interactAction;
    public InputActionReference attackAction;
    public ParanoiaMeter paranoia;
    public BookCatchZone catchZone;
    public Transform catchTarget;
    public MonoBehaviour presenterComponent;
    [Min(0f)] public float cooldown = 0.6f;
    [Range(0f, 1f)] public float graceWindow = 0.1f;

    public BookState State { get; private set; } = BookState.Stowed;

    public event Action<HealingParticle> LetterCaught;
    public event Action CatchMissed;
    public event Action<BookState> StateChanged;

    private IBookPresenter _presenter;
    private InputAction _holdBookAction;
    private InputAction _closeBookAction;
    private float _cooldownEndsAt;
    private bool _releasedDuringClose;

    private void OnEnable() {
        _presenter = presenterComponent as IBookPresenter;
        if (_presenter == null
            || interactAction == null
            || interactAction.action == null
            || attackAction == null
            || attackAction.action == null
            || paranoia == null
            || catchZone == null
            || catchTarget == null) {
            Debug.LogError("Book controller needs input actions, presenter, catch zone/target, and paranoia.", this);
            enabled = false;
            return;
        }

        _holdBookAction = interactAction.action.Clone();
        _closeBookAction = attackAction.action.Clone();
        _holdBookAction.wantsInitialStateCheck = true;
        _holdBookAction.Enable();
        _closeBookAction.Enable();

        _presenter.Opened += OnOpened;
        _presenter.Closed += OnClosed;
        _presenter.Stowed += OnStowed;

        catchZone.SetTracking(false);
        SetState(BookState.Stowed);
    }

    private void OnDisable() {
        if (State == BookState.Closing) {
            _cooldownEndsAt = Mathf.Max(_cooldownEndsAt, Time.time + Mathf.Max(0f, cooldown));
        }

        if (_presenter != null) {
            _presenter.Opened -= OnOpened;
            _presenter.Closed -= OnClosed;
            _presenter.Stowed -= OnStowed;
            _presenter.Stow();
        }

        _holdBookAction?.Dispose();
        _closeBookAction?.Dispose();
        _holdBookAction = null;
        _closeBookAction = null;

        if (catchZone != null) {
            catchZone.SetTracking(false);
        }

        SetState(BookState.Stowed);
    }

    private void SetState(BookState state) {
        if (State == state) {
            return;
        }

        State = state;
        StateChanged?.Invoke(state);
    }

    private void Stow() {
        SetState(BookState.Stowing);
        _presenter.Stow();
    }

    private void Update() {
        if (_holdBookAction == null || Time.timeScale <= 0f) {
            return;
        }

        bool isHoldingBook = _holdBookAction.IsPressed();
        BookState stateAtFrameStart = State;

        if (State == BookState.Closing) {
            if (!isHoldingBook) {
                _releasedDuringClose = true;
            }

            return;
        }

        if (!isHoldingBook) {
            if (State != BookState.Stowed && State != BookState.Stowing) {
                Stow();
            }

            return;
        }

        if (State == BookState.Stowed || State == BookState.Stowing) {
            // putting it away and pulling it back out shouldn't skip the wait
            if (Time.time < _cooldownEndsAt) {
                return;
            }

            catchZone.SetTracking(true);
            SetState(BookState.Drawing);
            _presenter.Draw();
        }
        else if (State == BookState.Cooldown && Time.time >= _cooldownEndsAt) {
            SetState(BookState.Drawing);
            _presenter.Reopen();
        }

        if (stateAtFrameStart == BookState.Held
            && State == BookState.Held
            && _closeBookAction.WasPressedThisFrame()) {
            TryClose();
        }
    }

    // only catch when the book is ready, even if another script asks it to close
    public bool TryClose() {
        if (!isActiveAndEnabled
            || Time.timeScale <= 0f
            || State != BookState.Held
            || _holdBookAction == null
            || !_holdBookAction.IsPressed()
            || Time.time < _cooldownEndsAt) {
            return false;
        }

        _releasedDuringClose = false;
        SetState(BookState.Closing);

        int caughtCount = 0;
        foreach (HealingParticle particle in catchZone.ResolveCatch(graceWindow)) {
            if (particle == null || !particle.TryCatch(catchTarget, paranoia)) {
                continue;
            }

            caughtCount++;
            LetterCaught?.Invoke(particle);
        }

        if (caughtCount == 0) {
            CatchMissed?.Invoke();
        }

        _presenter.Close();
        return true;
    }

    private void OnOpened() {
        if (State != BookState.Drawing) {
            return;
        }

        if (_holdBookAction.IsPressed()) {
            SetState(BookState.Held);
        }
        else {
            Stow();
        }
    }

    private void OnClosed() {
        if (State != BookState.Closing) {
            return;
        }

        _cooldownEndsAt = Time.time + Mathf.Max(0f, cooldown);
        if (_releasedDuringClose || !_holdBookAction.IsPressed()) {
            Stow();
        }
        else {
            SetState(BookState.Cooldown);
        }
    }

    private void OnStowed() {
        if (State != BookState.Stowing) {
            return;
        }

        catchZone.SetTracking(false);
        SetState(BookState.Stowed);
    }
}
