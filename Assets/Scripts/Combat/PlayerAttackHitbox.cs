using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int overlapBufferSize = 16;

    private Collider[] overlapResults;

    private void Awake()
    {
        overlapResults = new Collider[Mathf.Max(4, overlapBufferSize)];
    }

    public void ProcessHits(Transform attacker, Transform attackOrigin, MeleeAttackData attackData, HashSet<int> hitTargets)
    {
        if (attacker == null || attackOrigin == null || attackData == null)
        {
            return;
        }

        EnsureBuffer();

        Vector3 center = GetAttackCenter(attackOrigin, attackData);
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            attackData.Radius,
            overlapResults,
            attackData.TargetLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = overlapResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform.IsChildOf(attacker))
            {
                continue;
            }

            Component damageableComponent = hitCollider.GetComponentInParent(typeof(IDamageable)) as Component;
            if (damageableComponent == null)
            {
                continue;
            }

            if (!hitTargets.Add(damageableComponent.GetInstanceID()))
            {
                continue;
            }

            IDamageable damageable = damageableComponent as IDamageable;
            if (damageable == null)
            {
                continue;
            }

            damageable.Damage(attackData.Damage);

            EnemyHitReaction hitReaction = damageableComponent.GetComponent<EnemyHitReaction>();
            if (hitReaction == null)
            {
                hitReaction = damageableComponent.GetComponentInParent<EnemyHitReaction>();
            }

            hitReaction?.PlayHit(attacker.position, attackData.KnockbackForce);

            if (attackData.HitSfx != null)
            {
                AudioSource.PlayClipAtPoint(attackData.HitSfx, hitCollider.ClosestPoint(center));
            }

            if (attackData.HitVfx != null)
            {
                Instantiate(attackData.HitVfx, hitCollider.ClosestPoint(center), Quaternion.identity);
            }
        }
    }

    public Vector3 GetAttackCenter(Transform attackOrigin, MeleeAttackData attackData)
    {
        return attackOrigin.position + (attackOrigin.forward * attackData.ForwardOffset);
    }

    public void DrawGizmos(Transform attackOrigin, MeleeAttackData attackData)
    {
        if (attackOrigin == null || attackData == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.35f);
        Gizmos.DrawSphere(GetAttackCenter(attackOrigin, attackData), attackData.Radius);
    }

    private void EnsureBuffer()
    {
        if (overlapResults == null || overlapResults.Length != Mathf.Max(4, overlapBufferSize))
        {
            overlapResults = new Collider[Mathf.Max(4, overlapBufferSize)];
        }
    }
}
