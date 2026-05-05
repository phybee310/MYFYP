using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TapToBounce : MonoBehaviour
{
    [Header("Worry Spore Settings")]
    [Tooltip("Drag the Particle System (Worry Spores) here. If left blank, the script will try to find it automatically.")]
    public ParticleSystem worrySpores;

    [Header("Bounce Settings")]
    [Tooltip("How fast the mushroom squashes and stretches.")]
    public float bounceSpeed = 0.08f;
    [Tooltip("How much the mushroom squishes down (e.g., 0.8 is 80% height).")]
    public float squashAmount = 0.8f;
    [Tooltip("How much the mushroom stretches up (e.g., 1.1 is 110% height).")]
    public float stretchAmount = 1.1f;

    // Internal safety variables
    private bool _isBouncing = false;
    private Vector3 _originalScale;

    private void Start()
    {
        // Save the original size so the mushroom always returns to normal
        _originalScale = transform.localScale;

        // Auto-find the particles if you forget to drag them in the Inspector
        if (worrySpores == null)
        {
            worrySpores = GetComponentInChildren<ParticleSystem>();
        }
    }

    // NOTE: Your ARPlaceAndDragCube script is looking for this exact method name!
    public void TriggerAnimation()
    {
        // 1. Release the worries (Play Particles)
        if (worrySpores != null)
        {
            worrySpores.Play();
        }

        // 2. Do the physical squish (Play Code-Based Bounce)
        if (!_isBouncing)
        {
            StartCoroutine(DoSquashAndStretch());
        }

        Debug.Log($"You tapped {gameObject.name} and released the worries!");
    }

    private IEnumerator DoSquashAndStretch()
    {
        _isBouncing = true; // Lock it so they can't spam tap and break the math

        // Define our target shapes based on the original scale
        Vector3 squashed = new Vector3(_originalScale.x * stretchAmount, _originalScale.y * squashAmount, _originalScale.z * stretchAmount);
        Vector3 stretched = new Vector3(_originalScale.x * squashAmount, _originalScale.y * stretchAmount, _originalScale.z * squashAmount);

        float elapsed = 0f;

        // Phase 1: Squash down (Impact)
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(_originalScale, squashed, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: Stretch up (Rebound)
        elapsed = 0f;
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(squashed, stretched, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 3: Settle back to normal
        elapsed = 0f;
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(stretched, _originalScale, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Force it exactly back to its original size just to be 100% safe
        transform.localScale = _originalScale;

        // Unlock so it can be tapped again
        _isBouncing = false;
    }
}