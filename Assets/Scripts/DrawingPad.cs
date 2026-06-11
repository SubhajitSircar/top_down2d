using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawingPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI Drawing Settings")]
    [SerializeField] private Color lineColor = Color.cyan;
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private float minDistanceBetweenPoints = 6f;

    private GameObject currentLineContainer;
    private List<Vector2> currentPoints = new List<Vector2>();
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentPoints.Clear();

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

        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, eventData.pressEventCamera))
        {
            OnPointerUp(eventData);
            return;
        }

        Vector2 mousePos = eventData.position;
        if (currentPoints.Count == 0 || Vector2.Distance(mousePos, eventData.position - eventData.delta) > minDistanceBetweenPoints)
        {
            AddPoint(mousePos, eventData.pressEventCamera);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentLineContainer == null) return;

        // If the drawing line has enough points, update our memory bank!
        if (currentPoints.Count > 3)
        {
            PlayerCombat playerCombat = FindObjectOfType<PlayerCombat>();
            if (playerCombat != null)
            {
                // Silently updates your gun's active muzzle blueprint shape!
                playerCombat.UpdateDrawnSpellPattern(new List<Vector2>(currentPoints));
            }
        }

        // Clear the visual drawing slate immediately so you can sketch a different spell
        Destroy(currentLineContainer, 0.1f);
        currentLineContainer = null;
    }

    private void AddPoint(Vector2 screenPosition, Camera eventCamera)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out localPos);

        currentPoints.Add(localPos);

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

        Vector2 pivotOffset = new Vector2(rectTransform.rect.width * 0.5f, rectTransform.rect.height * 0.5f);
        rect.anchoredPosition = start + pivotOffset;
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}