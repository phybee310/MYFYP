using UnityEngine;

public class FlowerInteraction : MonoBehaviour
{
    [Header("Magical Effects")]
    [SerializeField] private ParticleSystem butterflyParticles;

    [SerializeField] private AudioSource magicalSound;

    public void TapFlower()
    {
        if (butterflyParticles != null)
        {
            butterflyParticles.Play();
        }
        else
        {
            Debug.LogWarning("Butterfly particles are missing from the Flower!");
        }

       
        if (magicalSound != null)
        {
            magicalSound.Play();
        }
    }
}