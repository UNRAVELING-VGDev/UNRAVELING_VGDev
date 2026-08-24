using UnityEngine;
using System.Collections;


public class TeacherState : MonoBehaviour
{
    public float turnSpeed = 50f;
    public bool isFacingBoard = true;

    void Start()
    {
        StartCoroutine(TurnAround());
    }

    IEnumerator TurnAround()
    {
        while (true)
        {
            // Wait 3 seconds before turning
            yield return new WaitForSeconds(Random.Range(3f, 10f));

            // Set the target rotation
            Quaternion targetRotation = Quaternion.Euler(0f, 270f, 0f);

            // Keep turning until the teacher reaches the target
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                yield return null;
            }
            isFacingBoard = false;

            // Wait 3 seconds while facing the board
            yield return new WaitForSeconds(Random.Range(2f, 6f));

            // Set the target back to the original rotation
            targetRotation = Quaternion.Euler(0f, 90f, 0f);

            // Keep turning until the teacher reaches the original rotation
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                yield return null;
            }
            isFacingBoard = true;
        }
    }
}