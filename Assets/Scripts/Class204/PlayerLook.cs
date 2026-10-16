using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform cameraTransform;

    public float pitchLimit = 90f;
    public float yawLimit = 70f;     

    float xRotation = 0f;
    float yRotation = 0f;            

    Quaternion startRotation;   

    void Start()
    {
        startRotation = transform.localRotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -pitchLimit, pitchLimit);

        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -yawLimit, yawLimit);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.localRotation = startRotation * Quaternion.Euler(0f, yRotation, 0f);
    }
}