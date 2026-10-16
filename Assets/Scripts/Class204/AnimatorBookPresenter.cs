using System;
using UnityEngine;

public class AnimatorBookPresenter : MonoBehaviour, IBookPresenter {
    public Animator bookAnimator;
    public Transform bookPose;
    public Renderer[] bookRenderers;
    public Vector3 heldPosition;
    public Vector3 stowedPosition = new Vector3(0f, -2f, 0f);
    [Min(0f)] public float drawDuration = 0.35f;
    [Min(0f)] public float stowDuration = 0.25f;

    public event Action Opened;
    public event Action Closed;
    public event Action Stowed;

    private static readonly int OpenHash = Animator.StringToHash("Base Layer.Open");
    private static readonly int CloseHash = Animator.StringToHash("Base Layer.Close");
    private static readonly int HeldOpenHash = Animator.StringToHash("Base Layer.HeldOpen");
    private static readonly int HeldClosedHash = Animator.StringToHash("Base Layer.HeldClosed");

    private enum Motion {
        None,
        Drawing,
        Opening,
        Closing,
        Stowing
    }

    private Motion _motion;
    private Vector3 _startPosition;
    private float _elapsedTime;
    private int _motionStartedFrame;
    private bool _isReady;

    private void OnEnable() {
        _isReady = bookAnimator != null
            && bookPose != null
            && bookRenderers != null
            && bookRenderers.Length > 0
            && bookAnimator.runtimeAnimatorController != null;

        if (_isReady) {
            _isReady = bookAnimator.HasState(0, OpenHash)
                && bookAnimator.HasState(0, CloseHash)
                && bookAnimator.HasState(0, HeldOpenHash)
                && bookAnimator.HasState(0, HeldClosedHash);
        }

        if (!_isReady) {
            Debug.LogError("Book presenter needs its Animator, four book states, pose, and renderers.", this);
            enabled = false;
            return;
        }

        bookAnimator.applyRootMotion = false;
        bookAnimator.updateMode = AnimatorUpdateMode.Normal;
        bookAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        bookPose.localPosition = stowedPosition;
        bookAnimator.Play(HeldClosedHash, 0, 0f);
        _motion = Motion.None;
        Show(false);
    }

    private void OnDisable() {
        _motion = Motion.None;
        if (bookPose != null) {
            bookPose.localPosition = stowedPosition;
        }

        Show(false);
    }

    private void Show(bool visible) {
        if (bookRenderers == null) {
            return;
        }

        foreach (Renderer renderer in bookRenderers) {
            if (renderer != null) {
                renderer.enabled = visible;
            }
        }
    }

    private void Begin(Motion motion, int stateHash) {
        if (!_isReady || !isActiveAndEnabled) {
            return;
        }

        _motion = motion;
        _motionStartedFrame = Time.frameCount;
        _startPosition = bookPose.localPosition;
        _elapsedTime = 0f;

        if (stateHash != 0) {
            bookAnimator.Play(stateHash, 0, 0f);
        }
    }

    public void Draw() {
        Show(true);
        Begin(Motion.Drawing, OpenHash);
    }

    public void Reopen() {
        Begin(Motion.Opening, OpenHash);
    }

    public void Close() {
        Begin(Motion.Closing, CloseHash);
    }

    public void Stow() {
        Begin(Motion.Stowing, 0);
    }

    private bool Finished(int hash) {
        AnimatorStateInfo animationState = bookAnimator.GetCurrentAnimatorStateInfo(0);
        return Time.frameCount > _motionStartedFrame
            && !bookAnimator.IsInTransition(0)
            && animationState.fullPathHash == hash
            && animationState.normalizedTime >= 1f;
    }

    private void LateUpdate() {
        if (_motion == Motion.None || Time.timeScale <= 0f) {
            return;
        }

        _elapsedTime += Time.deltaTime;

        if (_motion == Motion.Drawing || _motion == Motion.Stowing) {
            float duration = _motion == Motion.Drawing ? drawDuration : stowDuration;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(_elapsedTime / duration);
            bookPose.localPosition = Vector3.Lerp(
                _startPosition,
                _motion == Motion.Drawing ? heldPosition : stowedPosition,
                Mathf.SmoothStep(0f, 1f, progress));
        }

        if (_motion == Motion.Stowing && _elapsedTime >= stowDuration) {
            _motion = Motion.None;
            Show(false);
            Stowed?.Invoke();
        }
        else if ((_motion == Motion.Drawing || _motion == Motion.Opening)
            && _elapsedTime >= (_motion == Motion.Drawing ? drawDuration : 0f)
            && Finished(OpenHash)) {
            _motion = Motion.None;
            bookAnimator.Play(HeldOpenHash, 0, 0f);
            Opened?.Invoke();
        }
        else if (_motion == Motion.Closing && Finished(CloseHash)) {
            _motion = Motion.None;
            bookAnimator.Play(HeldClosedHash, 0, 0f);
            Closed?.Invoke();
        }
    }
}
