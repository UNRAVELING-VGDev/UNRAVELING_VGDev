using System.Linq;
using UnityEngine;


// As the level clock advances into stages 2 and 3, the objects in
// `objectsToStretch` move backward, making the room feel unnaturally long
public class RoomStretch : MonoBehaviour
{
    public Transform[] objectsToStretch;  
    public float stretchPerStage = 4f;    
    public float stretchSpeed = 0.5f;      

    private float[] baseZ; 
    private float targetOffset;   

    // vars needed for walls
    public Transform[] wallsToStretch;
    private float[] wallsBaseZ;
    private float[] wallsBaseScale;
    private float[] wallsBaseLength;     

    // vars needed for objects that need to be stretched proportionally
    public Transform[] proportionalObjectsStretch;
    private float[] propObjectOffset;
    private float[] propObjectBaseScale;    
    private float wallFace; 

    void Start()
    {
        // remember where each object started
        baseZ = new float[objectsToStretch.Length];
        for (int i = 0; i < objectsToStretch.Length; i++)
            baseZ[i] = objectsToStretch[i].position.z;
        
        // remember walls start, scaling (which should always be 100 but just in case), and length
        wallsBaseZ = new float[wallsToStretch.Length];
        wallsBaseScale = new float[wallsToStretch.Length];
        wallsBaseLength = new float[wallsToStretch.Length];
        for (int i = 0; i < wallsToStretch.Length; i++) {
            Transform walls = wallsToStretch[i];
            wallsBaseZ[i] = walls.position.z;
            wallsBaseScale[i] = walls.localScale.y;
            Renderer r = walls.GetComponent<Renderer>();
            wallsBaseLength[i] = r.bounds.size.z;     
        }

        // remember prop objects fractional offset relative to room and starting scale
        wallFace = wallsBaseZ[0] - wallsBaseLength[0] * 0.5f;
        propObjectOffset = new float[proportionalObjectsStretch.Length];
        propObjectBaseScale = new float[proportionalObjectsStretch.Length];

        for (int i = 0; i < proportionalObjectsStretch.Length; i++)
        {
            propObjectOffset[i] = (proportionalObjectsStretch[i].position.z - wallFace) / wallsBaseLength[0];
            propObjectBaseScale[i] = proportionalObjectsStretch[i].localScale.y;
        }
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
    }

    void Update()
    {
        for (int i = 0; i < objectsToStretch.Length; i++)
        {
            if (proportionalObjectsStretch.Contains(objectsToStretch[i])) continue; // skip if needs to be stretched proportionally
            Vector3 p = objectsToStretch[i].position;
            float goalZ = baseZ[i] + targetOffset;   // each object's own base + the shared offset
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            objectsToStretch[i].position = p;
        }

        // for stretching wall
        float wallsGoalLength = 0;
        for (int i = 0; i < wallsToStretch.Length; i++) {
            Transform walls = wallsToStretch[i];
            wallsGoalLength = wallsBaseLength[i] + targetOffset;
            float wallsGoalScale = wallsBaseScale[i] * (wallsGoalLength / wallsBaseLength[i]); // how much to scale by   
            float wallsGoalPos = wallsBaseZ[i] + targetOffset * 0.5f; // how much to move since scaling increases 
                                                                      // length on both sides (and we want to keep back wall anchored)

            Vector3 s = walls.localScale;
            s.y = Mathf.Lerp(s.y, wallsGoalScale, Time.deltaTime * stretchSpeed);
            walls.localScale = s;

            Vector3 p_walls = walls.position;
            p_walls.z = Mathf.Lerp(p_walls.z, wallsGoalPos, Time.deltaTime * stretchSpeed);
            walls.position = p_walls;
        }

        // for stretching things that need to be proportional to walls
        for (int i = 0; i < proportionalObjectsStretch.Length; i++) {
            Transform propObj = proportionalObjectsStretch[i];

            float goalZ = wallFace + propObjectOffset[i] * wallsGoalLength; // how much to move
            float goalScale = propObjectBaseScale[i] * (wallsGoalLength / wallsBaseLength[0]); // how much to scale

            Vector3 s = propObj.localScale;
            s.y = Mathf.Lerp(s.y, goalScale, Time.deltaTime * stretchSpeed);
            propObj.localScale = s;

            Vector3 p = propObj.position;
            p.z = Mathf.Lerp(p.z, goalZ, Time.deltaTime * stretchSpeed);
            propObj.position = p;
        }
    }
}