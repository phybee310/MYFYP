using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class CatInteraction : MonoBehaviour
{
    [Header("Audio Elements")]
    [Tooltip("Sound played on a quick tap")]
    [SerializeField] private AudioClip normalMeowClip;

    [Tooltip("Sound played when swiped/patted")]
    [SerializeField] private AudioClip happyMeowClip;

    private AudioSource _audioSource;

    [Header("Animation Triggers")]
    [SerializeField] private string laydownTrigger = "PlayLaydown";
    [SerializeField] private string sitTrigger = "PlaySit";
    [SerializeField] private float laydownDuration = 4f;

    private Animator _animator;
    private bool _isLayingDown = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    // --- NEW: Called when the user does a QUICK TAP ---
    public void NormalTap()
    {
        // Don't play the normal meow if the cat is currently resting
        if (_isLayingDown) return;

        if (normalMeowClip != null)
        {
            // PlayOneShot allows sounds to overlap slightly if tapped quickly
            _audioSource.PlayOneShot(normalMeowClip);
        }
    }

    // --- Called when the user SWIPES / PATS ---
    public void PatCat()
    {
        if (_isLayingDown) return;

        StartCoroutine(PatSequence());
    }

    private IEnumerator PatSequence()
    {
        _isLayingDown = true;

        // Play the HAPPY Sound!
        if (happyMeowClip != null)
        {
            _audioSource.PlayOneShot(happyMeowClip);
        }

        _animator.SetTrigger(laydownTrigger);

        yield return new WaitForSeconds(laydownDuration);

        _animator.SetTrigger(sitTrigger);
        _isLayingDown = false;
    }
}