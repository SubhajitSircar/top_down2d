using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Dynamic Spell Prefab")]
    [SerializeField] private GameObject dynamicSpellPrefab;

    [Header("Spell Settings")]
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float spellLifetime = 3f;
    [SerializeField] private float drawingScaleMultiplier = 0.02f;

    // This stores the last drawing pattern the player sketched
    private List<Vector2> activeSpellPattern = new List<Vector2>();
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        // Setup a basic default projectile line (a simple dot/square) 
        // so the player can shoot even before drawing anything for the first time
        activeSpellPattern.Add(new Vector2(-10, 0));
        activeSpellPattern.Add(new Vector2(10, 0));
    }

    void Update()
    {
        // Continuous Shooting Loop
        if (Input.GetMouseButtonDown(0))
        {
            // Only fire if the mouse cursor is inside the 70% gameplay zone on the left
            if (Input.mousePosition.x < Screen.width * 0.7f)
            {
                FireActiveSpell();
            }
        }
    }

    // Called by the DrawingPad script whenever a brand-new design is drawn
    public void UpdateDrawnSpellPattern(List<Vector2> newPattern)
    {
        if (newPattern == null || newPattern.Count < 2) return;

        // Overwrite our old spell memory with the new design layout!
        activeSpellPattern = new List<Vector2>(newPattern);
        Debug.Log("<color=yellow><b>Spell Updated! New shape locked into memory.</b></color>");
    }

    private void FireActiveSpell()
    {
        if (dynamicSpellPrefab == null || activeSpellPattern.Count < 2) return;

        // 1. Calculate direction from the player toward the mouse cursor in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 shootDirection = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        // 2. Spawn the empty projectile base container
        GameObject newSpell = Instantiate(dynamicSpellPrefab, transform.position, Quaternion.identity);
        Destroy(newSpell, spellLifetime);

        // 3. Attach a LineRenderer to project the drawing shape
        LineRenderer line = newSpell.AddComponent<LineRenderer>();
        line.startWidth = 0.15f;
        line.endWidth = 0.15f;
        line.useWorldSpace = false;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.cyan;
        line.endColor = Color.white;

        Vector2[] colliderPoints = new Vector2[activeSpellPattern.Count];
        line.positionCount = activeSpellPattern.Count;

        // Find the mathematical center of the saved layout vectors
        Vector2 centerOffset = Vector2.zero;
        foreach (Vector2 point in activeSpellPattern) centerOffset += point;
        centerOffset /= activeSpellPattern.Count;

        for (int i = 0; i < activeSpellPattern.Count; i++)
        {
            Vector2 localAdjustedPoint = (activeSpellPattern[i] - centerOffset) * drawingScaleMultiplier;
            colliderPoints[i] = localAdjustedPoint;
            line.SetPosition(i, new Vector3(localAdjustedPoint.x, localAdjustedPoint.y, 0f));
        }

        // 4. Update the physics edge collider boundaries to mirror the shape perfectly
        EdgeCollider2D edgeCollider = newSpell.GetComponent<EdgeCollider2D>();
        if (edgeCollider != null)
        {
            edgeCollider.points = colliderPoints;
            edgeCollider.isTrigger = true;
        }

        // 5. Blast the custom shaped beam towards your mouse aim direction!
        Rigidbody2D rb = newSpell.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDirection * projectileSpeed;

            // Turn the drawing shape to face towards your aiming line smoothly
            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            newSpell.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }
}