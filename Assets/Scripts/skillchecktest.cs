using UnityEngine;

public class skillchecktest : MonoBehaviour
{
  [SerializeField]
  private SkillCheckHandler handler;

  string lastEvent = "-";

  private void Awake()
  {
    if (handler == null)
      handler = FindFirstObjectByType<SkillCheckHandler>();
  }

  private void OnEnable()
  {
    SkillCheckHandler.OnSkillCheckStarted += Started;
    SkillCheckHandler.OnHit += Hit;
    SkillCheckHandler.OnMiss += Miss;
    SkillCheckHandler.OnTierChanged += TierChanged;
    SkillCheckHandler.OnParanoiaChanged += ParanoiaChanged;
    SkillCheckHandler.OnSkillCheckPassed += Passed;
    SkillCheckHandler.OnSkillCheckFailed += Failed;
  }

  private void OnDisable()
  {
    SkillCheckHandler.OnSkillCheckStarted -= Started;
    SkillCheckHandler.OnHit -= Hit;
    SkillCheckHandler.OnMiss -= Miss;
    SkillCheckHandler.OnTierChanged -= TierChanged;
    SkillCheckHandler.OnParanoiaChanged -= ParanoiaChanged;
    SkillCheckHandler.OnSkillCheckPassed -= Passed;
    SkillCheckHandler.OnSkillCheckFailed -= Failed;
  }

  private void Update()
  {
    if (handler == null)
      return;

    // start immediately
    if (Input.GetKeyDown(KeyCode.T))
      handler.StartSkillCheck();

    // go through the private delayed path, exactly like TeacherState.OnArrivedAtPlayer would
    if (Input.GetKeyDown(KeyCode.D))
      handler.SendMessage("StartSkillCheckDelayed");
  }

  void Started() => Log("Started");

  void Hit(int total) => Log($"Hit (total {total})");

  void Miss(int total) => Log($"Miss (total {total})");

  void TierChanged(int tier) => Log($"Tier changed -> {tier}");

  void ParanoiaChanged(ParanoiaLevel p) => Log($"Paranoia -> {p}");

  void Passed() => Log("PASSED");

  void Failed() => Log("FAILED");

  void Log(string msg)
  {
    lastEvent = msg;
    Debug.Log($"[SkillCheckTester] {msg}");
  }

  private void OnGUI()
  {
    if (handler == null)
      return;

    GUILayout.BeginArea(new Rect(10, 10, 300, 160), GUI.skin.box);
    GUILayout.Label("T = start now   D = start with delay   Space = press");
    GUILayout.Label($"Running: {SkillCheckHandler.SkillCheckRunning}");
    GUILayout.Label($"Tier: {handler.TierIndex}   Tier hits: {handler.TierHits}");
    GUILayout.Label($"Hits: {handler.Hits}   Misses: {handler.Misses}");
    GUILayout.Label($"Paranoia: {handler.Paranoia}");
    GUILayout.Label($"Last event: {lastEvent}");
    GUILayout.EndArea();
  }
}
