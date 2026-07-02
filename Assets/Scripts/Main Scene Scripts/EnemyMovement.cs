using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    // State machine tracking for procedural AI routines
    public enum LeechState { Exploring, Chasing }

    [Header("Movement Speeds")]
    public float exploreSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float latchPushSpeed = 1.0f; // Speed applied when tightly latched onto player footprint
    public float stopDistance = 0.2f;

    [Header("Detection Settings")]
    public float chaseRadius = 10f;
    public float loseChaseRadius = 50f;
    public LayerMask wallLayer;

    [Header("Feeding Settings")]
    public float damagePerSecond = 5f;

    [Header("State Debug")]
    public LeechState currentState = LeechState.Exploring;

    // External component references
    private Transform player;
    private NewPlayerHealth playerHealth;
    private NewPlayerMovement playerMovement;
    private Rigidbody2D rb;
    private Animator animator;

    // Movement tracking vectors & timers
    private Vector2 exploreDirection;
    private float directionChangeTimer;
    private float maxTimePerDirection = 4f;

    private bool isLatchedOnPlayer = false;

    void Start()
    {
        // Cache internal physics components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Find and link player object reference components
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;

            // 🛠️ THE FIX: Grab the "New" scripts here!
            playerHealth = playerObj.GetComponent<NewPlayerHealth>();
            playerMovement = playerObj.GetComponent<NewPlayerMovement>();
        }

        // Initialize first random travel direction vector
        PickRandomDirection();
    }

    void FixedUpdate()
    {
        // Process behaviors based on current active state machine node
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

        // Apply health-draining tick sequentially over consecutive physics frames
        if (isLatchedOnPlayer && playerHealth != null)
        {
            playerHealth.TakeDamageOverTime(damagePerSecond * Time.fixedDeltaTime);
        }
    }

    void ExploreBehavior()
    {
        directionChangeTimer += Time.fixedDeltaTime;

        // Raycast forward to prevent running into tilemap walls before impact
        float checkDistance = Mathf.Max(1.5f, exploreSpeed * 0.4f);
        RaycastHit2D wallHit = Physics2D.Raycast(rb.position, exploreDirection, checkDistance, wallLayer);

        // Turn immediately if wall is detected or direction change timer expires
        if (wallHit.collider != null || directionChangeTimer >= maxTimePerDirection)
        {
            PickRandomDirection();
        }

        MoveLeech(exploreDirection, exploreSpeed);
    }

    void PickRandomDirection()
    {
        directionChangeTimer = 0f;
        // Generate random vector using basic trigonometry mapping
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        exploreDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(player.position, rb.position);

        // If player enters perimeter, run a line-of-sight raycast to ensure no wall is blocking visibility
        if (distanceToPlayer <= chaseRadius)
        {
            Vector2 directionToPlayer = ((Vector2)player.position - rb.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(rb.position, directionToPlayer, distanceToPlayer, wallLayer);

            // Transition to chasing if path is totally clear
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

        // Drop target focus if player moves past max escape boundaries
        if (distanceToPlayer > loseChaseRadius)
        {
            currentState = LeechState.Exploring;
            PickRandomDirection();
            return;
        }

        Vector2 moveDirection = ((Vector2)player.position - rb.position).normalized;

        // Dynamic wall dodging while tracking: check if direct path to player hits an obstacle corner
        float dynamicCheckDistance = Mathf.Max(1.5f, chaseSpeed * 0.3f);
        RaycastHit2D wallCheck = Physics2D.Raycast(rb.position, moveDirection, dynamicCheckDistance, wallLayer);

        if (wallCheck.collider != null)
        {
            // Calculate left and right perpendicular steering options
            Vector2 leftSteer = new Vector2(-moveDirection.y, moveDirection.x);
            Vector2 rightSteer = new Vector2(moveDirection.y, -moveDirection.x);

            RaycastHit2D leftHit = Physics2D.Raycast(rb.position, leftSteer, dynamicCheckDistance, wallLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(rb.position, rightSteer, dynamicCheckDistance, wallLayer);

            // Steer along whichever path is not blocked by wall structures
            if (leftHit.collider == null) moveDirection += leftSteer * 1.5f;
            else if (rightHit.collider == null) moveDirection += rightSteer * 1.5f;
        }

        // Adjust velocity contextually depending on distance profile to target bounds
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
        // Flocking Mechanics: Query proximity to prevent enemy sprites from overlapping heavily
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 2f);
        Vector2 separation = Vector2.zero;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject == gameObject)
                continue;

            if (enemy.CompareTag("Enemy"))
            {
                // Accumulate pushaway vector vectors from neighboring leeches
                separation += ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
            }
        }

        // Combine direct target pathing vector with group separation priorities
        Vector2 finalDirection = targetDirection;
        if (targetDirection != Vector2.zero || separation != Vector2.zero)
        {
            float separationWeight = (currentSpeed == latchPushSpeed) ? 0.3f : 1.0f;
            finalDirection = (targetDirection + (separation * separationWeight)).normalized;
        }

        // Apply spatial translation updates cleanly via Rigidbody position cycles
        rb.MovePosition(rb.position + finalDirection * currentSpeed * Time.fixedDeltaTime);

        // Update blending tree values on the animator controller asset if mapped
        if (animator != null)
        {
            animator.SetFloat("Speed", finalDirection.sqrMagnitude * currentSpeed);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Engage physical feeding state when hitting player collider layers
        if (collision.gameObject.CompareTag("Player"))
        {
            isLatchedOnPlayer = true;

            // Trigger the red hit-flash visual loop indicator instantly on contact
            if (playerMovement != null)
            {
                playerMovement.TriggerHurtFlash();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Terminate feeding state parameters once contact is broken
        if (collision.gameObject.CompareTag("Player"))
        {
            isLatchedOnPlayer = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Render debugging sphere outlines directly inside the editor scene viewport windows
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseChaseRadius);
    }
}