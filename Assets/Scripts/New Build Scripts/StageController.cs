using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageController : MonoBehaviour
{
    [Header("Spawn Markers")]
    public Transform playerSpawnPoint;

    [Header("Portal Mechanics")]
    public GameObject portalObject;
    public float portalActiveDuration = 15f; // Seconds before portal closes

    private List<GameObject> activeEnemies = new List<GameObject>();
    private AreaStageManager areaManager;
    private Coroutine portalTimerCoroutine;
    private bool portalActive = false;

    private void Awake()
    {
        // Safety: Ensure portal object is hidden as soon as prefab spawns
        if (portalObject != null)
        {
            portalObject.SetActive(false);
        }
    }

    public void InitializeStage(AreaStageManager manager)
    {
        areaManager = manager;

        if (portalObject != null)
            portalObject.SetActive(false);

        RegisterStageEnemies();
    }

    private void RegisterStageEnemies()
    {
        activeEnemies.Clear();

        // Search through all child objects inside THIS specific stage prefab for "Enemy" tags
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.CompareTag("Enemy"))
            {
                // Ensure we don't add duplicate child colliders of the same enemy
                if (!activeEnemies.Contains(child.gameObject))
                {
                    activeEnemies.Add(child.gameObject);
                }
            }
        }

        // Check your Console tab in Unity when the stage loads to verify this number!
        Debug.Log($"[StageController] Registered {activeEnemies.Count} enemies in this stage.");

        if (activeEnemies.Count == 0)
        {
            Debug.LogWarning("[StageController] 0 enemies found! Opening portal automatically.");
            OpenPortal();
        }
    }

    // Call this from your Enemy Health / Death script when an enemy dies
    public void OnEnemyDefeated(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        Debug.Log($"[StageController] Enemy defeated. {activeEnemies.Count} remaining.");

        if (activeEnemies.Count == 0 && !portalActive)
        {
            OpenPortal();
        }
    }

    private void OpenPortal()
    {
        portalActive = true;

        if (portalObject != null)
            portalObject.SetActive(true);

        if (portalTimerCoroutine != null)
            StopCoroutine(portalTimerCoroutine);

        portalTimerCoroutine = StartCoroutine(PortalTimerCountdown());
    }

    private IEnumerator PortalTimerCountdown()
    {
        yield return new WaitForSeconds(portalActiveDuration);

        if (portalActive)
        {
            portalActive = false;

            if (portalObject != null)
                portalObject.SetActive(false);

            TriggerNewEnemyCycle();
        }
    }
    // Add this helper method inside StageController.cs
    public bool IsPortalActive()
    {
        return portalActive;
    }

    private void TriggerNewEnemyCycle()
    {
        Debug.Log("Portal expired! Spawning new enemy cycle...");
    }
}