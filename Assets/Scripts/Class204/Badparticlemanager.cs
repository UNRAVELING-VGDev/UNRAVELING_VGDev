using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Badparticlemanager : MonoBehaviour
{
    //references
    public BadParticle[] particlePrefabs;
    public Transform playerHead;
    public TeacherState teacher;
    public BoxCollider roomBounds;

    //how many particles 'live'
    public int particleCount = 4;
    // How many attackers may attack at once
    public int attackerCount {get; private set;} = 1;
    // Delay between sending attackers during an individual attack run
    public float minDelayBetweenAttackers = 0.5f;
    public float maxDelayBetweenAttackers = 1.5f;

    //attack timing
    public float minAttackDelay = 3f;
    public float maxAttackDelay = 8f;
    public float cooldown = 2f;

    //staredown barrage (while the teacher walks up)
    public float barrageMinDelay = 0.2f;   // time between shots during the walk-up
    public float barrageMaxDelay = 0.5f;

    //particles that currently exist (tracked)
    private List<BadParticle> idleParticles = new List<BadParticle>();
    private List<BadParticle> attackingParticles = new List<BadParticle>();

    public void Start(){
        if (teacher == null) teacher = FindFirstObjectByType<TeacherState>();

        for (int i = 0; i < particleCount; i++){
            //where does it spawn?!
            Bounds b = roomBounds.bounds;
            Vector3 spawnPosition = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );
            //particle appears poof
            BadParticle prefabToSpawn = particlePrefabs[Random.Range(0, particlePrefabs.Length)];
            BadParticle newParticle = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            //remember the particle
            newParticle.roomBounds = roomBounds;
            newParticle.playerHead = playerHead;
            idleParticles.Add(newParticle);
        }
        LevelClock.onStageChange += OnStageChange;
        TeacherState.OnArrivedAtPlayer += OnTeacherArrived;
        StartCoroutine(AttackLoop());
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
        TeacherState.OnArrivedAtPlayer -= OnTeacherArrived;
    }

    public void SetAttackerCount(int count) {
        if (count > particleCount) {
            attackerCount = particleCount;
            return;
        }
        attackerCount = count;
    }

    // true only while the teacher is facing the board
    bool TeacherBackTurned() {
        return teacher == null || teacher.IsBackTurned;
    }

    BadParticle PickAttacker(){
        if (idleParticles.Count == 0) return null;
        int idx = Random.Range(0, idleParticles.Count);
        BadParticle particle = idleParticles[idx];
        idleParticles.RemoveAt(idx);
        attackingParticles.Add(particle);
        return particle;
    }

    private bool AnyAttacking() {
        return attackingParticles.Any(attacker => attacker.IsAttacking);
    }

    // moves particles that finished their attack back to idle
    void RecycleFinished() {
        for (int i = attackingParticles.Count - 1; i >= 0; i--) {
            if (!attackingParticles[i].IsAttacking) {
                idleParticles.Add(attackingParticles[i]);
                attackingParticles.RemoveAt(i);
            }
        }
    }

    // calls off every attack in progress
    void CancelAllAttacks() {
        foreach (BadParticle p in attackingParticles)
            p.CancelAttack();
        RecycleFinished();
    }

    void OnStageChange(int stage) {
        if (stage == 4) {
            // staredown: stop the normal loop and start the walk-up barrage
            StopAllCoroutines();
            RecycleFinished();
            SetAttackerCount(particleCount);
            StartCoroutine(StaredownBarrage());
            return;
        } else if (stage != 1) {
            if (minAttackDelay != 0) {
                minAttackDelay -= 1;
            }
            if (maxAttackDelay != 0) {
                maxAttackDelay -= 2;
            }
            if (cooldown != 0) {
                cooldown -= 0.5f;
            }
            SetAttackerCount(attackerCount + 1);
        }
    }

    // teacher reached the player: the skill check takes over, so stop everything
    void OnTeacherArrived() {
        StopAllCoroutines();
        CancelAllAttacks();
    }

    // fires particles nonstop while the teacher walks up to the player
    IEnumerator StaredownBarrage() {
        while (teacher == null || teacher.state == TeacherState.State.WalkingToPlayer) {
            RecycleFinished();

            BadParticle attacker = PickAttacker();
            if (attacker != null) {
                attacker.Attack(playerHead);
            }

            yield return new WaitForSeconds(Random.Range(barrageMinDelay, barrageMaxDelay));
        }
    }

    IEnumerator AttackLoop(){
        while(true){
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            //only attack while the teacher's back is turned
            while (!TeacherBackTurned()){
                yield return null;
            }
            if (idleParticles.Count == 0) {
                continue;
            }
            StartCoroutine(SendAttackers(attackerCount));

            // wait one frame so the first attacker has launched
            yield return null;
            while (AnyAttacking()){
                yield return null;
            }
            idleParticles.AddRange(attackingParticles);
            attackingParticles.Clear();
            yield return new WaitForSeconds(cooldown);
        }
    }

    IEnumerator SendAttackers(int n)
    {
        for (int i = 0; i < n; i++)
        {
            // stop sending the moment she starts turning around
            if (!TeacherBackTurned()) yield break;

            BadParticle attacker = PickAttacker();
            if (attacker == null) {
                continue;
            }
            attacker.Attack(playerHead);
            if (n > 1 && i < n - 1)
                yield return new WaitForSeconds(
                    Random.Range(minDelayBetweenAttackers, maxDelayBetweenAttackers)
                );
        }
    }
}