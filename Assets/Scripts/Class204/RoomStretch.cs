using UnityEngine;

public class RoomStretch : MonoBehaviour
{
    public Transform[] objectsToStretch;
    public float stretchPerStage = 4f;
    public float stretchSpeed = 0.5f;

    public Transform[] wallsToStretch;
    public Transform[] proportionalObjectsStretch;
    public Transform[] proportionalMoveOnly;

    private float[] baseZ;
    private float targetOffset;

    private float[] wallsBaseZ;
    private float[] wallsBaseScale;
    private float[] wallsBaseLength;

    private float[] propObjectOffset;
    private float[] propObjectBaseScale;
    private float[] moveOnlyOffset;

    private float wallFace;
    private float wallsGoalLength;

    private bool stopStretching = false;
    private bool initialized = false;

    void Start()
    {

        if (wallsToStretch == null || wallsToStretch.Length == 0 || wallsToStretch[0] == null)
        {
            return;
        }

        // Objects that slide back by the full amount
        baseZ = new float[objectsToStretch.Length];
        for (int i = 0; i < objectsToStretch.Length; i++)
        {
            if (objectsToStretch[i] == null)
            {
                Debug.LogWarning($"RoomStretch on '{name}': Objects To Stretch slot {i} is empty.", this);
                continue;
            }
            baseZ[i] = objectsToStretch[i].position.z;
        }

        // Walls
        wallsBaseZ = new float[wallsToStretch.Length];
        wallsBaseScale = new float[wallsToStretch.Length];
        wallsBaseLength = new float[wallsToStretch.Length];
        for (int i = 0; i < wallsToStretch.Length; i++)
        {
            Transform wall = wallsToStretch[i];
            if (wall == null)
            {
                Debug.LogWarning($"RoomStretch on '{name}': Walls To Stretch slot {i} is empty.", this);
                continue;
            }

            wallsBaseZ[i] = wall.position.z;
            wallsBaseScale[i] = wall.localScale.y;

            Renderer r = wall.GetComponent<Renderer>();
            if (r == null) r = wall.GetComponentInChildren<Renderer>();
            if (r == null)
            {
                Debug.LogWarning($"RoomStretch on '{name}': wall '{wall.name}' has no Renderer.", wall);
                wallsBaseLength[i] = 1f;
                continue;
            }
            wallsBaseLength[i] = r.bounds.size.z;
        }

        wallFace = wallsBaseZ[0] - wallsBaseLength[0] * 0.5f;
        wallsGoalLength = wallsBaseLength[0];

        // Objects that keep their relative spot and stretch
        propObjectOffset = new float[proportionalObjectsStretch.Length];
        propObjectBaseScale = new float[proportionalObjectsStretch.Length];
        for (int i = 0; i < proportionalObjectsStretch.Length; i++)
        {
            if (proportionalObjectsStretch[i] == null)
            {
                Debug.LogWarning($"RoomStretch on '{name}': Proportional Objects Stretch slot {i} is empty.", this);
                continue;
            }
            propObjectOffset[i] = (proportionalObjectsStretch[i].position.z - wallFace) / wallsBaseLength[0];
            propObjectBaseScale[i] = proportionalObjectsStretch[i].localScale.y;
        }

        // Objects that keep their relative spot but don't stretch
        moveOnlyOffset = new float[proportionalMoveOnly.Length];
        for (int i = 0; i < proportionalMoveOnly.Length; i++)
        {
            if (proportionalMoveOnly[i] == null)
            {
                Debug.LogWarning($"RoomStretch on '{name}': Proportional Move Only slot {i} is empty.", this);
                continue;
            }
            moveOnlyOffset[i] = (proportionalMoveOnly[i].position.z - wallFace) / wallsBaseLength[0];
        }

        targetOffset = 0f;
        LevelClock.onStageChange += OnStageChange;
        initialized = true;
        Debug.Log($"RoomStretch on '{name}' finished setup.", this);
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
        if (!initialized || stopStretching) return;

        float t = Time.deltaTime * stretchSpeed;

        for (int i = 0; i < wallsToStretch.Length; i++)
        {
            Transform wall = wallsToStretch[i];
            if (wall == null) continue;

            float goalLength = wallsBaseLength[i] + targetOffset;
            float goalScale = wallsBaseScale[i] * (goalLength / wallsBaseLength[i]);
            float goalPos = wallsBaseZ[i] + targetOffset * 0.5f;

            Vector3 s = wall.localScale;
            s.y = Mathf.Lerp(s.y, goalScale, t);
            wall.localScale = s;

            Vector3 p = wall.position;
            p.z = Mathf.Lerp(p.z, goalPos, t);
            wall.position = p;

            if (i == 0) wallsGoalLength = goalLength;
        }

        for (int i = 0; i < objectsToStretch.Length; i++)
        {
            if (objectsToStretch[i] == null) continue;

            Vector3 p = objectsToStretch[i].position;
            p.z = Mathf.Lerp(p.z, baseZ[i] + targetOffset, t);
            objectsToStretch[i].position = p;
        }

        for (int i = 0; i < proportionalObjectsStretch.Length; i++)
        {
            Transform obj = proportionalObjectsStretch[i];
            if (obj == null) continue;

            float goalZ = wallFace + propObjectOffset[i] * wallsGoalLength;
            float goalScale = propObjectBaseScale[i] * (wallsGoalLength / wallsBaseLength[0]);

            Vector3 s = obj.localScale;
            s.y = Mathf.Lerp(s.y, goalScale, t);
            obj.localScale = s;

            Vector3 p = obj.position;
            p.z = Mathf.Lerp(p.z, goalZ, t);
            obj.position = p;
        }

        for (int i = 0; i < proportionalMoveOnly.Length; i++)
        {
            Transform obj = proportionalMoveOnly[i];
            if (obj == null) continue;

            Vector3 p = obj.position;
            p.z = Mathf.Lerp(p.z, wallFace + moveOnlyOffset[i] * wallsGoalLength, t);
            obj.position = p;
        }
    }
}