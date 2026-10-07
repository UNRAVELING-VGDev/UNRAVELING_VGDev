using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class BookCatchZone : MonoBehaviour {
    private readonly Dictionary<HealingParticle, HashSet<Collider>> _overlappingColliders =
        new Dictionary<HealingParticle, HashSet<Collider>>();
    private readonly Dictionary<HealingParticle, float> _lastExitTime =
        new Dictionary<HealingParticle, float>();
    private readonly List<HealingParticle> _particlesToRemove = new List<HealingParticle>();

    private BoxCollider _catchBox;
    private bool _isTracking;

    private void Awake() {
        _catchBox = GetComponent<BoxCollider>();
        _catchBox.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        SetTracking(false);
    }

    private void OnEnable() {
        HealingParticle.Unregistered += Forget;
    }

    private void OnDisable() {
        HealingParticle.Unregistered -= Forget;
        SetTracking(false);
    }

    public void SetTracking(bool enabled) {
        _isTracking = enabled;
        if (_catchBox == null) {
            _catchBox = GetComponent<BoxCollider>();
        }

        _catchBox.enabled = enabled;
        if (!enabled) {
            ClearTracking();
        }
    }

    public void ClearTracking() {
        _overlappingColliders.Clear();
        _lastExitTime.Clear();
    }

    private static bool Eligible(HealingParticle particle) {
        return particle != null && particle.isActiveAndEnabled && !particle.IsCaught;
    }

    private void Track(Collider other) {
        if (!_isTracking) {
            return;
        }

        HealingParticle particle = other.GetComponentInParent<HealingParticle>();
        if (!Eligible(particle)) {
            return;
        }

        if (!_overlappingColliders.TryGetValue(particle, out HashSet<Collider> colliders)) {
            colliders = new HashSet<Collider>();
            _overlappingColliders.Add(particle, colliders);
        }

        colliders.Add(other);
        _lastExitTime.Remove(particle);
    }

    private void OnTriggerEnter(Collider other) {
        Track(other);
    }

    private void OnTriggerStay(Collider other) {
        Track(other);
    }

    private void OnTriggerExit(Collider other) {
        if (!_isTracking) {
            return;
        }

        HealingParticle particle = other.GetComponentInParent<HealingParticle>();
        if (particle == null || !_overlappingColliders.TryGetValue(particle, out HashSet<Collider> colliders)) {
            return;
        }

        colliders.Remove(other);
        if (colliders.Count != 0) {
            return;
        }

        _overlappingColliders.Remove(particle);
        if (Eligible(particle) && other.enabled && other.gameObject.activeInHierarchy) {
            _lastExitTime[particle] = Time.time;
        }
    }

    private void Forget(HealingParticle particle) {
        _overlappingColliders.Remove(particle);
        _lastExitTime.Remove(particle);
    }

    // clean up letters or hitboxes that are gone or switched off, plus old exit times
    private void Prune() {
        _particlesToRemove.Clear();
        foreach (KeyValuePair<HealingParticle, HashSet<Collider>> entry in _overlappingColliders) {
            entry.Value.RemoveWhere(collider =>
                collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);

            if (!Eligible(entry.Key) || entry.Value.Count == 0) {
                _particlesToRemove.Add(entry.Key);
            }
        }

        foreach (HealingParticle particle in _particlesToRemove) {
            Forget(particle);
        }

        _particlesToRemove.Clear();
        foreach (KeyValuePair<HealingParticle, float> entry in _lastExitTime) {
            if (!Eligible(entry.Key) || Time.time - entry.Value > 1f) {
                _particlesToRemove.Add(entry.Key);
            }
        }

        foreach (HealingParticle particle in _particlesToRemove) {
            _lastExitTime.Remove(particle);
        }
    }

    private void FixedUpdate() {
        if (_isTracking) {
            Prune();
        }
    }

    public List<HealingParticle> ResolveCatch(float graceWindow = 0.1f) {
        List<HealingParticle> particlesToCatch = new List<HealingParticle>();
        if (!_isTracking) {
            return particlesToCatch;
        }

        Prune();
        foreach (HealingParticle particle in _overlappingColliders.Keys) {
            particlesToCatch.Add(particle);
        }

        foreach (KeyValuePair<HealingParticle, float> entry in _lastExitTime) {
            if (Time.time - entry.Value <= Mathf.Clamp(graceWindow, 0f, 1f)) {
                particlesToCatch.Add(entry.Key);
            }
        }

        // make a separate list so letters disappearing during a catch won't mess this up
        foreach (HealingParticle particle in particlesToCatch) {
            Forget(particle);
        }

        return particlesToCatch;
    }
}
