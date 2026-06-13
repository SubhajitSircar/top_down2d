using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawingPad : MonoBehaviour, IPointerDownHandler
{
    [Header("Drawing Setup")]
    [SerializeField] private Color lineColor = Color.cyan;
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private float minDistanceBetweenPoints = 6f;

    [Header("Multi-Stroke Settings")]
    [SerializeField] private float multiStrokeCombineDelay = 1.2f;

    private List<GameObject> visualStrokes = new List<GameObject>();

    // --- NEW ARCHITECTURE: A separate list container for EACH stroke ---
    private List<List<Vector2>> allStrokesData = new List<List<Vector2>>();
    private List<Vector2> currentActiveStrokePoints = new List<Vector2>();

    private Vector2 lastPointInCurrentStroke;
    private bool isFirstPointOfStroke = true;
    private RectTransform rectTransform;

    private bool isWaitingForMoreStrokes = false;
    private float strokeTimer = 0f;
    private bool isCurrentlyDrawingThisStroke = false;

    void Awake() => rectTransform = GetComponent<RectTransform>();

    void Update()
    {
        bool isMousePhysicallyDown = Input.GetMouseButton(0);
        bool isCursorInsidePad = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null);

        // Track points ONLY when the hardware button is pressed down
        if (isMousePhysicallyDown && isCurrentlyDrawingThisStroke)
        {
            if (isCursorInsidePad)
            {
                TrackMouseFrame();
            }
            else
            {
                StopCurrentStroke();
            }
        }

        // Cut tracking the exact frame the mouse button is released
        if (!isMousePhysicallyDown && isCurrentlyDrawingThisStroke)
        {
            StopCurrentStroke();
        }

        // Countdown delay window to combine multiple strokes
        if (isWaitingForMoreStrokes)
        {
            strokeTimer += Time.deltaTime;
            if (strokeTimer >= multiStrokeCombineDelay)
            {
                ProcessFinalCombinedDrawing();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isWaitingForMoreStrokes = false;
        isCurrentlyDrawingThisStroke = true;
        isFirstPointOfStroke = true;

        // Create a completely clean data list container for this specific line stroke segment
        currentActiveStrokePoints = new List<Vector2>();

        GameObject container = new GameObject("StrokeLine", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        RectTransform r = container.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;

        visualStrokes.Add(container);
        TrackMouseFrame();
    }

    private void TrackMouseFrame()
    {
        if (!Input.GetMouseButton(0)) return;

        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePos, null, out Vector2 localPos);

        if (isFirstPointOfStroke)
        {
            currentActiveStrokePoints.Add(localPos);
            lastPointInCurrentStroke = localPos;
            isFirstPointOfStroke = false;
        }
        else if (Vector2.Distance(localPos, lastPointInCurrentStroke) > minDistanceBetweenPoints)
        {
            currentActiveStrokePoints.Add(localPos);
            if (visualStrokes.Count > 0)
            {
                CreateLineSegment(lastPointInCurrentStroke, localPos);
            }
            lastPointInCurrentStroke = localPos;
        }
    }

    private void StopCurrentStroke()
    {
        if (!isCurrentlyDrawingThisStroke) return;
        isCurrentlyDrawingThisStroke = false;

        // If the stroke we just finished drawing actually has data points, save it to our master catalog!
        if (currentActiveStrokePoints != null && currentActiveStrokePoints.Count > 1)
        {
            allStrokesData.Add(new List<Vector2>(currentActiveStrokePoints));
        }

        if (allStrokesData.Count > 0)
        {
            isWaitingForMoreStrokes = true;
            strokeTimer = 0f;
        }
    }

    private void ProcessFinalCombinedDrawing()
    {
        isWaitingForMoreStrokes = false;
        isCurrentlyDrawingThisStroke = false;

        // Flatten our separated stroke lists into a single sequential list ONLY for the UI preview window
        List<Vector2> flatPointsForPreview = new List<Vector2>();
        foreach (var strokeList in allStrokesData)
        {
            flatPointsForPreview.AddRange(strokeList);
        }

        if (flatPointsForPreview.Count > 4)
        {
            // Bake our matrix texture safely using our completely separated structural lists
            Texture2D drawnMap = BakeStrokesToMatrixClean();

            PlayerCombat player = FindObjectOfType<PlayerCombat>();
            if (player != null)
            {
                player.ProcessPixelMatching(drawnMap, flatPointsForPreview);
            }
        }

        // Clean out our memory spaces completely for the next spellcast
        foreach (GameObject stroke in visualStrokes) Destroy(stroke);
        visualStrokes.Clear();
        allStrokesData.Clear();
        currentActiveStrokePoints.Clear();
    }

    private Texture2D BakeStrokesToMatrixClean()
    {
        Texture2D tex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, Color.clear);

        // Step 1: Find the absolute bounds of ONLY real points across all independent strokes
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var strokeList in allStrokesData)
        {
            foreach (Vector2 p in strokeList)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
            }
        }

        float width = maxX - minX;
        float height = maxY - minY;
        float longestSide = Mathf.Max(width, height);
        if (longestSide < 1f) longestSide = 1f;

        // Step 2: Draw pixels onto the matrix stroke by stroke without ever connecting them!
        foreach (var strokeList in allStrokesData)
        {
            foreach (Vector2 pt in strokeList)
            {
                float normX = ((pt.x - minX) + (longestSide - width) * 0.5f) / longestSide;
                float normY = ((pt.y - minY) + (longestSide - height) * 0.5f) / longestSide;

                int pixelX = Mathf.Clamp((int)(normX * 24) + 4, 0, 31);
                int pixelY = Mathf.Clamp((int)(normY * 24) + 4, 0, 31);

                // Safe 3x3 pixel stamp brush map
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        tex.SetPixel(Mathf.Clamp(pixelX + dx, 0, 31), Mathf.Clamp(pixelY + dy, 0, 31), Color.red);
                    }
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private void CreateLineSegment(Vector2 start, Vector2 end)
    {
        GameObject segment = new GameObject("Segment", typeof(Image));
        segment.transform.SetParent(visualStrokes[visualStrokes.Count - 1].transform, false);
        segment.GetComponent<Image>().color = lineColor;

        RectTransform r = segment.GetComponent<RectTransform>();
        Vector2 dir = end - start;
        r.anchorMin = r.anchorMax = Vector2.zero;
        r.sizeDelta = new Vector2(dir.magnitude, lineWidth);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = start + new Vector2(rectTransform.rect.width * 0.5f, rectTransform.rect.height * 0.5f);
        r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
}