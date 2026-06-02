using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Sprite idleSprite; // Drag your default idle sprite here in the Inspector

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement.Normalize();

        if (movement != Vector2.zero)
        {
            // 1. We are moving! Make sure the animator is running
            animator.enabled = true;

            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);

            // 2. Flip the sprite horizontally based on direction
            if (movement.x > 0)
            {
                spriteRenderer.flipX = false; // Face Right
            }
            else if (movement.x < 0)
            {
                spriteRenderer.flipX = true;  // Face Left
            }
        }
        else
        {
            // 3. We are standing still! Turn off the animator so it stops changing frames
            animator.enabled = false;

            // 4. Force the SpriteRenderer to show your preferred idle pose
            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }

        animator.SetFloat("Speed", movement.sqrMagnitude);
    }

    void FixedUpdate()
    {
        Vector2 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}