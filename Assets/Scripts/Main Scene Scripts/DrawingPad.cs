using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawingPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Drawing Setup")]
    [SerializeField] private Color lineColor = Color.cyan;
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private float minDistanceBetweenPoints = 6f;

    [Header("Multi-Stroke Settings")]
    [SerializeField] private float multiStrokeCombineDelay = 1.2f;

    private List<GameObject> visualStrokes = new List<GameObject>();
    private List<List<Vector2>> allStrokesData = new List<List<Vector2>>();
    private List<Vector2> currentActiveStrokePoints = new List<Vector2>();

    private Vector2 lastPointInCurrentStroke;
    private bool isFirstPointOfStroke = true;
    private RectTransform rectTransform;

    private bool isWaitingForMoreStrokes = false;
    private float strokeTimer = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // The Update loop ONLY handles the countdown timer buffer to combine separate strokes
        if (isWaitingForMoreStrokes)
        {
            strokeTimer += Time.deltaTime;
            if (strokeTimer >= multiStrokeCombineDelay)
            {
                ProcessFinalCombinedDrawing();
            }
        }
    }

    // NATIVE UI EVENT: Fires EXACTLY ONE FRAME when left click is pressed down INSIDE the pad boundaries
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isWaitingForMoreStrokes = false;
        isFirstPointOfStroke = true;
        currentActiveStrokePoints = new List<Vector2>();

        // Create a dedicated parent container object for this specific line segment path
        GameObject container = new GameObject("StrokeLineContainer", typeof(RectTransform));
        container.transform.SetParent(transform, false);
        RectTransform r = container.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;

        visualStrokes.Add(container);

        // Record the initial touch point position safely
        RecordPoint(eventData.position);
    }

    // NATIVE UI EVENT: Fires continuously ONLY when mouse is clicked down AND dragging across the screen
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Double check bounds safety lock to stop drawing if they drag outside the drawing panel box
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera))
        {
            return;
        }

        RecordPoint(eventData.position);
    }

    // NATIVE UI EVENT: Fires EXACTLY ONE FRAME the absolute millisecond your finger lifts off the mouse button
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Instantly save our temporary stroke into the master array catalog data tree
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

    private void RecordPoint(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out Vector2 localPos);

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

    private void ProcessFinalCombinedDrawing()
    {
        isWaitingForMoreStrokes = false;

        List<Vector2> trackingPointsWithMarkers = new List<Vector2>();

        foreach (var strokeList in allStrokesData)
        {
            // Inject structural layout breakers so PlayerCombat reads stroke intervals cleanly
            trackingPointsWithMarkers.Add(new Vector2(-10000f, -10000f));
            trackingPointsWithMarkers.AddRange(strokeList);
        }

        if (trackingPointsWithMarkers.Count > 4)
        {
            PlayerCombat player = FindObjectOfType<PlayerCombat>();
            if (player != null)
            {
                player.ProcessCoordinateRecognition(trackingPointsWithMarkers);
            }
        }

        // Wipe UI GameObjects and references cleanly before starting next stroke
        foreach (GameObject stroke in visualStrokes)
        {
            if (stroke != null) Destroy(stroke);
        }
        visualStrokes.Clear();
        allStrokesData.Clear();
        currentActiveStrokePoints.Clear();
    }

    private void CreateLineSegment(Vector2 start, Vector2 end)
    {
        GameObject segment = new GameObject("SegmentLineUI", typeof(Image));
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