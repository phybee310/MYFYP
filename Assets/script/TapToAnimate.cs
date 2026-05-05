using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TapToAnimate : MonoBehaviour
{
    [Header("Interaction Settings")]
    public string triggerName = "PlayAnim";
    public AudioSource interactSound;

    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();

        if (interactSound == null) interactSound = GetComponent<AudioSource>();
    }

    public void TriggerAnimation()
    {
        // Play Animation (if it has one)
        if (_animator != null && !string.IsNullOrEmpty(triggerName))
        {
            _animator.SetTrigger(triggerName);
        }

        // Play Sound (if it has one)
        if (interactSound != null)
        {
            interactSound.Play();
        }
    }
}