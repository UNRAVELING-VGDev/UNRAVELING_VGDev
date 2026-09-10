using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Badparticlemanager : MonoBehaviour
{
    //references
    public BadParticle particlePrefab;
    public Transform playerHead;
    public TeacherState teacher;
    public BoxCollider roomBounds;

    //how many particles 'live'
    public int particleCount = 4;

    //attack timing
    public float minAttackDelay = 3f;
    public float maxAttackDelay = 8f;
    public float cooldown = 2f;

    //particles that currently exist (tracked) 
    private List<BadParticle> particles = new List<BadParticle>();

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
            BadParticle newParticle = Instantiate(particlePrefab,spawnPosition,Quaternion.identity);
            //remember the particle
            newParticle.roomBounds = roomBounds;
            newParticle.playerHead = playerHead;
            particles.Add(newParticle);
        }
        StartCoroutine(AttackLoop());
    }
    BadParticle PickAttacker(){
        if (particles.Count == 0) return null; 
        return particles[Random.Range(0, particles.Count)]; //pick random particle
    }

    IEnumerator AttackLoop(){
        while(true){
            yield return new WaitForSeconds(Random.Range(minAttackDelay, maxAttackDelay));

            //not attacking when teacher looking
            while (teacher != null && !teacher.isFacingBoard){
                yield return null;
            }
            BadParticle attacker = PickAttacker();
            if (attacker == null) {
                continue;
            }
            attacker.Attack(playerHead);
            while (attacker.IsAttacking){
                yield return null;
            }
            yield return new WaitForSeconds(cooldown);
        }
    }
    
}