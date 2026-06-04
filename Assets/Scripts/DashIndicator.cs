using UnityEngine;

public class DashIndicator : MonoBehaviour
{
    public Transform player;
    public float distanceFromPlayer = 1.5f;

    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        playerMovement =
            player.GetComponent<PlayerMovement>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null)
            return;

        // Only show when dash is ready
        if (playerMovement == null ||
            !playerMovement.CanDash)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;

        Vector3 mousePos =
            Camera.main.ScreenToWorldPoint(
                Input.mousePosition
            );

        mousePos.z = 0;

        Vector2 direction =
            ((Vector2)mousePos -
             (Vector2)player.position).normalized;

        transform.position =
            player.position +
            (Vector3)(direction *
            distanceFromPlayer);

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0,
                0,
                angle
            );
    }
}