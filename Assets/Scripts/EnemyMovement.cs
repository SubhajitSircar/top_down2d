using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public enum LeechState { Exploring, Chasing }

    [Header("Movement Speeds")]
    public float exploreSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float latchPushSpeed = 1.0f; // Slow persistent force to shove the player into corners
    public float stopDistance = 0.2f;   // Kept practically at zero so they get right on top of you

    [Header("Detection Settings")]
    public float chaseRadius = 10f;
    public float loseChaseRadius = 50f;
    public LayerMask wallLayer;

    [Header("Feeding Settings")]
    public float damagePerSecond = 5f;

    [Header("State Debug")]
    public LeechState currentState = LeechState.Exploring;

    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 exploreDirection;
    private float directionChangeTimer;
    private float maxTimePerDirection = 4f;

    private bool isLatchedOnPlayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        PickRandomDirection();
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case LeechState.Exploring:
                ExploreBehavior();
                CheckForPlayer();
                break;

            case LeechState.Chasing:
                ChaseBehavior();
                break;
        }

        // FEEDING LOGIC: If latched onto the player, suck health continuously
        if (isLatchedOnPlayer && playerHealth != null)
        {
            // Multiplied by Time.fixedDeltaTime so it drains smoothly precisely over 1 real second
            playerHealth.TakeDamageOverTime(damagePerSecond * Time.fixedDeltaTime);
        }
    }

    void ExploreBehavior()
    {
        directionChangeTimer += Time.fixedDeltaTime;

        float checkDistance = Mathf.Max(1.5f, exploreSpeed * 0.4f);
        RaycastHit2D wallHit = Physics2D.Raycast(rb.position, exploreDirection, checkDistance, wallLayer);

        if (wallHit.collider != null || directionChangeTimer >= maxTimePerDirection)
        {
            PickRandomDirection();
        }

        MoveLeech(exploreDirection, exploreSpeed);
    }

    void PickRandomDirection()
    {
        directionChangeTimer = 0f;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        exploreDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(player.position, rb.position);

        if (distanceToPlayer <= chaseRadius)
        {
            Vector2 directionToPlayer = ((Vector2)player.position - rb.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(rb.position, directionToPlayer, distanceToPlayer, wallLayer);

            if (hit.collider == null)
            {
                currentState = LeechState.Chasing;
            }
        }
    }

    void ChaseBehavior()
    {
        if (player == null)
        {
            currentState = LeechState.Exploring;
            return;
        }

        float distanceToPlayer = Vector2.Distance(player.position, rb.position);

        if (distanceToPlayer > loseChaseRadius)
        {
            currentState = LeechState.Exploring;
            PickRandomDirection();
            return;
        }

        Vector2 moveDirection = ((Vector2)player.position - rb.position).normalized;

        float dynamicCheckDistance = Mathf.Max(1.5f, chaseSpeed * 0.3f);
        RaycastHit2D wallCheck = Physics2D.Raycast(rb.position, moveDirection, dynamicCheckDistance, wallLayer);

        if (wallCheck.collider != null)
        {
            Vector2 leftSteer = new Vector2(-moveDirection.y, moveDirection.x);
            Vector2 rightSteer = new Vector2(moveDirection.y, -moveDirection.x);

            RaycastHit2D leftHit = Physics2D.Raycast(rb.position, leftSteer, dynamicCheckDistance, wallLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(rb.position, rightSteer, dynamicCheckDistance, wallLayer);

            if (leftHit.collider == null) moveDirection += leftSteer * 1.5f;
            else if (rightHit.collider == null) moveDirection += rightSteer * 1.5f;
        }

        // PHYSICAL PUSH LOGIC:
        // If they are further away, run full speed. If they hit the player, 
        // maintain a grinding latch speed to push her into the corners.
        if (distanceToPlayer > stopDistance)
        {
            MoveLeech(moveDirection.normalized, chaseSpeed);
        }
        else
        {
            MoveLeech(moveDirection.normalized, latchPushSpeed);
        }
    }

    void MoveLeech(Vector2 targetDirection, float currentSpeed)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 2f);
        Vector2 separation = Vector2.zero;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject == gameObject)
                continue;

            if (enemy.CompareTag("Enemy"))
            {
                separation += ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
            }
        }

        Vector2 finalDirection = targetDirection;
        if (targetDirection != Vector2.zero || separation != Vector2.zero)
        {
            // When close to the player, we reduce separation influence slightly
            // so they can compact into a tighter swarming clump over her body
            float separationWeight = (currentSpeed == latchPushSpeed) ? 0.3f : 1.0f;
            finalDirection = (targetDirection + (separation * separationWeight)).normalized;
        }

        rb.MovePosition(rb.position + finalDirection * currentSpeed * Time.fixedDeltaTime);

        if (animator != null)
        {
            animator.SetFloat("Speed", finalDirection.sqrMagnitude * currentSpeed);
        }
    }

    // Detection hooks using Unity Physics Colliders
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isLatchedOnPlayer = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isLatchedOnPlayer = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseChaseRadius);
    }
}