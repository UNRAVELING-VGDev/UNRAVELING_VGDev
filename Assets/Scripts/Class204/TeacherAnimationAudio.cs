using UnityEngine;

public class TeacherAnimationAudio : MonoBehaviour
{
    public void PlayChalkSound()
    {
        AudioManager.instance.playOneShot("teacher_chalk", transform.position);
    }
}
