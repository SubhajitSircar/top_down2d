using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCombat : MonoBehaviour
{
    [Header("Drag Your 5 Colored Circle Prefabs Here!")]
    [SerializeField] private GameObject defaultProjectilePrefab;
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private GameObject waterProjectilePrefab;
    [SerializeField] private GameObject lightningProjectilePrefab;
    [SerializeField] private GameObject earthProjectilePrefab;
    [SerializeField] private GameObject windProjectilePrefab;

    [Header("Drag Your 5 Shape Sprites For The UI Display!")]
    [SerializeField] private Sprite fireDisplaySprite;
    [SerializeField] private Sprite waterDisplaySprite;
    [SerializeField] private Sprite lightningDisplaySprite;
    [SerializeField] private Sprite earthDisplaySprite;
    [SerializeField] private Sprite windDisplaySprite;

    [Header("Spell Classifier Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float spellLifetime = 3f;
    [Range(0.5f, 0.95f)] [SerializeField] private float mlConfidenceThreshold = 0.65f;

    [Header("UI Toggle Settings")]
    // --- THE NEW SERIALIZED TOGGLE FIELD ---
    [Tooltip("Check this box to show the yellow vector line tracing. Uncheck it to completely hide it!")]
    [SerializeField] private bool showVisualDrawingPreview = true;

    [Header("UI Preview Setup")]
    [SerializeField] private RectTransform spellPreviewDisplay;
    [SerializeField] private float previewLineWidth = 4f;
    [SerializeField] private TextMeshProUGUI infoPanelTitleText;
    [SerializeField] private Image infoPanelSpriteDisplay;

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

        // Clean out any leftover preview segments on reset
        WipeOldPreviewLines();
    }

    public void ProcessCoordinateRecognition(List<Vector2> rawDrawingPoints)
    {
        int strokeCount = 0;
        foreach (var p in rawDrawingPoints)
        {
            if (p.x == -10000f) strokeCount++;
        }

        List<Vector2> cleanPointsForML = new List<Vector2>();
        foreach (Vector2 p in rawDrawingPoints)
        {
            if (p.x != -10000f) cleanPointsForML.Add(p);
        }

        if (cleanPointsForML.Count < 8)
        {
            ResetToDefaultSpell();
            return;
        }

        float totalBendsDegrees = CalculateTotalCurvature(cleanPointsForML);
        string bestMatchElement = GestureRecognizerML.Classify(cleanPointsForML, out float confidenceScore);
        bool structuralStrokeMatch = true;

        switch (bestMatchElement)
        {
            case "Lightning":
                if (strokeCount != 1) structuralStrokeMatch = false;
                if (totalBendsDegrees < 120f) structuralStrokeMatch = false;
                break;

            case "Fire":
                if (strokeCount != 2) structuralStrokeMatch = false;
                if (totalBendsDegrees < 250f) structuralStrokeMatch = false;
                break;

            case "Water":
                if (strokeCount != 2) structuralStrokeMatch = false;
                break;

            case "Earth":
                if (strokeCount < 2) structuralStrokeMatch = false;
                if (totalBendsDegrees < 200f) structuralStrokeMatch = false;
                break;

            case "Wind":
                if (strokeCount != 3) structuralStrokeMatch = false;
                break;
        }

        if (strokeCount == 3 && bestMatchElement == "Earth")
        {
            bestMatchElement = "Wind";
            structuralStrokeMatch = true;
        }

        Debug.Log($"======================================================");
        Debug.Log($"🤖 [ML CLASSIFIER] Match: {bestMatchElement} | Confidence: {confidenceScore * 100f:F1}% | Total Bends: {totalBendsDegrees:F1}° | Strokes: {strokeCount} | Valid: {structuralStrokeMatch}");
        Debug.Log($"======================================================");

        if (confidenceScore >= mlConfidenceThreshold && bestMatchElement != "Unknown" && structuralStrokeMatch)
        {
            ActivateElementState(bestMatchElement, cleanPointsForML);
        }
        else
        {
            Debug.Log("<color=orange><b>Structural Guard:</b></color> Angle validation failed or stroke mismatch. Resetting.");
            ResetToDefaultSpell();
        }
    }

    private float CalculateTotalCurvature(List<Vector2> points)
    {
        if (points.Count < 3) return 0f;

        float totalAngles = 0f;
        int sampleStep = Mathf.Max(1, points.Count / 12);

        List<Vector2> filteredPoints = new List<Vector2>();
        for (int i = 0; i < points.Count; i += sampleStep) filteredPoints.Add(points[i]);
        if (!filteredPoints.Contains(points[points.Count - 1])) filteredPoints.Add(points[points.Count - 1]);

        for (int i = 1; i < filteredPoints.Count - 1; i++)
        {
            Vector2 dir1 = (filteredPoints[i] - filteredPoints[i - 1]).normalized;
            Vector2 dir2 = (filteredPoints[i + 1] - filteredPoints[i]).normalized;

            float dot = Vector2.Dot(dir1, dir2);
            dot = Mathf.Clamp(dot, -1f, 1f);

            float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            if (angle > 15f) totalAngles += angle;
        }
        return totalAngles;
    }

    private void ActivateElementState(string element, List<Vector2> shapeCoordinates)
    {
        WipeOldPreviewLines();

        activeSpellPattern = new List<Vector2>(shapeCoordinates);
        Sprite activeSpriteToDisplay = null;
        string formalSpellName = "";

        switch (element)
        {
            case "Fire":
                currentActivePrefab = fireProjectilePrefab;
                activeUiColor = Color.red;
                activeSpriteToDisplay = fireDisplaySprite;
                formalSpellName = "IGNIS FLASH (FIRE)";
                break;
            case "Water":
                currentActivePrefab = waterProjectilePrefab;
                activeUiColor = Color.blue;
                activeSpriteToDisplay = waterDisplaySprite;
                formalSpellName = "AQUA SURGE (WATER)";
                break;
            case "Lightning":
                currentActivePrefab = lightningProjectilePrefab;
                activeUiColor = Color.yellow;
                activeSpriteToDisplay = lightningDisplaySprite;
                formalSpellName = "VOLT BOLT (LIGHTNING)";
                break;
            case "Earth":
                currentActivePrefab = earthProjectilePrefab;
                activeUiColor = Color.green;
                activeSpriteToDisplay = earthDisplaySprite;
                formalSpellName = "TERRA WALL (EARTH)";
                break;
            case "Wind":
                currentActivePrefab = windProjectilePrefab;
                activeUiColor = Color.gray;
                activeSpriteToDisplay = windDisplaySprite;
                formalSpellName = "ZEPHYR GALE (WIND)";
                break;
        }

        if (infoPanelTitleText != null)
        {
            infoPanelTitleText.text = formalSpellName;
            infoPanelTitleText.color = activeUiColor;
        }

        if (infoPanelSpriteDisplay != null && activeSpriteToDisplay != null)
        {
            infoPanelSpriteDisplay.enabled = true;
            infoPanelSpriteDisplay.sprite = activeSpriteToDisplay;
            infoPanelSpriteDisplay.color = Color.white;
            infoPanelSpriteDisplay.transform.SetAsLastSibling();
        }

        // --- CHECK THE TOGGLE GUARD BEFORE DRAWING ---
        if (showVisualDrawingPreview)
        {
            GenerateUiPreview();
        }
    }

    private void WipeOldPreviewLines()
    {
        if (spellPreviewDisplay == null) return;
        for (int i = spellPreviewDisplay.childCount - 1; i >= 0; i--)
        {
            Destroy(spellPreviewDisplay.GetChild(i).gameObject);
        }
    }

    private void GenerateUiPreview()
    {
        if (spellPreviewDisplay == null) return;

        Vector2 centerOffset = Vector2.zero;
        foreach (Vector2 point in activeSpellPattern) centerOffset += point;
        centerOffset /= activeSpellPattern.Count;

        for (int i = 1; i < activeSpellPattern.Count; i++)
        {
            Vector2 start = activeSpellPattern[i - 1] - centerOffset;
            Vector2 end = activeSpellPattern[i] - centerOffset;

            start *= 0.45f; end *= 0.45f;

            GameObject segment = new GameObject("PreviewSegmentLine", typeof(Image));
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