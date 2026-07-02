using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Blink (Flash) Settings")]
    public float blinkDistance = 4f;
    public float dashCooldown = 1f;
    public LayerMask wallLayer;
    public LayerMask portalLayer;
    public GameObject lightningVfxPrefab;

    [Header("Idle Sprite")]
    public Sprite idleSprite;

    [Header("Combat & Targeting References")]
    public GameObject spellPrefab;
    public Transform spellSpawnPoint;
    public GameObject dashIndicator;

    [Header("Damage Flash Settings")]
    public Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    private bool isInvulnerable = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private float cooldownTimer = 0f;

    public bool isDashing = false;
    private bool canDash = true;

    public bool CanDash
    {
        get { return canDash; }
    }

    void Awake()
    {
        // Cache internal component references
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Gather and process player inputs if not currently dashing
        if (!isDashing)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            movement.Normalize();

            // Handle directional animations and sprite flipping
            if (movement != Vector2.zero)
            {
                animator.enabled = true;
                animator.SetFloat("MoveX", movement.x);
                animator.SetFloat("MoveY", movement.y);

                if (movement.x > 0)
                    spriteRenderer.flipX = false;
                else if (movement.x < 0)
                    spriteRenderer.flipX = true;
            }
            else
            {
                animator.enabled = false;
                if (idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
            }

            animator.SetFloat("Speed", movement.sqrMagnitude);
        }

        // Mouse inputs for Dashing (Right Click) and Casting Spells (Left Click)
        if (Input.GetMouseButtonDown(1) && canDash)
        {
            StartCoroutine(Dash());
        }

        if (Input.GetMouseButtonDown(0))
        {
            CastSpell();
        }
    }

    void FixedUpdate()
    {
        // Block standard physics movement while processing a dash
        if (isDashing)
            return;

        // Apply constant velocity movement
        Vector2 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void CastSpell()
    {
        if (dashIndicator == null || spellPrefab == null || spellSpawnPoint == null) return;

        // Calculate fire direction relative to the targeting indicator placement
        Vector2 direction = (dashIndicator.transform.position - transform.position).normalized;

        // Instantiate projectile at the child spawn point anchor
        GameObject spell = Instantiate(spellPrefab, spellSpawnPoint.position, Quaternion.identity);

        // Assign trajectory data to the bullet instance
        SpellProjectile projectile = spell.GetComponent<SpellProjectile>();
        if (projectile != null) projectile.Initialize(direction);
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        if (dashIndicator != null)
        {
            // Set up directional vectors and default destination position
            Vector2 blinkDirection = (dashIndicator.transform.position - transform.position).normalized;
            Vector2 startPos = rb.position;
            Vector2 targetPos = startPos + blinkDirection * blinkDistance;

            // Fetch physical collider dimension parameters to check for narrow paths
            Vector2 playerSize = GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : new Vector2(0.5f, 0.5f);

            // Sweep checks: Cast footprints ahead along the trajectory line to detect obstacles
            RaycastHit2D wallCheck = Physics2D.BoxCast(startPos, playerSize, 0f, blinkDirection, blinkDistance, wallLayer);
            RaycastHit2D portalCheck = Physics2D.BoxCast(startPos, playerSize, 0f, blinkDirection, blinkDistance, portalLayer);

            // Wall Collision: Pull back and bounce out along the surface normal face to avoid tile seams
            if (wallCheck.collider != null)
            {
                targetPos = wallCheck.point;
                float pushBackPadding = (blinkDirection.y < 0) ? 0.75f : 0.4f;
                targetPos += (Vector2)(wallCheck.normal * pushBackPadding);
            }

            // Safety Check: If target position still overlaps a wall box, safely nudge it upwards
            if (Physics2D.OverlapBox(targetPos, playerSize, 0f, wallLayer) != null)
            {
                targetPos.y += 0.2f;

                // Absolute Fallback: Abort destination entirely if she remains completely stuck
                if (Physics2D.OverlapBox(targetPos, playerSize, 0f, wallLayer) != null)
                {
                    targetPos = startPos;
                }
            }

            // Portal Check: Catch gateway overlap to prep an immediate scene transition jump
            bool hitPortal = false;
            if (portalCheck.collider != null)
            {
                Door portalDoor = portalCheck.collider.GetComponent<Door>();
                if (portalDoor != null)
                {
                    targetPos = portalCheck.collider.transform.position;
                    hitPortal = true;
                }
            }

            // VFX Step 1: Drop the first visual ghost trace right at the start point
            SpawnSingleGhost(startPos, spriteRenderer.sprite, spriteRenderer.flipX);

            // VFX Step 2: Spawn lightning strike prefab at initial frame position
            GameObject lightningInstance = null;
            if (lightningVfxPrefab != null)
            {
                lightningInstance = Instantiate(lightningVfxPrefab, startPos, Quaternion.identity);
            }

            // VFX Step 3: Lower character opacity momentarily to mimic the teleportation flash
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.1f);

            yield return new WaitForSeconds(0.08f);

            // VFX Step 4: Drop a secondary mid-way trail ghost during transit
            Vector2 midWayPoint = Vector2.Lerp(startPos, targetPos, 0.35f);
            SpawnSingleGhost(midWayPoint, spriteRenderer.sprite, spriteRenderer.flipX);

            // VFX Step 5: Shift lightning prefab location to map out the final target point
            if (lightningInstance != null)
            {
                lightningInstance.transform.position = targetPos;
            }

            // Perform position shift updates across physics body and script transform anchors
            rb.position = targetPos;
            transform.position = targetPos;

            // Restore opaque character rendering parameters
            spriteRenderer.color = originalColor;

            // VFX Step 6: Spawn final trailing visual ghost slightly behind arrival coordinates
            Vector2 justBehindArrival = Vector2.Lerp(startPos, targetPos, 0.8f);
            SpawnSingleGhost(justBehindArrival, spriteRenderer.sprite, spriteRenderer.flipX);

            // Procedural Transition handler: Clear states to prevent level loading input-freezes
            if (hitPortal)
            {
                DungeonGenerator generator = FindObjectOfType<DungeonGenerator>();
                if (generator != null)
                {
                    isDashing = false;
                    spriteRenderer.color = originalColor;
                    generator.NextLevel();
                }
                yield break;
            }
        }

        yield return null;
        isDashing = false;

        // Process cooldown tracking loops
        cooldownTimer = dashCooldown;
        while (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            yield return null;
        }

        canDash = true;
    }

    // Helper method for prospective UI meters to inspect remaining active cooling durations
    public float GetDashCooldownProgress()
    {
        if (dashCooldown <= 0f) return 1f;
        return 1f - (Mathf.Clamp(cooldownTimer, 0f, dashCooldown) / dashCooldown);
    }

    // Instantiates a temporary sprite clone object to compose the purple trail aesthetic
    void SpawnSingleGhost(Vector2 position, Sprite currentSprite, bool isFlipped)
    {
        GameObject ghost = new GameObject("BlinkGhostTrail");
        ghost.transform.position = position;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = currentSprite;
        ghostRenderer.flipX = isFlipped;

        // Ensure trail tracks behind regular character layering models
        ghostRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        ghostRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        ghostRenderer.color = new Color(0.7f, 0.2f, 1f, 0.4f);

        StartCoroutine(FadeAndDestroyGhost(ghost, ghostRenderer));
    }

    // Gradually decreases alpha parameters before destroying the visual trail instantiation completely
    IEnumerator FadeAndDestroyGhost(GameObject ghostObj, SpriteRenderer ghostRenderer)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        Color startColor = ghostRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            if (ghostRenderer != null)
            {
                ghostRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            yield return null;
        }

        Destroy(ghostObj);
    }

    // Public method triggered by external enemies to engage visual damage reactions
    public void TriggerHurtFlash()
    {
        if (isInvulnerable || isDashing) return;
        StartCoroutine(DamageFlashRoutine());
    }

    // Flashes the sprite to a distinct tint color before smoothing back to standard balances
    IEnumerator DamageFlashRoutine()
    {
        isInvulnerable = true;
        Color normalColor = spriteRenderer.color;

        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(0.12f);

        float fadeDuration = 0.15f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(damageFlashColor, normalColor, elapsed / fadeDuration);
            yield return null;
        }

        spriteRenderer.color = normalColor;
        isInvulnerable = false;
    }
}