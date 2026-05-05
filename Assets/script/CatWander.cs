using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CatWander : MonoBehaviour
{
    [Header("Wander Settings")]
    public float wanderRadius = 1.0f;
    public float walkSpeed = 0.2f;
    public float waitTimeBetweenWalks = 2.0f;

    private Animator _animator;
    private Vector3 _homePosition;
    private Vector3 _targetPosition;

    private bool _isWalking = false;
    private float _waitTimer = 0f;
    private bool _isBeingDragged = false;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _homePosition = transform.position;
        SetNewRandomTarget();
    }

    private void Update()
    {
        if (_isBeingDragged) return;

        if (_isWalking)
        {
            // 1. Move the cat
            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, walkSpeed * Time.deltaTime);

            // 2. Rotate the cat to face its target
            Vector3 direction = (_targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            // 3. Stop if we reached the target
            if (Vector3.Distance(transform.position, _targetPosition) < 0.05f)
            {
                StopWalking();
            }
        }
        else
        {
            // 4. Wait for the timer to finish before walking again
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= waitTimeBetweenWalks)
            {
                SetNewRandomTarget();
            }
        }
    }

    private void SetNewRandomTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        _targetPosition = new Vector3(_homePosition.x + randomCircle.x, transform.position.y, _homePosition.z + randomCircle.y);

        _isWalking = true;
        _animator.SetBool("IsWalking", true);
    }

    private void StopWalking()
    {
        _isWalking = false;
        _waitTimer = 0f;
        _animator.SetBool("IsWalking", false);

        // Notice the random Meow code is completely gone from here!
    }

    public void PauseWander()
    {
        _isBeingDragged = true;
        _isWalking = false;
        _animator.SetBool("IsWalking", false);
    }

    public void ResumeWander(Vector3 newHomePosition)
    {
        _isBeingDragged = false;
        _homePosition = newHomePosition;
        _waitTimer = 0f;
    }
}