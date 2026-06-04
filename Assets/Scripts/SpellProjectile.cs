using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;

    private Vector2 direction;

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);
    }
}