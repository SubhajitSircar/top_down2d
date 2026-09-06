using System;
using System.Collections;
using UnityEngine;

public class NewPlayerMovement : MonoBehaviour
{
    // Static event broadcast when the player uses blink dash
    public static event Action<float, float> OnDash;

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

    [Header("Targeting References")]
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

    [HideInInspector] public bool isDashing = false;
    private bool canDash = true;

    public bool CanDash => canDash;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isDashing)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            movement.Normalize();

            if (movement != Vector2.zero)
            {
                animator.enabled = true;
                animator.SetFloat("MoveX", movement.x);
                animator.SetFloat("MoveY", movement.y);

                if (movement.x > 0) spriteRenderer.flipX = false;
                else if (movement.x < 0) spriteRenderer.flipX = true;
            }
            else
            {
                animator.enabled = false;
                if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            }

            animator.SetFloat("Speed", movement.sqrMagnitude);
        }

        if (Input.GetMouseButtonDown(1) && canDash)
        {
            StartCoroutine(Dash());
        }

        if (Input.GetMouseButtonDown(0))
        {
            ProcessCombatFireRequest();
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        Vector2 newPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void ProcessCombatFireRequest()
    {
        UiPanelController uiController = UnityEngine.Object.FindFirstObjectByType<UiPanelController>();

        if (uiController != null && uiController.IsPanelOpen)
        {
            if (Input.mousePosition.x < Screen.width * 0.7f)
            {
                TriggerActiveSpellPrefab();
            }
        }
        else
        {
            TriggerActiveSpellPrefab();
        }
    }

    private void TriggerActiveSpellPrefab()
    {
        NewPlayerCombat combatComponent = GetComponent<NewPlayerCombat>();
        if (combatComponent != null)
        {
            Vector3 spawnPos = spellSpawnPoint != null ? spellSpawnPoint.position : transform.position;

            Vector2 shootDirection = (spawnPos - transform.position).normalized;

            if (shootDirection == Vector2.zero)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                shootDirection = ((Vector2)mousePos - (Vector2)transform.position).normalized;
            }

            combatComponent.InvokePrivateFireMechanism(spawnPos, shootDirection);
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        if (dashIndicator != null)
        {
            Vector2 blinkDirection = (dashIndicator.transform.position - transform.position).normalized;
            Vector2 startPos = rb.position;
            Vector2 targetPos = startPos + blinkDirection * blinkDistance;

            Vector2 playerSize = GetComponent<BoxCollider2D>() != null ? GetComponent<BoxCollider2D>().size : new Vector2(0.5f, 0.5f);

            RaycastHit2D wallCheck = Physics2D.BoxCast(startPos, playerSize, 0f, blinkDirection, blinkDistance, wallLayer);
            RaycastHit2D portalCheck = Physics2D.BoxCast(startPos, playerSize, 0f, blinkDirection, blinkDistance, portalLayer);

            if (wallCheck.collider != null)
            {
                targetPos = wallCheck.point;
                float pushBackPadding = (blinkDirection.y < 0) ? 0.75f : 0.4f;
                targetPos += (Vector2)(wallCheck.normal * pushBackPadding);
            }

            if (Physics2D.OverlapBox(targetPos, playerSize, 0f, wallLayer) != null)
            {
                targetPos.y += 0.2f;
                if (Physics2D.OverlapBox(targetPos, playerSize, 0f, wallLayer) != null)
                {
                    targetPos = startPos;
                }
            }

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

            SpawnSingleGhost(startPos, spriteRenderer.sprite, spriteRenderer.flipX);

            GameObject lightningInstance = null;
            if (lightningVfxPrefab != null)
            {
                lightningInstance = Instantiate(lightningVfxPrefab, startPos, Quaternion.identity);
            }

            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.1f);

            yield return new WaitForSeconds(0.08f);

            Vector2 midWayPoint = Vector2.Lerp(startPos, targetPos, 0.35f);
            SpawnSingleGhost(midWayPoint, spriteRenderer.sprite, spriteRenderer.flipX);

            if (lightningInstance != null)
            {
                lightningInstance.transform.position = targetPos;
            }

            // Broadcast dash event (Multiplier: 3.2, Duration: 0.3s)
            OnDash?.Invoke(3.2f, 0.3f);

            rb.position = targetPos;
            transform.position = targetPos;
            spriteRenderer.color = originalColor;

            Vector2 justBehindArrival = Vector2.Lerp(startPos, targetPos, 0.8f);
            SpawnSingleGhost(justBehindArrival, spriteRenderer.sprite, spriteRenderer.flipX);

            if (hitPortal)
            {
                DungeonGenerator generator = UnityEngine.Object.FindFirstObjectByType<DungeonGenerator>();
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

        cooldownTimer = dashCooldown;
        while (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            yield return null;
        }

        canDash = true;
    }

    public float GetDashCooldownProgress()
    {
        if (dashCooldown <= 0f) return 1f;
        return 1f - (Mathf.Clamp(cooldownTimer, 0f, dashCooldown) / dashCooldown);
    }

    void SpawnSingleGhost(Vector2 position, Sprite currentSprite, bool isFlipped)
    {
        GameObject ghost = new GameObject("BlinkGhostTrail");
        ghost.transform.position = position;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = currentSprite;
        ghostRenderer.flipX = isFlipped;

        ghostRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        ghostRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghostRenderer.color = new Color(0.7f, 0.2f, 1f, 0.4f);

        StartCoroutine(FadeAndDestroyGhost(ghost, ghostRenderer));
    }

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

    public void TriggerHurtFlash()
    {
        if (isInvulnerable || isDashing) return;
        StartCoroutine(DamageFlashRoutine());
    }

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