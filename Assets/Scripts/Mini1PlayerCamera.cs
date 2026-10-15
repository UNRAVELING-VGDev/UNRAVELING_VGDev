using UnityEngine;
using UnityEngine.InputSystem;

public class Mini1PlayerCamera : MonoBehaviour {
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.6f, 0f); //just adjust the y height if needed
    private Transform player;
    private float a;
    private float b;

    void Start() {
        player = GameObject.Find("Player").transform;
        a = player.eulerAngles.y;

        //lock the mouse so the camera can turn freely cuz first person
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        transform.position = player.position + eyeOffset;
    }

    void LateUpdate() {
        Vector2 look = Mouse.current.delta.ReadValue();
        a += look.x;
        b -= look.y;
        //stop at straight up and straight down so the view doesnt flip over
        b = Mathf.Clamp(b, -90f, 90f);
        transform.position = player.position;
        transform.rotation = Quaternion.Euler(b, a, 0f);
    }
}