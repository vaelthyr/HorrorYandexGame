using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerCharacterController))]
[RequireComponent(typeof(CharacterAnimatorController))]
[RequireComponent(typeof(PlayerAttackHitbox))]
public class PlayerAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private PlayerCharacterController characterController;
    [SerializeField] private CharacterAnimatorController animatorController;
    [SerializeField] private PlayerAttackHitbox attackHitbox;

    [Header("Attack")]
    [SerializeField] private MeleeAttackData basicAttack = new MeleeAttackData();

    private readonly HashSet<int> hitTargets = new HashSet<int>();

    private bool wasAttackHeld;
    private bool attackInProgress;
    private bool attackWindowActive;
    private bool lungeApplied;
    private float attackTimer;

    public bool IsAttacking => attackInProgress;

    private void Reset()
    {
        attackOrigin = transform;
        characterController = GetComponent<PlayerCharacterController>();
        animatorController = GetComponent<CharacterAnimatorController>();
        attackHitbox = GetComponent<PlayerAttackHitbox>();
    }

    private void Awake()
    {
        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }

        if (characterController == null)
        {
            characterController = GetComponent<PlayerCharacterController>();
        }

        if (animatorController == null)
        {
            animatorController = GetComponent<CharacterAnimatorController>();
        }

        if (attackHitbox == null)
        {
            attackHitbox = GetComponent<PlayerAttackHitbox>();
        }
    }

    private void Update()
    {
        bool attackHeld = InputManager.instance != null && InputManager.instance.Fire;
        bool attackPressed = attackHeld && !wasAttackHeld;
        wasAttackHeld = attackHeld;

        if (attackPressed)
        {
            TryStartAttack();
        }

        if (attackInProgress)
        {
            TickAttack(Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        ResetAttackState();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackHitbox == null)
        {
            attackHitbox = GetComponent<PlayerAttackHitbox>();
        }

        attackHitbox?.DrawGizmos(attackOrigin != null ? attackOrigin : transform, basicAttack);
    }

    private void TryStartAttack()
    {
        if (attackInProgress || basicAttack == null || characterController == null)
        {
            return;
        }

        if (characterController.CurrentCharacterState != CharacterState.Default)
        {
            return;
        }

        if (basicAttack.RequireGrounded && !characterController.Motor.GroundingStatus.IsStableOnGround)
        {
            return;
        }

        attackInProgress = true;
        attackWindowActive = false;
        lungeApplied = false;
        attackTimer = 0f;
        hitTargets.Clear();

        animatorController?.PlayAttackAnimation();

        if (basicAttack.SwingSfx != null)
        {
            AudioSource.PlayClipAtPoint(basicAttack.SwingSfx, transform.position);
        }
    }

    private void TickAttack(float deltaTime)
    {
        attackTimer += deltaTime;

        if (!attackWindowActive && attackTimer >= basicAttack.Startup)
        {
            BeginAttackWindow();
        }

        if (attackWindowActive)
        {
            attackHitbox.ProcessHits(transform, attackOrigin, basicAttack, hitTargets);

            if (attackTimer >= basicAttack.Startup + basicAttack.ActiveTime)
            {
                EndAttackWindow();
            }
        }

        if (attackTimer >= basicAttack.TotalDuration)
        {
            ResetAttackState();
        }
    }

    private void BeginAttackWindow()
    {
        attackWindowActive = true;

        if (!lungeApplied && basicAttack.ForwardLunge > 0f)
        {
            characterController.AddVelocity(transform.forward * basicAttack.ForwardLunge);
            lungeApplied = true;
        }
    }

    private void EndAttackWindow()
    {
        attackWindowActive = false;
    }

    private void ResetAttackState()
    {
        attackInProgress = false;
        attackWindowActive = false;
        lungeApplied = false;
        attackTimer = 0f;
        hitTargets.Clear();
    }
}
