using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

// to call a cutscene use CutsceneManager.Play(index, scene); where index is the index int of clip in CutsceneManager object's cutscenes and where scene is a string of the scene to go after the cutscene ends
// Example: CutsceneManager.Play(0, "Class204");
public class CutsceneManager : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer videoPlayer;
    [SerializeField]
    private VideoClip[] cutscenes;
    [SerializeField]
    private AudioSource audioSource;
    private static int cutsceneIndex = 0;
    private static string nextScene = "MainMenu";

    public static void Play(int index, string sceneAfter)
    {
        cutsceneIndex = index;
        nextScene = sceneAfter;
        SceneManager.LoadScene("Cutscenes");
    }

    private void Start()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.GameTime;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);

        videoPlayer.clip = cutscenes[cutsceneIndex];
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepared;
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp) => EndCutscene();

    public void Skip() => EndCutscene();

    private void EndCutscene()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.prepareCompleted -= OnPrepared;
        SceneManager.LoadScene(nextScene);
    }
}
