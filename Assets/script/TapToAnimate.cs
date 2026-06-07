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
       
        if (_animator != null && !string.IsNullOrEmpty(triggerName))
        {
            _animator.SetTrigger(triggerName);
        }

        if (interactSound != null)
        {
            interactSound.Play();
        }
    }
}