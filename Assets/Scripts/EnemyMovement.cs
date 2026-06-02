using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float stopDistance = 2f;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector2 moveDirection =
            ((Vector2)player.position - rb.position)
            .normalized;

        Collider2D[] nearbyEnemies =
            Physics2D.OverlapCircleAll(
                transform.position,
                2f
            );

        Vector2 separation =
            Vector2.zero;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject == gameObject)
                continue;

            if (enemy.CompareTag("Enemy"))
            {
                separation +=
                    ((Vector2)transform.position -
                     (Vector2)enemy.transform.position)
                    .normalized;
            }
        }

        Vector2 finalDirection =
            (moveDirection + separation)
            .normalized;

        rb.MovePosition(
            rb.position +
            finalDirection *
            moveSpeed *
            Time.fixedDeltaTime
        );
    }
}