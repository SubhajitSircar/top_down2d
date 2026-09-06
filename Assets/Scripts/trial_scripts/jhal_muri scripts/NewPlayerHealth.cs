using System;
using UnityEngine;
using UnityEngine.UI;

public class NewPlayerHealth : MonoBehaviour
{
    // Static event broadcast when the player takes damage
    public static event Action<float, float> OnPlayerHurt;

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

        // Broadcast hurt event (Multiplier: 1.8, Duration: 0.2s)
        OnPlayerHurt?.Invoke(1.8f, 0.2f);

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

        if (movementScript != null)
        {
            movementScript.enabled = false;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("Die");
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
}