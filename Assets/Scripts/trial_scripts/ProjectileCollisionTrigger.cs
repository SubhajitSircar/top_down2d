using UnityEngine;

public class ProjectileCollisionTrigger : MonoBehaviour
{
    [Header("Layer Collision Mask Setup")]
    [SerializeField] private LayerMask wallLayerMask;

    [Header("Amplify Mechanical Splitting")]
    [SerializeField] private GameObject splinterPrefab;
    [SerializeField] private int splinterCount = 4;

    private bool isASplinter = false;
    private int customSplinterDamage = -1;

    private ElementalProjectile identityScript;
    private Rigidbody2D rb;
    private int wallBounceCount = 0;
    private const int MaxWallBounces = 3;

    void Awake()
    {
        identityScript = GetComponent<ElementalProjectile>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void MarkAsSplinter(int reducedDamage)
    {
        isASplinter = true;
        customSplinterDamage = reducedDamage;
        if (identityScript != null) identityScript.baseDamage = reducedDamage;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                ElementType myElement = identityScript != null ? identityScript.GetElementType() : ElementType.Default;
                int damageToDeal = customSplinterDamage != -1 ? customSplinterDamage : (identityScript != null ? identityScript.baseDamage : 5);

                Vector2 impactAngle = (collision.transform.position - transform.position).normalized;

                ElementalEnemyIdentity enemyID = collision.GetComponent<ElementalEnemyIdentity>();
                ElementType defenderElement = enemyID != null ? enemyID.GetEnemyElement() : ElementType.Default;
                ReactionType outcome = ElementSystem.GetEffectiveness(myElement, defenderElement);

                // AMPLIFY: The parent applies its damage, shatters into splinters, and destroys itself!
                if (outcome == ReactionType.Amplify && !isASplinter)
                {
                    enemyHealth.ProcessElementalHit(myElement, damageToDeal, impactAngle);

                    // Pass the enemy's collider so the splinters know to ignore it
                    TriggerAmplifySplitting(transform.position, impactAngle, damageToDeal, collision);

                    Destroy(gameObject); // The parent shatters completely
                    return;
                }

                // Standard hit processing
                enemyHealth.ProcessElementalHit(myElement, damageToDeal, impactAngle);

                if (outcome != ReactionType.Amplify || isASplinter)
                {
                    Destroy(gameObject);
                }
            }
        }
        else if (((1 << collision.gameObject.layer) & wallLayerMask) != 0)
        {
            if (!isASplinter && identityScript != null)
            {
                wallBounceCount++;
                if (wallBounceCount > MaxWallBounces) Destroy(gameObject);
                else ReflectOffSurface(collision);
            }
            else
            {
                // Splinters pop instantly on walls
                Destroy(gameObject);
            }
        }
    }

    private void TriggerAmplifySplitting(Vector2 impactPoint, Vector2 hitDir, int parentDamage, Collider2D hitEnemyCollider)
    {
        float angleStep = 360f / splinterCount;
        int splinterDamage = Mathf.Max(1, parentDamage / 2); // Splinters deal 50% damage

        for (int i = 0; i < splinterCount; i++)
        {
            // Add a slight rotation offset so it doesn't look like a perfect "+", looks more organic
            float currentAngle = (i * angleStep + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector2 launchDir = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));

            GameObject prefabToUse = splinterPrefab != null ? splinterPrefab : gameObject;

            // Spawn at the exact edge where the projectile hit, not inside the enemy
            GameObject splinter = Instantiate(prefabToUse, impactPoint, Quaternion.identity);

            // 🛠️ CRITICAL FIX: Tell Unity's physics engine to completely ignore the enemy we just hit!
            Collider2D splinterCollider = splinter.GetComponent<Collider2D>();
            if (splinterCollider != null && hitEnemyCollider != null)
            {
                Physics2D.IgnoreCollision(splinterCollider, hitEnemyCollider);
            }

            splinter.transform.localScale = transform.localScale * 0.5f; // Half size

            ProjectileCollisionTrigger trig = splinter.GetComponent<ProjectileCollisionTrigger>();
            if (trig != null) trig.MarkAsSplinter(splinterDamage);

            ElementalProjectile elem = splinter.GetComponent<ElementalProjectile>();
            if (elem != null)
            {
                // Randomize speed slightly for a better visual explosion
                float randomSpeed = Random.Range(13f, 17f);
                elem.InitializeVelocity(launchDir, randomSpeed);
            }
        }
    }

    private void ReflectOffSurface(Collider2D wall)
    {
        if (rb == null) return;
        Vector2 normal = (transform.position - wall.transform.position).normalized;
        rb.velocity = Vector2.Reflect(rb.velocity, normal).normalized * 12f;
    }
}