using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AreaStageManager : MonoBehaviour
{
    [Header("Area Configuration")]
    public GameObject[] stagePrefabs;
    public string nextAreaSceneName;

    [Header("References")]
    public GameObject player;

    [Header("Transition Settings")]
    [Tooltip("Drag your transition PREFAB asset from the Project window here.")]
    public GameObject transitionPrefab;
    [Tooltip("Optional: Drag your UI Canvas here. If left empty, it will auto-find the Canvas in the scene.")]
    public Canvas targetCanvas;
    public float transitionDuration = 0.8f;

    private Image transitionImage;
    private Material transitionMaterial;
    private int currentStageIndex = 0;
    private GameObject currentStageInstance;
    private bool isTransitioning = false;

    private void Awake()
    {
        // 1. Spawn the transition prefab directly into the Canvas
        if (transitionPrefab != null)
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }

            if (targetCanvas != null)
            {
                GameObject transitionInstance = Instantiate(transitionPrefab, targetCanvas.transform);

                // Bring to front so it renders above all other UI panels
                transitionInstance.transform.SetAsLastSibling();

                // Force RectTransform to stretch across full screen
                RectTransform rect = transitionInstance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.one;
                    rect.localScale = Vector3.one;
                }

                // Get the Image component from the instantiated prefab
                transitionImage = transitionInstance.GetComponentInChildren<Image>();

                // Ensure CanvasGroup doesn't hide the shader overlay
                CanvasGroup canvasGroup = transitionInstance.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
            else
            {
                Debug.LogError("[AreaStageManager] No Canvas found in scene to spawn transition prefab!");
            }
        }
        else
        {
            Debug.LogError("[AreaStageManager] Transition Prefab slot is empty! Drag your transition prefab into the Inspector.");
        }

        // 2. Instantiate runtime material copy on the Image
        if (transitionImage != null && transitionImage.material != null)
        {
            transitionMaterial = new Material(transitionImage.material);
            transitionImage.material = transitionMaterial;
            transitionMaterial.SetFloat("_Progress", 0f);
        }
        else
        {
            Debug.LogError("[AreaStageManager] Transition Image or Material is missing on prefab!");
        }
    }

    private void Start()
    {
        LoadStage(currentStageIndex);
    }

    public void TriggerNextStageTransition()
    {
        Debug.Log("[AreaStageManager] TriggerNextStageTransition called!");
        if (isTransitioning) return;
        StartCoroutine(StageTransitionRoutine());
    }

    private IEnumerator StageTransitionRoutine()
    {
        isTransitioning = true;

        // Ensure transitionDuration is valid
        if (transitionDuration <= 0.05f)
        {
            Debug.LogWarning("[AreaStageManager] Transition Duration was 0 or too low! Setting default to 0.8s.");
            transitionDuration = 0.8f;
        }

        Debug.Log("[AreaStageManager] Starting Spiral In (0 -> 1)...");
        // 1. Spiral wipes screen black
        yield return StartCoroutine(AnimateTransition(0f, 1f));

        Debug.Log("[AreaStageManager] Screen fully spiraled black. Loading next stage...");
        // 2. Load stage while screen is fully spiraled black
        LoadStage(currentStageIndex + 1);

        yield return new WaitForSecondsRealtime(0.1f);

        Debug.Log("[AreaStageManager] Starting Spiral Out (1 -> 0)...");
        // 3. Spiral opens back up to reveal new stage
        yield return StartCoroutine(AnimateTransition(1f, 0f));

        Debug.Log("[AreaStageManager] Transition Complete!");
        isTransitioning = false;
    }

    private IEnumerator AnimateTransition(float startProgress, float targetProgress)
    {
        if (transitionMaterial == null)
        {
            Debug.LogError("[AreaStageManager] Cannot animate - transitionMaterial is NULL!");
            yield break;
        }

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.unscaledDeltaTime; // Unscaled so it works even if Time.timeScale is 0
            float currentProgress = Mathf.Lerp(startProgress, targetProgress, timer / transitionDuration);

            transitionMaterial.SetFloat("_Progress", currentProgress);
            yield return null;
        }

        transitionMaterial.SetFloat("_Progress", targetProgress);
    }

    private void LoadStage(int stageIndex)
    {
        if (currentStageInstance != null)
        {
            Destroy(currentStageInstance);
        }

        if (stageIndex < stagePrefabs.Length)
        {
            currentStageIndex = stageIndex;
            currentStageInstance = Instantiate(stagePrefabs[currentStageIndex], Vector3.zero, Quaternion.identity);

            StageController stageController = currentStageInstance.GetComponent<StageController>();

            if (stageController != null)
            {
                if (player != null && stageController.playerSpawnPoint != null)
                {
                    player.transform.position = stageController.playerSpawnPoint.position;
                }

                stageController.InitializeStage(this);
            }
        }
        else
        {
            LoadNextAreaScene();
        }
    }

    private void LoadNextAreaScene()
    {
        if (!string.IsNullOrEmpty(nextAreaSceneName))
        {
            SceneManager.LoadScene(nextAreaSceneName);
        }
    }
}