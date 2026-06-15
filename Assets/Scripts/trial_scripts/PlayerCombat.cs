using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    [Header("Drag Your 5 Colored Circle Prefabs Here!")]
    [SerializeField] private GameObject defaultProjectilePrefab; // Your Lime Circle Attack!
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject waterProjectilePrefab;
    [SerializeField] private GameObject lightningProjectilePrefab;
    [SerializeField] private GameObject earthProjectilePrefab;
    [SerializeField] private GameObject windProjectilePrefab;

    [Header("Drag Your Slices Directly Here from the Project Window!")]
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
        ResetToDefaultSpell();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < Screen.width * 0.7f)
        {
            FireActiveSpell();
        }
    }

    private void ResetToDefaultSpell()
    {
        currentActivePrefab = defaultProjectilePrefab;
        activeUiColor = Color.green;
        if (infoPanelTitleText != null)
        {
            infoPanelTitleText.text = "READY TO CAST (DRAW SPELL)";
            infoPanelTitleText.color = Color.white;
        }
    }

    public void ProcessPixelMatching(Texture2D drawnTexture, List<Vector2> rawDrawingPoints)
    {
        string bestMatchElement = "Unknown";
        float highestMatchScore = 0.12f;

        Dictionary<string, Sprite> database = new Dictionary<string, Sprite>()
        {
            { "Fire", fireSprite },
            { "Water", waterSprite },
            { "Lightning", lightningSprite },
            { "Earth", earthSprite },
            { "Wind", windSprite }
        };

        // 🛠️ EXTRACTION: Calculate the geometric aspect ratio of what the user actually drew
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 pt in rawDrawingPoints)
        {
            if (pt.x < minX) minX = pt.x; if (pt.x > maxX) maxX = pt.x;
            if (pt.y < minY) minY = pt.y; if (pt.y > maxY) maxY = pt.y;
        }
        float drawingWidth = maxX - minX;
        float drawingHeight = maxY - minY;
        float drawingAspectRatio = drawingWidth / (drawingHeight > 0f ? drawingHeight : 1f);

        Debug.Log("================ SIGIL COMPARISON RUN ================");
        foreach (var element in database)
        {
            if (element.Value == null) continue;

            float score = ComparePixelsWithSpriteSlice(drawnTexture, element.Value);

            // 🛠️ CALIBRATION HOOK: If the drawing is wide and we are checking Wind, boost its score.
            // If the drawing is wide and we are checking Earth, penalize it since Earth must be a square diamond.
            if (element.Key == "Wind" && drawingAspectRatio > 1.25f)
            {
                score += 0.15f; // Structural boost for horizontal chevron stacking
            }
            else if (element.Key == "Earth" && drawingAspectRatio > 1.25f)
            {
                score -= 0.15f; // Protect against wide shapes matching the square diamond template
            }

            Debug.Log($"📊 Matrix Evaluation -> {element.Key}: {score * 100f:F1}% match accuracy");

            if (score > highestMatchScore)
            {
                highestMatchScore = score;
                bestMatchElement = element.Key;
            }
        }
        Debug.Log("======================================================");

        if (bestMatchElement != "Unknown")
        {
            Debug.Log($"<color=green><b>MATCH DETECTED:</b></color> Loaded {bestMatchElement} elements!");
            ActivateElementState(bestMatchElement, rawDrawingPoints);
        }
        else
        {
            Debug.Log("<color=red><b>Match failed:</b></color> High-precision threshold missed. Falling back to default.");
            ResetToDefaultSpell();
        }
    }

    private float ComparePixelsWithSpriteSlice(Texture2D drawn, Sprite targetSprite)
    {
        Texture2D sheetTex = targetSprite.texture;
        Rect spriteRect = targetSprite.rect;

        int referenceLinePixels = 0;
        int positiveMatches = 0;
        int negativeMismatches = 0;

        // 🛠️ FIX: Calculate aspect ratio scaling bounds to prevent thin rectangles from stretching out
        float maxSide = Mathf.Max(spriteRect.width, spriteRect.height);
        if (maxSide < 1f) maxSide = 1f;

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                float normX = (float)x / 32f;
                float normY = (float)y / 32f;

                // Map coordinates uniformly to keep the original visual aspect ratio intact
                float localX = (normX - 0.5f) * maxSide + (spriteRect.width * 0.5f);
                float localY = (normY - 0.5f) * maxSide + (spriteRect.height * 0.5f);

                // Clamp to keep lookups safely bounded inside your custom slice box
                float sheetPixelX = spriteRect.x + Mathf.Clamp(localX, 0f, spriteRect.width);
                float sheetPixelY = spriteRect.y + Mathf.Clamp(localY, 0f, spriteRect.height);

                Color refPixel = sheetTex.GetPixelBilinear(sheetPixelX / sheetTex.width, sheetPixelY / sheetTex.height);
                Color drawnPixel = drawn.GetPixel(x, y);

                // Forgiving alpha/color check for clean detection across thin lines
                bool isRefLine = refPixel.a > 0.25f && refPixel.r > 0.35f;

                if (isRefLine)
                {
                    referenceLinePixels++;
                    if (drawnPixel.r > 0.5f) positiveMatches++;
                }
                else
                {
                    // Adjusted mismatch penalty multiplier from 0.4f to 0.2f for better multi-stroke detection
                    if (drawnPixel.r > 0.5f) negativeMismatches++;
                }
            }
        }

        if (referenceLinePixels == 0) return 0f;

        float finalScore = (float)(positiveMatches - (negativeMismatches * 0.2f)) / referenceLinePixels;
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