using UnityEngine;

public class DashIndicator : MonoBehaviour
{
    public Transform player;
    public float distanceFromPlayer = 1.5f;

    [Header("Visibility Settings")]
    [Range(0f, 1f)] public float readyOpacity = 1f;
    [Range(0f, 1f)] public float cooldownOpacity = 0.3f;

    [Header("Juice & Color Settings")]
    public Color startCooldownColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color fullyChargedColor = new Color(0.7f, 0.2f, 1f, 1f);
    public float snapPunchSize = 1.3f;

    [Header("Optional Polish References")]
    public ParticleSystem readyParticleBurst;

    private PlayerMovement playerMovement;
    private SpriteRenderer baseSpriteRenderer;
    private SpriteRenderer glowFillRenderer;

    private bool playedReadyEffects = false;

    void Start()
    {
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        baseSpriteRenderer = GetComponent<SpriteRenderer>();
        CreateGlowOverlay();
    }

    void Update()
    {
        if (player == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector2 direction = ((Vector2)mousePos - (Vector2)player.position).normalized;

        // Pushes the arrow closer to the witch's outfit
        float pivotOffset = 0.7f;
        transform.position = player.position + (Vector3)(direction * (distanceFromPlayer - pivotOffset));

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // COOLDOWN JUICE ENGINE
        if (playerMovement != null)
        {
            Color baseColor = baseSpriteRenderer.color;

            if (playerMovement.CanDash)
            {
                baseSpriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, readyOpacity);

                if (glowFillRenderer != null)
                {
                    glowFillRenderer.enabled = true;
                    glowFillRenderer.color = fullyChargedColor;

                    if (!playedReadyEffects)
                    {
                        StartCoroutine(JuicySnapPulse());

                        if (readyParticleBurst != null)
                        {
                            readyParticleBurst.Play();
                        }

                        playedReadyEffects = true;
                    }
                }
            }
            else
            {
                playedReadyEffects = false;
                baseSpriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, cooldownOpacity);

                if (glowFillRenderer != null)
                {
                    glowFillRenderer.enabled = true;

                    float progress = playerMovement.GetDashCooldownProgress();

                    glowFillRenderer.transform.localScale = new Vector3(progress, 1f, 1f);
                    glowFillRenderer.color = Color.Lerp(startCooldownColor, fullyChargedColor, progress);
                }
            }
        }
    }

    System.Collections.IEnumerator JuicySnapPulse()
    {
        float duration = 0.12f;
        float elapsed = 0f;

        glowFillRenderer.transform.localScale = new Vector3(snapPunchSize, snapPunchSize, 1f);
        glowFillRenderer.color = Color.white;

        yield return new WaitForSeconds(0.03f);
        glowFillRenderer.color = fullyChargedColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentScale = Mathf.Lerp(snapPunchSize, 1f, elapsed / duration);

            if (glowFillRenderer != null && playerMovement.CanDash)
            {
                glowFillRenderer.transform.localScale = new Vector3(currentScale, currentScale, 1f);
            }
            yield return null;
        }

        if (glowFillRenderer != null && playerMovement.CanDash)
        {
            glowFillRenderer.transform.localScale = Vector3.one;
        }
    }

    void CreateGlowOverlay()
    {
        GameObject glowObj = new GameObject("GlowFill_Overlay");
        glowObj.transform.SetParent(this.transform);

        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localRotation = Quaternion.identity;
        glowObj.transform.localScale = Vector3.one;

        glowFillRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowFillRenderer.sprite = baseSpriteRenderer.sprite;
        glowFillRenderer.sortingLayerName = baseSpriteRenderer.sortingLayerName;
        glowFillRenderer.sortingOrder = baseSpriteRenderer.sortingOrder + 1;

        glowFillRenderer.color = startCooldownColor;
    }
}