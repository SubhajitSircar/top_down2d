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
        if (rb != null) rb.velocity = direction * speed;
    }
}