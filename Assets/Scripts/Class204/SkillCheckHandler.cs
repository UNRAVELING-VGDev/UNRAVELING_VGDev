using System;
using UnityEngine;
using UnityEngine.UI;

// One difficulty step of the skill check
[System.Serializable]
public class SkillCheckTier
{
    public string name = "Tier";
    public int hitsRequired = 3;      // successful presses needed to clear this tier
    public float spinnerSpeed = 2f;   // radians per second
    public float zoneSize = 30f;      // zone width in degrees
}

[System.Serializable]
public class SkillCheckSettings
{
    [Header("Difficulty tiers (played in order)")]
    public SkillCheckTier[] tiers = new SkillCheckTier[]
    {
        new SkillCheckTier { name = "Easy",   hitsRequired = 3, spinnerSpeed = 2f,   zoneSize = 30f },
        new SkillCheckTier { name = "Medium", hitsRequired = 3, spinnerSpeed = 3.5f, zoneSize = 22f },
        new SkillCheckTier { name = "Hard",   hitsRequired = 4, spinnerSpeed = 5f,   zoneSize = 14f },
    };

    [Header("Rules")]
    public int maxMisses = 3;         // misses allowed (whole skill check) before losing
}

// Paranoia goes up one level per miss. Effects can be hooked up to this later.
public enum ParanoiaLevel
{
    Calm = 0,      // no misses
    Uneasy = 1,    // 1 miss
    Panicked = 2,  // 2 misses
    Caught = 3,    // 3 misses -> lose
}

public class SkillCheckHandler : MonoBehaviour
{
    // ---- events other scripts can listen to ----
    public static event Action OnSkillCheckStarted;              // skill check just appeared
    public static event Action<int> OnHit;                       // successful press (total hits)
    public static event Action<int> OnMiss;                      // missed press (total misses)
    public static event Action<int> OnTierChanged;               // moved up a difficulty tier (new tier index)
    public static event Action<ParanoiaLevel> OnParanoiaChanged; // paranoia went up
    public static event Action OnSkillCheckFailed;               // too many misses -> lose
    public static event Action OnSkillCheckPassed;               // all tiers cleared -> level clear

    // true while the skill check is on screen (healing particles check this)
    public static bool SkillCheckRunning { get; private set; }

    [SerializeField] private SkillCheckSettings skillCheckSettings;
    [Header("Objects")]
    [SerializeField] protected Vector2 canvasPosition;
    [SerializeField] protected RectTransform backgroundRT;
    [SerializeField] protected RectTransform zoneRT;
    [SerializeField] protected RectTransform spinnerRT;
    [SerializeField] protected float radiusOffSet;
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private float hitLeniency = 3f;   // extra degrees of forgiveness on each side of the zone

    [Header("Player lock")]
    [SerializeField] private PlayerLook look;
    [SerializeField] private LeanController lean;
    [SerializeField] private bool lockLeaning = true;   // also stop leaning during the skill check

    RectTransform rt;
    Material mat;

    float position = 0;
    float currentMin;
    float currentMax;
    bool forward = true;
    Unity.Mathematics.Random rng;

    // current difficulty (taken from the current tier)
    float currentSpeed;
    float currentZoneSize;

    int hits = 0;          // total hits across all tiers
    int misses = 0;        // total misses across all tiers
    int tierIndex = 0;     // which tier we're on
    int tierHits = 0;      // hits within the current tier
    ParanoiaLevel paranoia = ParanoiaLevel.Calm;

    private bool isActive = false;

    // read-only access for other scripts / UI later
    public int Hits => hits;
    public int Misses => misses;
    public int TierIndex => tierIndex;
    public int TierHits => tierHits;
    public ParanoiaLevel Paranoia => paranoia;

    SkillCheckTier CurrentTier => skillCheckSettings.tiers[tierIndex];

    private void OnEnable()
    {
        // start the skill check a few seconds after the teacher finishes walking up
        TeacherState.OnArrivedAtPlayer += StartSkillCheckDelayed;
    }

    private void OnDisable()
    {
        TeacherState.OnArrivedAtPlayer -= StartSkillCheckDelayed;
        CancelInvoke(nameof(StartSkillCheck));
    }

    private void StartSkillCheckDelayed()
    {
        CancelInvoke(nameof(StartSkillCheck));
        Invoke(nameof(StartSkillCheck), startDelay);
    }

    private void Start()
    {
        SkillCheckRunning = false;   // static values can survive a scene reload, so reset it

        if (look == null) look = FindFirstObjectByType<PlayerLook>();
        if (lean == null) lean = FindFirstObjectByType<LeanController>();

        rt = GetComponent<RectTransform>();
        mat = zoneRT.GetComponent<RawImage>().material;

        if (rt == null || backgroundRT == null || zoneRT == null || spinnerRT == null) {
            return;
        }

        rt.anchoredPosition = canvasPosition;
        backgroundRT.anchoredPosition = Vector2.zero;
        zoneRT.anchoredPosition = Vector2.zero;

        int textureID = Shader.PropertyToID("_imageTexture");
        mat.SetTexture(textureID, zoneRT.GetComponent<RawImage>().texture);

        rng = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

        // hide the skill-check visuals until the teacher arrives
        SetVisualsVisible(false);
    }

