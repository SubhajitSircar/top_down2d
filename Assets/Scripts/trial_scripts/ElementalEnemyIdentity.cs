using UnityEngine;
using System.Collections;

public class ElementalEnemyIdentity : MonoBehaviour
{
    [Header("Elemental Settings")]
    [SerializeField] private ElementType enemyElement = ElementType.Default;

    [Header("Empowerment (WEAK Interaction)")]
    [SerializeField] private float sizeSwellMultiplier = 1.35f;
    [SerializeField] private float speedBoostMultiplier = 1.5f;
    [SerializeField] private float buffDuration = 5.0f;

    [Header("Debuff (REACTIVE Interaction)")]
    [SerializeField] private float reactiveSlowMultiplier = 0.4f; // Drops to 40% speed
    [SerializeField] private float debuffDuration = 4.0f;

    private EnemyMovement movementComponent;
    private Vector3 originalLocalScale;
    private float originalExploreSpeed;
    private float originalChaseSpeed;

    private Coroutine activeStateRoutine;

    void Awake()
    {
        movementComponent = GetComponent<EnemyMovement>();
        originalLocalScale = transform.localScale;

        if (movementComponent != null)
        {
            originalExploreSpeed = movementComponent.exploreSpeed;
            originalChaseSpeed = movementComponent.chaseSpeed;
        }
    }

    public ElementType GetEnemyElement() => enemyElement;

    public void EmpowerEnemy()
    {
        if (activeStateRoutine != null) StopCoroutine(activeStateRoutine);
        activeStateRoutine = StartCoroutine(TemporaryBuffRoutine());
    }

    public void ApplyReactiveDebuff()
    {
        if (activeStateRoutine != null) StopCoroutine(activeStateRoutine);
        activeStateRoutine = StartCoroutine(TemporaryDebuffRoutine());
    }

    private IEnumerator TemporaryBuffRoutine()
    {
        // Smooth swell up
        float elapsed = 0f;
        Vector3 targetScale = originalLocalScale * sizeSwellMultiplier;

        if (movementComponent != null)
        {
            movementComponent.exploreSpeed = originalExploreSpeed * speedBoostMultiplier;
            movementComponent.chaseSpeed = originalChaseSpeed * speedBoostMultiplier;
        }

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, elapsed / 0.2f);
            yield return null;
        }

        // Wait for buff duration
        yield return new WaitForSeconds(buffDuration);

        // Smooth shrink back down
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalLocalScale, elapsed / 0.5f);
            yield return null;
        }

        if (movementComponent != null)
        {
            movementComponent.exploreSpeed = originalExploreSpeed;
            movementComponent.chaseSpeed = originalChaseSpeed;
        }
    }

    private IEnumerator TemporaryDebuffRoutine()
    {
        if (movementComponent != null)
        {
            movementComponent.exploreSpeed = originalExploreSpeed * reactiveSlowMultiplier;
            movementComponent.chaseSpeed = originalChaseSpeed * reactiveSlowMultiplier;
        }

        yield return new WaitForSeconds(debuffDuration);

        // Restore
        if (movementComponent != null)
        {
            movementComponent.exploreSpeed = originalExploreSpeed;
            movementComponent.chaseSpeed = originalChaseSpeed;
        }
    }
}