using UnityEngine;

public interface IAttackMovement
{
    Vector3 GetPosition(Vector3 start, Vector3 target, float timeElapsed);
    
}