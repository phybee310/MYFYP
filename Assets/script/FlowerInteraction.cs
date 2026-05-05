using UnityEngine;

public class FlowerInteraction : MonoBehaviour
{
    [Header("Magical Effects")]
    [Tooltip("Drag your Butterfly Particle System object here")]
    [SerializeField] private ParticleSystem butterflyParticles;

    [Tooltip("Optional: Add a magical chime sound!")]
    [SerializeField] private AudioSource magicalSound;

    // Called by your main AR script when the raycast hits the flower
    public void TapFlower()
    {
        // 1. Play the glowing butterflies
        if (butterflyParticles != null)
        {
            butterflyParticles.Play();
        }
        else
        {
            Debug.LogWarning("Butterfly particles are missing from the Flower!");
        }

        // 2. Play a sound if you added one
        if (magicalSound != null)
        {
            magicalSound.Play();
        }
    }
}