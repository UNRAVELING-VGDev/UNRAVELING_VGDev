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

    //particles that currently exist (tracked) 
    private List<BadParticle> idleParticles = new List<BadParticle>();
    private List<BadParticle> attackingParticles = new List<BadParticle>();

    public void Start(){
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
        StartCoroutine(AttackLoop());
    }

    public void SetAttackerCount(int count) {
        if (count > particleCount) {
            attackerCount = particleCount;
            return;
        }
        attackerCount = count;
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

    void OnStageChange(int stage) {
        if (stage == 4) {
            cooldown = 0;
            minAttackDelay = 0;
            maxAttackDelay = 0;
            SetAttackerCount(particleCount);
        }
        if (stage == 4) {  
            StopAllCoroutines();  
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


    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    IEnumerator AttackLoop(){
        while(true){
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            //not attacking when teacher looking
            while (teacher != null && teacher.isFacingPlayer){
                yield return null;
            }
            BadParticle attacker = PickAttacker();
            if (attacker == null) {
                continue;
            }
            StartCoroutine(SendAttackers(attackerCount));
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