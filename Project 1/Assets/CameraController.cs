using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 20.0f;      // Speed of camera movement
    public float mouseSensitivity = 200.0f; // Sensitivity for mouse rotation

    private float pitch = 0.0f; // X rotation (up and down)
    private float yaw = 0.0f;   // Y rotation (left and right)

    void Update()
    {
        // Mouse rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Prevent flipping the camera upside down

        // Apply rotation
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        // WASD movement
        float moveX = Input.GetAxis("Horizontal"); // A and D for left/right
        float moveZ = Input.GetAxis("Vertical");   // W and S for forward/backward

        // Move the camera relative to its current orientation
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Vertical movement using Q (down) and E (up)
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
    }
}

