using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ElementalProjectile : MonoBehaviour
{
    [Header("Elemental Identity")]
    [Tooltip("Assign the type that matches this projectile prefab asset!")]
    [SerializeField] private ElementType projectileElement = ElementType.Default;

    [Header("Damage Calibration")]
    [Tooltip("Base damage. Default should be ~5, Elements should be ~25.")]
    public int baseDamage = 25;

    [Header("Visual Settings")]
    [Tooltip("Tweak this angle if the sprite's head/tail are facing the wrong way (e.g., 90, -90, 180)")]
    public float spriteRotationOffset = 0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-nerf the default projectile to force sigil drawing
        if (projectileElement == ElementType.Default)
        {
            baseDamage = 5;
        }
    }

    public ElementType GetElementType() => projectileElement;

    public void InitializeVelocity(Vector2 direction, float speed)
    {
        // 1. Apply the physics force
        if (rb != null) rb.velocity = direction * speed;

        // 2. Exact rotation math from your old SpellProjectile script
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}