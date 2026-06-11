using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    [Header("Movement Control")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        // Automatically grab the physics engine component on our player
        rb = GetComponent<Rigidbody2D>();

        // Double check that gravity won't pull our top-down player down off-screen
        rb.gravityScale = 0f;
        rb.freezeRotation = true; // Prevents the player from spinning out of control
    }

    void Update()
    {
        // Read input from WASD or Arrow Keys
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Normalize so diagonal movement isn't accidentally faster
        moveInput = moveInput.normalized;
    }

    void FixedUpdate()
    {
        // Apply smooth physics movement to the Rigidbody
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}