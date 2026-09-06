using UnityEngine;

public class JuicyDungeonCamera : MonoBehaviour
{
    public static JuicyDungeonCamera Instance;

    [Header("Target & Base Follow")]
    public Transform target;
    public float smoothTime = 0.15f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("1. Mouse Peeking (Look Ahead into Dark)")]
    [Range(0f, 5f)] public float mousePeekDistance = 2.5f;

    [Header("2. Ambient Magical Floating (Breathing Effect)")]
    public float floatFrequency = 1.2f;
    public float floatAmplitude = 0.12f;

    [Header("3. Screen Shake Settings")]
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.3f;

    private Vector3 currentVelocity;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Base follow position
        Vector3 desiredPosition = target.position + offset;

        // 1. Shift camera toward mouse cursor to peer ahead into the dark
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 mouseDirection = (mouseWorldPos - target.position).normalized;
        float distanceToMouse = Vector3.Distance(target.position, mouseWorldPos);

        // Clamp peek distance so camera doesn't jump too far off-screen
        Vector3 peekOffset = mouseDirection * Mathf.Min(distanceToMouse * 0.3f, mousePeekDistance);
        peekOffset.z = 0f;
        desiredPosition += peekOffset;

        // 2. Add subtle ambient floating motion (sine wave)
        float floatX = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        float floatY = Mathf.Cos(Time.time * floatFrequency * 0.8f) * floatAmplitude;
        desiredPosition += new Vector3(floatX, floatY, 0f);

        // Smooth movement to target position
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime
        );

        // 3. Apply active screen shake
        if (shakeDuration > 0)
        {
            smoothedPosition += (Vector3)Random.insideUnitCircle * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }

        transform.position = smoothedPosition;
    }

    // Call this anywhere using: JuicyDungeonCamera.Instance.TriggerShake(0.15f, 0.3f);
    public void TriggerShake(float duration = 0.2f, float magnitude = 0.3f)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}