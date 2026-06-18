using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 15f;
    public float lifeTime = 3f;
    public int damageDealt = 10; // 🛠️ Damage value per single fireball impact

    [Header("Premium Impact Polish")]
    [Tooltip("How hard this spell pushes the enemy back.")]
    public float knockbackForce = 5f;
    [Tooltip("How long the enemy freezes when hit by this spell.")]
    public float stunDuration = 0.15f;

    private Vector2 direction;

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Moves the projectile forward manually every frame
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Wall Collision Handler
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            // TODO: Add a small particle effect instantiation here for a premium wall hit
            Destroy(gameObject);
        }

        // 🛠️ ENEMY IMPACT INTERCEPTOR
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // 🛠️ UPDATED CALL: Now includes the knockback and stun duration required by the new system!
                enemyHealth.TakeDamage(damageDealt, direction, knockbackForce, stunDuration);

                // TODO: Add impact spark particles here before destroying
                Destroy(gameObject);
            }
        }
    }
}