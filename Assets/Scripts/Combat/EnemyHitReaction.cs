using UnityEngine;

public class EnemyHitReaction : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private float upwardKnockbackModifier = 0.25f;
    [SerializeField] private float knockbackMultiplier = 1f;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        targetRigidbody = GetComponent<Rigidbody>();
    }

    public void PlayHit(Vector3 attackerPosition, float knockbackForce)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(hitTriggerName))
        {
            animator.SetTrigger(hitTriggerName);
        }

        if (targetRigidbody == null || knockbackForce <= 0f)
        {
            return;
        }

        Vector3 direction = transform.position - attackerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        Vector3 force = (direction + (Vector3.up * upwardKnockbackModifier)) * (knockbackForce * knockbackMultiplier);
        targetRigidbody.AddForce(force, ForceMode.Impulse);
    }
}
