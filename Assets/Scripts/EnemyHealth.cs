using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Pools")]
    public int maxHealth = 30;
    private int currentHealth;

    [Header("Post-Mortem Dispersal")]
    public float corpseDecayDuration = 3.5f;

    [Header("Juice & Feedback Prefabs")]
    public GameObject hitSparkVfxPrefab; // Drop a fast orange particle system here if you have one!

    private Animator animator;
    private Rigidbody2D rb;
    private EnemyMovement enemyMovement;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyMovement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damageAmount, Vector2 hitDirection)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Trigger the high-impact visual and physical feedback loop
            StartCoroutine(HitFeedbackRoutine(hitDirection));
        }
    }

    IEnumerator HitFeedbackRoutine(Vector2 knockbackDirection)
    {
        // 1. IMPACT FREEZE: Halt animator speed completely to register the smash
        float originalAnimSpeed = animator.speed;
        animator.speed = 0f;

        // 2. VISUAL FLASH: Flash her a crisp bright tint
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = new Color(1f, 1f, 1f, 1f); // Pure solid white profile or bright tint

        // 3. PHYSICAL KNOCKBACK: Apply an immediate impulse punch backward
        if (rb != null)
        {
            // Temporarily break pathfinding influence to handle recoil tracking
            if (enemyMovement != null) enemyMovement.enabled = false;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = Vector2.zero; // Clear ambient pacing speed
            rb.AddForce(knockbackDirection * 8f, ForceMode2D.Impulse);
        }

        // Hold the frozen physics frame state for an intense micro-moment
        yield return new WaitForSeconds(0.06f);

        // 4. PHYSICS RECOVERY: Restore pathfinding and clear velocity lines
        if (!isDead)
        {
            spriteRenderer.color = originalColor;
            animator.speed = originalAnimSpeed;

            if (rb != null && enemyMovement != null)
            {
                rb.velocity = Vector2.zero;
                enemyMovement.enabled = true;
            }
        }
    }

    void Die()
    {
        isDead = true;

        // Send a tracking update check notice to see if this belongs to the initial floor guards
        DungeonGenerator generator = FindObjectOfType<DungeonGenerator>();
        if (generator != null)
        {
            generator.TrackGuardDeath();
        }

        if (enemyMovement != null)
        {
            enemyMovement.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Die");
        }

        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        Destroy(gameObject, corpseDecayDuration);
    }
}