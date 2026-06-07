using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    // The purple animation has quite a few frames, so 0.5 to 0.6 seconds is perfect 
    public float lifeTime = 0.5f;

    void Start()
    {
        // This completely purges the object from your game's memory once the timer ends
        Destroy(gameObject, lifeTime);
    }
}