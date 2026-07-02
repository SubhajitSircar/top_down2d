using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    public Slider healthSlider;

    private Animator animator;
    private PlayerMovement movementScript;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        movementScript = GetComponent<PlayerMovement>();

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
    }

    public void TakeDamageOverTime(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        Debug.Log("Player Health: " + Mathf.CeilToInt(currentHealth));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;

        if (healthSlider != null)
        {
            healthSlider.value = 0;
        }

        Debug.Log("The witch has perished...");

        // 1. Instantly stop the PlayerMovement script so it stops turning off the animator
        if (movementScript != null)
        {
            movementScript.enabled = false;
        }

        // 2. Clear out the static snapshot image your movement script forced onto the sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }

        // 3. Force the animator back on and slam the "Die" trigger down
        if (animator != null)
        {
            animator.enabled = true; // Wakes the animator up from line 41 of your movement script!
            animator.SetTrigger("Die");
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
}