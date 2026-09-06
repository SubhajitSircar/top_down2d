using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Match the camera's rotation exactly so pixel art stays crisp and flat
        transform.rotation = mainCamera.transform.rotation;
    }
}