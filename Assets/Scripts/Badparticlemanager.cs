using UnityEngine;
using System.Collections;

public class BadParticleManager : MonoBehaviour
{
    public Transform playerHead;
    public TeacherState teacher;

    public float minAttackDelay = 3f;
    public float maxAttackDelay = 8f;
    public float cooldown = 2f;

    void Start()
    {
        StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            // hold here until the teacher is facing the board
            while (teacher != null && !teacher.isFacingBoard)
                yield return null;

            BadParticle attacker = PickAttacker();
            if (attacker == null) continue;

            attacker.Attack(playerHead);

            while (attacker != null)
                yield return null;

            yield return new WaitForSeconds(cooldown);
        }
    }

    BadParticle PickAttacker()
    {
        BadParticle[] all = FindObjectsByType<BadParticle>(FindObjectsSortMode.None);

        if (all.Length == 0) return null;

        return all[Random.Range(0, all.Length)];
    }
}