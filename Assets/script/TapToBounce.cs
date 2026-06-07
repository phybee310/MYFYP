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

   
    private bool _isBouncing = false;
    private Vector3 _originalScale;

    private void Start()
    {
       
        _originalScale = transform.localScale;

   
        if (worrySpores == null)
        {
            worrySpores = GetComponentInChildren<ParticleSystem>();
        }
    }


    public void TriggerAnimation()
    {
     
        if (worrySpores != null)
        {
            worrySpores.Play();
        }

        if (!_isBouncing)
        {
            StartCoroutine(DoSquashAndStretch());
        }

        Debug.Log($"You tapped {gameObject.name} and released the worries!");
    }

    private IEnumerator DoSquashAndStretch()
    {
        _isBouncing = true; 

        Vector3 squashed = new Vector3(_originalScale.x * stretchAmount, _originalScale.y * squashAmount, _originalScale.z * stretchAmount);
        Vector3 stretched = new Vector3(_originalScale.x * squashAmount, _originalScale.y * stretchAmount, _originalScale.z * squashAmount);

        float elapsed = 0f;

       
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(_originalScale, squashed, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(squashed, stretched, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        
        elapsed = 0f;
        while (elapsed < bounceSpeed)
        {
            transform.localScale = Vector3.Lerp(stretched, _originalScale, elapsed / bounceSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        
        transform.localScale = _originalScale;

  
        _isBouncing = false;
    }
}