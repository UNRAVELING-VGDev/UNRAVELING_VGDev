using UnityEngine;


//Object component that plays heartbeat sounds, scaled by paranoia
public class HeartbeatPlayer : MonoBehaviour
{
    //max heartrate should never be below 1.0f
    [SerializeField]
    private float maxHeartrateMultiplier = 2.0f;
    [SerializeField]
    private AudioSource source;
    private ParanoiaMeter pm;
    private static HeartbeatPlayer _instance;
    private FMOD.Studio.EventInstance heartbeatSound;

    //call HeartbeatPlayer.Instance()._somefunction_ to use any of its functions
    public static HeartbeatPlayer Instance()
    {
        return _instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        } 
        else if (_instance != this)
        {
            Destroy(this);
        }
        pm = FindFirstObjectByType<ParanoiaMeter>();
        if (pm == null)
        {
            Debug.LogWarning("Paranoia Meter not found in scene, destroying heartbeat player");
            Destroy(this);
        }
        if (maxHeartrateMultiplier < 1.0f)
        {
            maxHeartrateMultiplier = 1.0f;
        }
        FMOD.Studio.EventInstance heartbeatSound;
        heartbeatSound = FMODUnity.RuntimeManager.CreateInstance("event:/path");

        //comment this out if you want some other way to start heartbeat playing
        PlayHeartbeat();
    }

    void Update()
    {
        //pitch is playback speed, 1.0 is baseline speed
        heartbeatSound.setPitch((pm.Normalized * (maxHeartrateMultiplier - 1.0f)) + 1.0f);
    }

    public void PlayHeartbeat()
    {
        heartbeatSound.start();
        
    }

    public void StopHeartbeat()
    {
        heartbeatSound.release();
    }
}
