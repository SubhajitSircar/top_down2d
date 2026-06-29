using UnityEngine;

public class UiPanelController : MonoBehaviour
{
    [Header("UI Element Reference")]
    [SerializeField] private CanvasGroup rightContainerCanvasGroup;

    [Header("Input Shortcut")]
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    private bool isPanelCurrentlyVisible = true;

    // --- NEW: Public property so PlayerCombat can read the state ---
    public bool IsPanelOpen => isPanelCurrentlyVisible;

    void Start()
    {
        if (rightContainerCanvasGroup == null)
        {
            rightContainerCanvasGroup = GetComponent<CanvasGroup>();
        }
        ToggleRightContainerPanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleRightContainerPanel();
        }
    }

    public void ToggleRightContainerPanel()
    {
        isPanelCurrentlyVisible = !isPanelCurrentlyVisible;
        UpdatePanelVisibilityState();
    }

    private void UpdatePanelVisibilityState()
    {
        if (rightContainerCanvasGroup == null) return;

        if (isPanelCurrentlyVisible)
        {
            rightContainerCanvasGroup.alpha = 1f;
            rightContainerCanvasGroup.interactable = true;
            rightContainerCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            rightContainerCanvasGroup.alpha = 0f;
            rightContainerCanvasGroup.interactable = false;
            rightContainerCanvasGroup.blocksRaycasts = false;
        }
    }
}