using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;

    [Header("Idle Sprite")]
    public Sprite idleSprite;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 movement;

    public bool isDashing = false;
    private bool canDash = true;

    public bool CanDash
    {
        get { return canDash; }
    }

    public GameObject spellPrefab;
    public Transform spellSpawnPoint;

    public GameObject dashIndicator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isDashing)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            movement.Normalize();

            if (movement != Vector2.zero)
            {
                animator.enabled = true;

                animator.SetFloat("MoveX", movement.x);
                animator.SetFloat("MoveY", movement.y);

                if (movement.x > 0)
                    spriteRenderer.flipX = false;
                else if (movement.x < 0)
                    spriteRenderer.flipX = true;
            }
            else
            {
                animator.enabled = false;

                if (idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
            }

            animator.SetFloat("Speed", movement.sqrMagnitude);
        }

        // Right Mouse Button
        if (Input.GetMouseButtonDown(1) && canDash)
        {
            StartCoroutine(Dash());
        }

        if (Input.GetMouseButtonDown(0))
        {
            CastSpell();
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
            return;

        Vector2 newPosition =
            rb.position +
            movement * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPosition);

    }


    void CastSpell()
    {
        Vector2 direction =
            (
                dashIndicator.transform.position -
                transform.position
            ).normalized;

        GameObject spell =
            Instantiate(
                spellPrefab,
                dashIndicator.transform.position,
                Quaternion.identity
            );

        SpellProjectile projectile =
            spell.GetComponent<SpellProjectile>();

        projectile.Initialize(direction);
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                Input.mousePosition
            );

        mousePosition.z = 0;

        Vector2 dashDirection =
            ((Vector2)mousePosition -
             rb.position).normalized;

        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            rb.MovePosition(
                rb.position +
                dashDirection *
                dashSpeed *
                Time.fixedDeltaTime
            );

            elapsedTime += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }
}