    // called (after the delay) when the teacher has walked up to the player
    public void StartSkillCheck()
    {
        if (skillCheckSettings.tiers == null || skillCheckSettings.tiers.Length == 0) {
            Debug.LogError("SkillCheckHandler: no difficulty tiers set up!", this);
            return;
        }

        // reset progress
        hits = 0;
        misses = 0;
        tierIndex = 0;
        tierHits = 0;
        paranoia = ParanoiaLevel.Calm;
        forward = true;
        position = 0;

        ApplyTier();
        GenerateSkillZone();
        isActive = true;
        SetVisualsVisible(true);

        // freeze the camera so the player can only focus on the skill check
        if (look != null) look.enabled = false;
        if (lockLeaning && lean != null) lean.enabled = false;

        SkillCheckRunning = true;
        OnSkillCheckStarted?.Invoke();
    }

    // stops the skill check and hides it
    public void StopSkillCheck()
    {
        isActive = false;
        SkillCheckRunning = false;
        SetVisualsVisible(false);
    }

    private void SetVisualsVisible(bool visible)
    {
        if (backgroundRT != null) backgroundRT.gameObject.SetActive(visible);
        if (zoneRT != null) zoneRT.gameObject.SetActive(visible);
        if (spinnerRT != null) spinnerRT.gameObject.SetActive(visible);
    }

    // loads the speed and zone size of the current tier
    private void ApplyTier()
    {
        currentSpeed = CurrentTier.spinnerSpeed;
        currentZoneSize = CurrentTier.zoneSize;
        Debug.Log($"Skill check tier {tierIndex + 1}/{skillCheckSettings.tiers.Length}: {CurrentTier.name}");
    }

    private void Update()
    {
        if (!isActive) return;

        // check the press against the spinner exactly as it's drawn right now
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (IsSpinnerInZone()) {
                RegisterHit();
            } else {
                RegisterMiss();
            }
            if (!isActive) return;   // the press may have ended the skill check
        }

        visualUpdate();
    }

    // true if the spinner is inside the zone (handles zones that wrap past 360)
    private bool IsSpinnerInZone()
    {
        float calcPos = position * Mathf.Rad2Deg;
        float min = currentMin - hitLeniency;
        float max = currentMax + hitLeniency;

        bool inZone = Mathf.DeltaAngle(calcPos, Mathf.Repeat(min, 360f)) <= 0f &&
                      Mathf.DeltaAngle(calcPos, Mathf.Repeat(max, 360f)) >= 0f;

        Debug.Log($"Press at {calcPos:F0}°, zone {currentMin:F0}°-{currentMax:F0}° -> {(inZone ? "HIT" : "MISS")}");
        return inZone;
    }

    // hit: count it, reverse direction, and move up a tier when this one is cleared
    private void RegisterHit()
    {
        hits++;
        tierHits++;
        forward = !forward;
        OnHit?.Invoke(hits);

        if (tierHits >= CurrentTier.hitsRequired) {
            tierIndex++;
            tierHits = 0;

            // cleared the last tier: the player survived the staredown
            if (tierIndex >= skillCheckSettings.tiers.Length) {
                Pass();
                return;
            }

            ApplyTier();
            OnTierChanged?.Invoke(tierIndex);
        }

        GenerateSkillZone();
    }

    // miss: count it, raise paranoia, and lose after too many
    private void RegisterMiss()
    {
        misses++;
        OnMiss?.Invoke(misses);

        paranoia = (ParanoiaLevel)Mathf.Min(misses, (int)ParanoiaLevel.Caught);
        OnParanoiaChanged?.Invoke(paranoia);
        Debug.Log($"Skill check miss {misses}/{skillCheckSettings.maxMisses} - paranoia: {paranoia}");

        if (misses >= skillCheckSettings.maxMisses) {
            Lose();
            return;
        }

        // new zone so the player can't just spam the same spot
        GenerateSkillZone();
    }

    private void Pass()
    {
        StopSkillCheck();
        Debug.Log("Skill check passed - level clear");
        OnSkillCheckPassed?.Invoke();
    }

    private void Lose()
    {
        StopSkillCheck();
        Debug.Log("Skill check failed - lose state");
        OnSkillCheckFailed?.Invoke();
    }

    private void visualUpdate()
    {
        // move first, then draw, so what you see is what gets checked next press
        position += (forward ? 1 : -1) * currentSpeed * Time.deltaTime;
        position = (position + Mathf.PI * 2) % (Mathf.PI * 2);

        float radius = zoneRT.rect.width / 2 + radiusOffSet;
        spinnerRT.anchoredPosition = radius * new Vector2(Mathf.Cos(position), Mathf.Sin(position));
        spinnerRT.rotation = Quaternion.Euler(0, 0, position * Mathf.Rad2Deg);

        int minID = Shader.PropertyToID("_minAngle");
        int maxID = Shader.PropertyToID("_maxAngle");
        mat.SetFloat(minID, currentMin);
        mat.SetFloat(maxID, currentMax);
    }

    // picks a new random zone using the current tier's zone size
    private void GenerateSkillZone()
    {
        currentMin = rng.NextInt(360);
        currentMax = (currentMin + currentZoneSize) % 360;
    }
}