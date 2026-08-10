using UnityEngine;
using System.Collections;
using TMPro;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Pools")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI Display")]
    public TMP_Text healthText;

    [Header("Post-Mortem Dispersal")]
    public float corpseDecayDuration = 1.5f;

    [Header("Elemental Balancing Multipliers")]
    public float strongMultiplier = 2.5f;
    public float reactiveMultiplier = 1.5f;

    private Animator animator;
    private Rigidbody2D rb;
    private EnemyMovement enemyMovement;
    private SpriteRenderer spriteRenderer;
    private ElementalEnemyIdentity identityComponent;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyMovement = GetComponent<EnemyMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        identityComponent = GetComponent<ElementalEnemyIdentity>();

        UpdateHealthUI();
    }

    public void ProcessElementalHit(ElementType attackElement, int baseDamage, Vector2 hitDirection)
    {
        if (isDead) return;

        // 🛠️ FIX: Intercept Default spell immediately so it doesn't get zeroed out by NonReact
        if (attackElement == ElementType.Default)
        {
            StartCoroutine(JuiceFlashRoutine(Color.white, 0.1f));
            TakeDamage(baseDamage, hitDirection, 3f, 0.05f); // Minor flinch
            return;
        }

        ElementType defenderElement = identityComponent != null ? identityComponent.GetEnemyElement() : ElementType.Default;
        ReactionType outcome = ElementSystem.GetEffectiveness(attackElement, defenderElement);

        int finalCalculatedDamage = baseDamage;
        float knockbackForce = 5f;
        float stunDuration = 0.15f;

        switch (outcome)
        {
            case ReactionType.Strong:
                finalCalculatedDamage = Mathf.RoundToInt(baseDamage * strongMultiplier);
                knockbackForce = 12f;
                stunDuration = 0.4f;
                StartCoroutine(JuiceFlashRoutine(new Color(0.8f, 0.1f, 1f, 1f), 0.2f));
                break;

            case ReactionType.Weak:
                finalCalculatedDamage = 0;
                currentHealth = Mathf.Clamp(currentHealth + 15, 0, maxHealth);
                UpdateHealthUI();

                if (identityComponent != null) identityComponent.EmpowerEnemy();
                StartCoroutine(JuiceFlashRoutine(new Color(0f, 1f, 0.5f, 1f), 0.3f));
                ApplyPhysicsRecoil(hitDirection * 2f, 0.1f);
                return;

            case ReactionType.Amplify:
                knockbackForce = 6f;
                StartCoroutine(JuiceFlashRoutine(Color.yellow, 0.15f));
                break;

            case ReactionType.Reactive:
                finalCalculatedDamage = Mathf.RoundToInt(baseDamage * reactiveMultiplier);
                if (identityComponent != null) identityComponent.ApplyReactiveDebuff();

                StartCoroutine(JuiceFlashRoutine(Color.cyan, 0.2f));
                knockbackForce = 4f;
                break;

            case ReactionType.NonReact:
                // This now ONLY catches actual elements hitting identical elements (Fire vs Fire)
                finalCalculatedDamage = 0;
                StartCoroutine(JuiceFlashRoutine(new Color(0.5f, 0.5f, 0.5f, 0.7f), 0.1f));
                ApplyPhysicsRecoil(hitDirection * 1f, 0.05f);
                return;
        }

        TakeDamage(finalCalculatedDamage, hitDirection, knockbackForce, stunDuration);
    }

    public void TakeDamage(int damageAmount, Vector2 hitDirection, float knockback, float stunTime)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0) Die();
        else ApplyPhysicsRecoil(hitDirection * knockback, stunTime);
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";

            float healthPercent = (float)currentHealth / maxHealth;
            if (healthPercent > 0.5f)
                healthText.color = Color.green;
            else if (healthPercent > 0.25f)
                healthText.color = Color.yellow;
            else
                healthText.color = Color.red;
        }
    }

    private void ApplyPhysicsRecoil(Vector2 forceVector, float stunTime)
    {
        if (isDead || rb == null) return;
        StartCoroutine(PhysicsStunTimeoutRoutine(forceVector, stunTime));
    }

    IEnumerator PhysicsStunTimeoutRoutine(Vector2 forceVector, float stunTime)
    {
        if (enemyMovement != null) enemyMovement.enabled = false;

        rb.velocity = Vector2.zero;
        rb.AddForce(forceVector, ForceMode2D.Impulse);

        yield return new WaitForSeconds(stunTime);

        if (!isDead && enemyMovement != null)
        {
            while (rb.velocity.magnitude > 0.5f && !isDead)
            {
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 10f);
                yield return null;
            }
            rb.velocity = Vector2.zero;
            enemyMovement.enabled = true;
        }
    }

    IEnumerator JuiceFlashRoutine(Color targetColor, float duration)
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = Color.white;
        float elapsed = 0f;

        spriteRenderer.color = targetColor;
        yield return new WaitForSeconds(duration * 0.3f);

        while (elapsed < duration * 0.7f)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(targetColor, originalColor, elapsed / (duration * 0.7f));
            yield return null;
        }
        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (healthText != null) healthText.gameObject.SetActive(false);

        if (enemyMovement != null) enemyMovement.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }

        // 🛠️ THE FIX: Notify StageController that an enemy in this prefab died!
        StageController stage = GetComponentInParent<StageController>();
        if (stage == null)
        {
            stage = FindObjectOfType<StageController>();
        }

        if (stage != null)
        {
            stage.OnEnemyDefeated(gameObject);
        }

        StartCoroutine(DeathPopRoutine());
    }

    IEnumerator DeathPopRoutine()
    {
        Vector3 startScale = transform.localScale;
        Vector3 popScale = startScale * 1.3f;
        float elapsed = 0f;

        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, popScale, elapsed / 0.15f);
            yield return null;
        }

        if (animator != null) animator.SetTrigger("Die");
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, corpseDecayDuration);
    }
}