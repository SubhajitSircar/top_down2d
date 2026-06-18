using UnityEngine;

public class EnemyTestSpawner : MonoBehaviour
{
    [Header("Testing Key Bindings")]
    [SerializeField] private KeyCode spawnKey = KeyCode.X;

    [Header("Enemy Prefab Roster")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Tight Sandbox Bounds Control")]
    [SerializeField] private float spawnForwardDistance = 1.2f;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnSingleEnemy();
        }
    }

    private void SpawnSingleEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject selectedPrefab = enemyPrefabs[randomIndex];
        if (selectedPrefab == null) return;

        Vector3 safeSpawnPosition = transform.position + new Vector3(0f, spawnForwardDistance, 0f);
        GameObject newlySpawnedEnemy = Instantiate(selectedPrefab, safeSpawnPosition, Quaternion.identity);

        // Premium touch: Make them pop in dynamically rather than appearing statically
        newlySpawnedEnemy.transform.localScale = Vector3.zero;
        StartCoroutine(SpawnPopRoutine(newlySpawnedEnemy.transform));
    }

    System.Collections.IEnumerator SpawnPopRoutine(Transform target)
    {
        if (target == null) yield break;

        // Assumes target scale is roughly 1. Modify if your slimes are differently scaled.
        Vector3 endScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < 0.2f && target != null)
        {
            elapsed += Time.deltaTime;
            // A simple overshoot easing
            float t = elapsed / 0.2f;
            float outBounce = Mathf.Sin(t * Mathf.PI * 0.5f + 0.2f) * 1.1f;
            target.localScale = endScale * Mathf.Clamp01(outBounce);
            yield return null;
        }
        if (target != null) target.localScale = endScale;
    }
}