using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    [Header("Drag Your 5 Colored Circle Prefabs Here!")]
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject waterProjectilePrefab;
    [SerializeField] private GameObject lightningProjectilePrefab;
    [SerializeField] private GameObject earthProjectilePrefab;
    [SerializeField] private GameObject windProjectilePrefab;

    [Header("Drag Your 5 Shape Images Here!")]
    [SerializeField] private Sprite fireSprite;
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Sprite lightningSprite;
    [SerializeField] private Sprite earthSprite;
    [SerializeField] private Sprite windSprite;

    [Header("Spell Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float spellLifetime = 3f;

    [Header("UI Preview Setup")]
    [SerializeField] private RectTransform spellPreviewDisplay;
    [SerializeField] private float previewLineWidth = 4f;
    [SerializeField] private Text infoPanelTitleText;

    private GameObject currentActivePrefab;
    private Color activeUiColor = Color.white;
    private List<Vector2> activeSpellPattern = new List<Vector2>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        activeSpellPattern.Add(new Vector2(-10, 0));
        activeSpellPattern.Add(new Vector2(10, 0));
        ActivateElementState("Fire", activeSpellPattern);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < Screen.width * 0.7f) FireActiveSpell();
    }

    public void ProcessPixelMatching(Texture2D drawnTexture, List<Vector2> rawDrawingPoints)
    {
        string bestMatchElement = "Unknown";
        float highestMatchScore = -1000f;

        Dictionary<string, Sprite> database = new Dictionary<string, Sprite>()
        {
            { "Fire", fireSprite },
            { "Water", waterSprite },
            { "Lightning", lightningSprite },
            { "Earth", earthSprite },
            { "Wind", windSprite }
        };

        foreach (var element in database)
        {
            if (element.Value == null) continue;

            float score = ComparePixelsAdvanced(drawnTexture, element.Value.texture);
            Debug.Log($"Sigil Scan: {element.Key} -> Net Accuracy: {score * 100f:F1}%");

            if (score > highestMatchScore)
            {
                highestMatchScore = score;
                bestMatchElement = element.Key;
            }
        }

        if (highestMatchScore > 0.15f && bestMatchElement != "Unknown")
        {
            Debug.Log($"<color=green><b>MATCH DETECTED:</b></color> Loaded {bestMatchElement} elements!");
            ActivateElementState(bestMatchElement, rawDrawingPoints);
        }
        else
        {
            Debug.Log("<color=red><b>Match failed:</b></color> Shapes did not register clear boundaries.");
        }
    }

    private float ComparePixelsAdvanced(Texture2D drawn, Texture2D reference)
    {
        int referenceLinePixels = 0;
        int positiveMatches = 0;
        int negativeMismatches = 0;

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                Color refPixel = reference.GetPixelBilinear(x / 32f, y / 32f);
                Color drawnPixel = drawn.GetPixel(x, y);

                bool isRefLine = (refPixel.r < 0.82f || refPixel.g < 0.6f || refPixel.b < 0.6f);

                if (isRefLine)
                {
                    referenceLinePixels++;
                    if (drawnPixel.r > 0.5f) positiveMatches++;
                }
                else
                {
                    // Penalize score if player drew outside the reference lines!
                    if (drawnPixel.r > 0.5f) negativeMismatches++;
                }
            }
        }

        if (referenceLinePixels == 0) return 0f;

        float finalScore = (float)(positiveMatches - (negativeMismatches * 0.4f)) / referenceLinePixels;
        return finalScore;
    }

    private void ActivateElementState(string element, List<Vector2> shapeCoordinates)
    {
        activeSpellPattern = new List<Vector2>(shapeCoordinates);

        switch (element)
        {
            case "Fire": currentActivePrefab = fireProjectilePrefab; activeUiColor = Color.red; break;
            case "Water": currentActivePrefab = waterProjectilePrefab; activeUiColor = Color.blue; break;
            case "Lightning": currentActivePrefab = lightningProjectilePrefab; activeUiColor = Color.yellow; break;
            case "Earth": currentActivePrefab = earthProjectilePrefab; activeUiColor = Color.green; break;
            case "Wind": currentActivePrefab = windProjectilePrefab; activeUiColor = Color.gray; break;
        }

        if (infoPanelTitleText != null)
        {
            infoPanelTitleText.text = element.ToUpper() + " SPELL ACTIVATED!";
            infoPanelTitleText.color = activeUiColor;
        }

        GenerateUiPreview();
    }

    private void GenerateUiPreview()
    {
        if (spellPreviewDisplay == null) return;
        foreach (Transform child in spellPreviewDisplay) Destroy(child.gameObject);

        Vector2 centerOffset = Vector2.zero;
        foreach (Vector2 point in activeSpellPattern) centerOffset += point;
        centerOffset /= activeSpellPattern.Count;

        for (int i = 1; i < activeSpellPattern.Count; i++)
        {
            Vector2 start = activeSpellPattern[i - 1] - centerOffset;
            Vector2 end = activeSpellPattern[i] - centerOffset;
            start *= 0.5f; end *= 0.5f;

            GameObject segment = new GameObject("PreviewSegment", typeof(Image));
            segment.transform.SetParent(spellPreviewDisplay, false);

            Image image = segment.GetComponent<Image>();
            image.color = activeUiColor;

            RectTransform rect = segment.GetComponent<RectTransform>();
            Vector2 direction = end - start;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(direction.magnitude, previewLineWidth);
            rect.anchoredPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }

    private void FireActiveSpell()
    {
        if (currentActivePrefab == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 shootDirection = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        GameObject projectile = Instantiate(currentActivePrefab, transform.position, Quaternion.identity);
        Destroy(projectile, spellLifetime);

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = shootDirection * projectileSpeed;
    }
}