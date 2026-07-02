using UnityEngine;

public class Door : MonoBehaviour
{
    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Player"))
        {
            DungeonGenerator generator =
                FindObjectOfType<DungeonGenerator>();

            generator.NextLevel();
        }
    }
}