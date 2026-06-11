using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    [Header("Dynamic Spell Prefab")]
    [SerializeField] private GameObject dynamicSpellPrefab;

    [Header("Spell Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float spellLifetime = 3f;
    [SerializeField] private float drawingScaleMultiplier = 0.02f;

    [Tooltip("The color of your projectile in the game panel and your UI spellbook preview.")]
    [SerializeField] private Color spellColor = Color.cyan;

    [Header("UI Preview Setup")]
    [SerializeField] private RectTransform spellPreviewDisplay; // Drag your SpellPreviewDisplay here!
    [SerializeField] private float previewLineWidth = 4f;

    private List<Vector2> activeSpellPattern = new List<Vector2>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Default spell baseline
        activeSpellPattern.Add(new Vector2(-10, 0));
        activeSpellPattern.Add(new Vector2(10, 0));
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Only fire if the mouse cursor is inside the 70% gameplay zone on the left
            if (Input.mousePosition.x < Screen.width * 0.7f)
            {
                FireActiveSpell();
            }
        }
    }

    public void UpdateDrawnSpellPattern(List<Vector2> newPattern)
    {
        if (newPattern == null || newPattern.Count < 2) return;

        activeSpellPattern = new List<Vector2>(newPattern);
        Debug.Log("<color=yellow><b>Active spell pattern updated!</b></color>");

        // --- GENERATE THE UI PREVIEW ---
        GenerateUiPreview();
    }

    private void GenerateUiPreview()
    {
        if (spellPreviewDisplay == null) return;

        // 1. Wipe out the old drawing segments inside the info panel box
        foreach (Transform child in spellPreviewDisplay)
        {
            Destroy(child.gameObject);
        }

        // 2. Find the mathematical center of your drawing so it centers perfectly in the box
        Vector2 centerOffset = Vector2.zero;
        foreach (Vector2 point in activeSpellPattern) centerOffset += point;
        centerOffset /= activeSpellPattern.Count;

        // 3. Recreate the drawing lines step-by-step as clean UI shapes inside your info window
        for (int i = 1; i < activeSpellPattern.Count; i++)
        {
            // Align points to the center of the info container
            Vector2 start = activeSpellPattern[i - 1] - centerOffset;
            Vector2 end = activeSpellPattern[i] - centerOffset;

            // Scale down the drawing by 50% so it fits nicely inside your small preview box
            start *= 0.5f;
            end *= 0.5f;

            GameObject segment = new GameObject("PreviewLineSegment", typeof(Image));
            segment.transform.SetParent(spellPreviewDisplay, false);

            Image image = segment.GetComponent<Image>();

            // --- UPDATED: UI PREVIEW MATCHES YOUR CUSTOM COLOR WHEEL NOW ---
            image.color = spellColor;

            RectTransform rect = segment.GetComponent<RectTransform>();
            Vector2 direction = end - start;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Anchor it squarely to the center of your info block
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(distance, previewLineWidth);
            rect.anchoredPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void FireActiveSpell()
    {
        if (dynamicSpellPrefab == null || activeSpellPattern.Count < 2) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 shootDirection = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        GameObject newSpell = Instantiate(dynamicSpellPrefab, transform.position, Quaternion.identity);
        Destroy(newSpell, spellLifetime);

        LineRenderer line = newSpell.AddComponent<LineRenderer>();
        line.startWidth = 0.15f;
        line.endWidth = 0.15f;
        line.useWorldSpace = false;
        line.material = new Material(Shader.Find("Sprites/Default"));

        // --- UPDATED: PROJECTILE MATCHES YOUR CUSTOM COLOR WHEEL NOW ---
        line.startColor = spellColor;
        line.endColor = Color.white;

        Vector2[] colliderPoints = new Vector2[activeSpellPattern.Count];
        line.positionCount = activeSpellPattern.Count;

        Vector2 centerOffset = Vector2.zero;
        foreach (Vector2 point in activeSpellPattern) centerOffset += point;
        centerOffset /= activeSpellPattern.Count;

        for (int i = 0; i < activeSpellPattern.Count; i++)
        {
            Vector2 localAdjustedPoint = (activeSpellPattern[i] - centerOffset) * drawingScaleMultiplier;
            colliderPoints[i] = localAdjustedPoint;
            line.SetPosition(i, new Vector3(localAdjustedPoint.x, localAdjustedPoint.y, 0f));
        }

        EdgeCollider2D edgeCollider = newSpell.GetComponent<EdgeCollider2D>();
        if (edgeCollider != null)
        {
            edgeCollider.points = colliderPoints;
            edgeCollider.isTrigger = true;
        }

        Rigidbody2D rb = newSpell.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDirection * projectileSpeed;
            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            newSpell.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }
}