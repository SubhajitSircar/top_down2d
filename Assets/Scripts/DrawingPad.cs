using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawingPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI Drawing Settings")]
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private float minDistanceBetweenPoints = 5f;

    private GameObject currentLineContainer;
    private List<Vector2> currentPoints = new List<Vector2>();
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentPoints.Clear();

        // Create an empty GameObject to act as the holder for this drawing stroke
        currentLineContainer = new GameObject("UI_LineStroke", typeof(RectTransform));
        currentLineContainer.transform.SetParent(transform, false);

        RectTransform containerRect = currentLineContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;

        AddPoint(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentLineContainer == null) return;

        // Check if the mouse leaves the blue panel boundaries
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera))
        {
            OnPointerUp(eventData);
            return;
        }

        Vector2 mousePos = eventData.position;

        // Use screenPosition directly for distance calculation to keep tracking accurate to your mouse cursor speed
        if (currentPoints.Count == 0 || Vector2.Distance(mousePos, eventData.position - eventData.delta) > minDistanceBetweenPoints)
        {
            AddPoint(mousePos, eventData.pressEventCamera);
        }
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentLineContainer == null) return;

        Debug.Log($"Finished drawing shape with {currentPoints.Count} points!");

        // Temporarily destroy the stroke after 1.5 seconds so the screen clears up
        Destroy(currentLineContainer, 1.5f);
        currentLineContainer = null;
    }

    private void AddPoint(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 localPos;

        // 1. Get the screen point relative to the UI element
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out localPos);

        // 2. Add the calculated position to our mathematical tracking points list
        currentPoints.Add(localPos);

        // 3. Draw it!
        if (currentPoints.Count > 1)
        {
            CreateUiLineSegment(currentPoints[currentPoints.Count - 2], currentPoints[currentPoints.Count - 1]);
        }
    }

    private void CreateUiLineSegment(Vector2 start, Vector2 end)
    {
        GameObject segment = new GameObject("LineSegment", typeof(Image));
        segment.transform.SetParent(currentLineContainer.transform, false);

        Image image = segment.GetComponent<Image>();
        image.color = lineColor;

        RectTransform rect = segment.GetComponent<RectTransform>();
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.sizeDelta = new Vector2(distance, lineWidth);
        rect.pivot = new Vector2(0f, 0.5f);
        // Subtract half the width and height of the panel to align local coordinates perfectly with the cursor position
        Vector2 pivotOffset = new Vector2(rectTransform.rect.width * 0.5f, rectTransform.rect.height * 0.5f);
        rect.anchoredPosition = start + pivotOffset;
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}