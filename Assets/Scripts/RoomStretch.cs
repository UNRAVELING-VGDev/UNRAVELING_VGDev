using System.Linq;
using UnityEngine;

public class RoomStretch : MonoBehaviour
{
    public Transform[] objectsToStretch;
    public float stretchPerStage = 4f;
    public float stretchSpeed = 0.5f;

    private float[] baseZ;
    private float targetOffset;

    public Transform[] wallsToStretch;
    private float[] wallsBaseZ;
    private float[] wallsBaseScale;
    private float[] wallsBaseLength;

    public Transform[] proportionalObjectsStretch;
    private float[] propObjectOffset;
    private float[] propObjectBaseScale;

    public Transform[] proportionalMoveOnly;
    private float[] moveOnlyOffset;

    private float wallFace;
    private float wallsGoalLength;

    private bool stopStretching = false;

    void Start()
    {
        baseZ = new float[objectsToStretch.Length];
        for (int i = 0; i < objectsToStretch.Length; i++)
            baseZ[i] = objectsToStretch[i].position.z;

        if (wallsToStretch.Length == 0)
        {
            Debug.LogWarning("RoomStretch: no walls assigned - stretch won't work.", this);
            return;
        }

        wallsBaseZ = new float[wallsToStretch.Length];
        wallsBaseScale = new float[wallsToStretch.Length];
        wallsBaseLength = new float[wallsToStretch.Length];
        for (int i = 0; i < wallsToStretch.Length; i++)
        {
            Transform walls = wallsToStretch[i];
            wallsBaseZ[i] = walls.position.z;
            wallsBaseScale[i] = walls.localScale.y;

            Renderer r = walls.GetComponent<Renderer>();
            if (r == null) r = walls.GetComponentInChildren<Renderer>();
            wallsBaseLength[i] = r.bounds.size.z;
        }

        wallFace = wallsBaseZ[0] - wallsBaseLength[0] * 0.5f;
        wallsGoalLength = wallsBaseLength[0];

        propObjectOffset = new float[proportionalObjectsStretch.Length];
        propObjectBaseScale = new float[proportionalObjectsStretch.Length];
        for (int i = 0; i < proportionalObjectsStretch.Length; i++)
        {
            propObjectOffset[i] = (proportionalObjectsStretch[i].position.z - wallFace) / wallsBaseLength[0];
            propObjectBaseScale[i] = proportionalObjectsStretch[i].localScale.y;
        }

        moveOnlyOffset = new float[proportionalMoveOnly.Length];
        for (int i = 0; i < proportionalMoveOnly.Length; i++)
            moveOnlyOffset[i] = (proportionalMoveOnly[i].position.z - wallFace) / wallsBaseLength[0];

        targetOffset = 0f;
        LevelClock.onStageChange += OnStageChange;
    }

    void OnDisable()
    {
        LevelClock.onStageChange -= OnStageChange;
    }

    void OnStageChange(int stage)
    {
        if (stage == 2) targetOffset = stretchPerStage;
        if (stage == 3) targetOffset = stretchPerStage * 2f;
        if (stage == 4) stopStretching = true;
    }

    void Update()
    {
        if (wallsToStretch.Length == 0) return;
        if (stopStretching) return;

        for (int i = 0; i < wallsToStretch.Length; i++)
        {
            Transform walls = wallsToStretch[i];
            float goalLength = wallsBaseLength[i] + targetOffset;
            float goalScale = wallsBaseScale[i] * (goalLength / wallsBaseLength[i]);
            float goalPos = wallsBaseZ[i] + targetOffset * 0.5f;

            Vector3 s = walls.localScale;
            s.y = Mathf.Lerp(s.y, goalScale, Time.deltaTime * stretchSpeed);
            walls.localScale = s;

            Vector3 p = walls.position;
            p.z = Mathf.Lerp(p.z, goalPos, Time.deltaTime * stretchSpeed);
            walls.position = p;

            if (i == 0) wallsGoalLength = goalLength;
        }

        for (int i = 0; i < objectsToStretch.Length; i++)
        {
            Vector3 p = objectsToStretch[i].position;
            float goalZ = baseZ[i] + targetOffset;
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            objectsToStretch[i].position = p;
        }

        for (int i = 0; i < proportionalObjectsStretch.Length; i++)
        {
            Transform obj = proportionalObjectsStretch[i];
            float goalZ = wallFace + propObjectOffset[i] * wallsGoalLength;
            float goalScale = propObjectBaseScale[i] * (wallsGoalLength / wallsBaseLength[0]);

            Vector3 s = obj.localScale;
            s.y = Mathf.Lerp(s.y, goalScale, Time.deltaTime * stretchSpeed);
            obj.localScale = s;

            Vector3 p = obj.position;
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            obj.position = p;
        }

        for (int i = 0; i < proportionalMoveOnly.Length; i++)
        {
            Transform obj = proportionalMoveOnly[i];
            float goalZ = wallFace + moveOnlyOffset[i] * wallsGoalLength;

            Vector3 p = obj.position;
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            obj.position = p;
        }
    }
}