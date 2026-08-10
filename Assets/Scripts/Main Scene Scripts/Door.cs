using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isTransitioning && other.CompareTag("Player"))
        {
            // 🛠️ SAFETY LOCK: Ask StageController if portal is actually active
            StageController stage = GetComponentInParent<StageController>();
            if (stage == null)
            {
                stage = FindObjectOfType<StageController>();
            }

            // If stage controller exists and portal is NOT active, lock the door!
            if (stage != null && !stage.IsPortalActive())
            {
                Debug.Log("[Door] Portal is locked! Defeat all enemies first.");
                return;
            }

            isTransitioning = true;

            AreaStageManager manager = FindObjectOfType<AreaStageManager>();
            if (manager != null)
            {
                manager.TriggerNextStageTransition();
            }
        }
    }
}