using KinematicCharacterController;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum OrientationMethod
{
    TowardsCamera,
    TowardsMovement,
}

public enum CharacterState
{
    Default,
    NoClip,
    Charging,
    Swimming,
    Climbing,
    AFK,
    Dance
}

public enum ClimbingState
{
    Anchoring,
    Climbing,
    DeAnchoring
}

public enum WallSprintState
{
    Anchoring,
    Sprinting,
    DeAnchoring
}

public enum BonusOrientationMethod
{
    None,
    TowardsGravity,
    TowardsGroundSlopeAndGravity,
}

public struct PlayerCharacterInputs
{
    public float MoveAxisForward;
    public float MoveAxisRight;
    public Quaternion CameraRotation;
    public bool JumpDown;
    public bool JumpHeld;
    public bool CrouchDown;
    public bool CrouchUp;
    public bool CrouchHeld;
    public bool NoClipDown;
    public bool ChargingDown;
    public bool ClimbLadder;
    public bool Sprint;
    public bool Attack;
    public bool Dance;
}

public class PlayerCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [Header("Audio")] [SerializeField] private AudioClip LandingAudioClip;
    [SerializeField] private AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Flags")] public bool CanSwim = false;
    public bool CanClimb = false;
    public bool CanWallSprint = false;

    [Header("Stable Movement")] public float MaxStableMoveSpeed = 10f;
    public float StableMovementSharpness = 15;
    public float OrientationSharpness = 10;
    public float MaxStableDistanceFromLedge = 5f;
    [Range(0f, 180f)] public float MaxStableDenivelationAngle = 180f;
    public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

    [Header("Sprinting")] public float MaxSprintingSpeed = 16f;
    public float MaxClimbingSprintingSpeed = 7f;
    public float MaxSwimingSprintingSpeed = 7f;
    public float MaxInAirSprintingSpeed = 16f;


    [Header("Wall Sprinting")] public float MaxWallSprintingSpeed = 10f;
    public float WallJumpReloadTime = 1f;
    public float WallSprintMaxTime = 4f;
    public float WallJumpVectorUpModifire = 0.5f;


    [Header("Air Movement")] public float MaxAirMoveSpeed = 10f;
    public float AirAccelerationSpeed = 5f;
    public float Drag = 0.1f;

    [Header("Jumping")] public bool AllowJumpingWhenSliding = false;
    public bool AllowDoubleJump = false;
    public bool AllowWallJump = false;
    public float JumpSpeed = 10f;
    public float JumpPreGroundingGraceTime = 0f;
    public float JumpPostGroundingGraceTime = 0f;


    [Header("NoClip")] public float NoClipMoveSpeed = 10f;
    public float NoClipSharpness = 15;

    [Header("Charging")] public float ChargeSpeed = 15f;
    public float MaxChargeTime = 1.5f;
    public float StoppedTime = 1f;
    public float ChargeReloadTimer = 1f;
    public bool ReloadTimerOnGround = true;

    [Header("Ladder Climbing")] public float ClimbingSpeed = 4f;
    public float AnchoringDuration = 0.25f;
    public LayerMask InteractionLayer;

    [Header("Swimming")] public Transform SwimmingReferencePoint;
    public LayerMask WaterLayer;
    public float SwimmingSpeed = 4f;
    public float SwimmingMovementSharpness = 3;
    public float SwimmingOrientationSharpness = 2f;


    [Header("Misc")] public List<Collider> IgnoredColliders = new List<Collider>();
    public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
    public float BonusOrientationSharpness = 10f;
    public Vector3 Gravity = new Vector3(0, -30f, 0);
    public Vector3 DefaultGravity = new Vector3(0, -30f, 0);
    public Transform MeshRoot;
    public Transform CameraFollowPoint;
    public float CrouchedCapsuleHeight = 1f;
    public bool OrientTowardsGravity = false;

    public CharacterState CurrentCharacterState { get; private set; }

    private Collider[] _probedColliders = new Collider[8];
    private Vector3 _moveInputVector;
    private Vector3 _lookInputVector;
    private bool _jumpInputIsHeld = false;
    private bool _crouchInputIsHeld = false;
    private bool _jumpRequested = false;
    private bool _jumpConsumed = false;
    private bool _doubleJumpConsumed = false;
    private bool _jumpedThisFrame = false;
    private bool _canWallJump = false;
    private Vector3 _wallJumpNormal;
    private Collider _previousWallJumpHitCollider;
    private float _timeSinceJumpRequested = Mathf.Infinity;
    private float _timeSinceLastAbleToJump = 0f;
    private Vector3 _internalVelocityAdd = Vector3.zero;
    private bool _shouldBeCrouching = false;
    private bool _isCrouching = false;

    private Vector3 _currentChargeVelocity;
    private bool _isStopped = false;
    private bool _mustStopVelocity = false;
    private float _timeSinceStartedCharge = 0;
    private float _timeSinceStopped = 0;
    private float _timeChargeReload = 0;
    private bool _wasOnGround;
    private bool _isSprint = false;
    private bool _isCanDance = false;
    private bool _isDance = false;

    private Collider _waterZone;

    private float _ladderUpDownInput;

    private bool _isWallSprint = false;
    private float _wallJumpReload = 0;
    float wallDetectionRadius = 1f;
    float wallDetectionDistance = 1.0f;
    private Vector3 _wallRunDirection;

    private CharacterAnimatorController _animatorController;


    private bool isGrabbing;
    private Vector3 grabPoint;
    private bool isFalling;
    private Rigidbody rb;


    Collider[] wallColliders = new Collider[8];
    private Ladder _activeLadder { get; set; }
    private ClimbingState _internalClimbingState;

    private ClimbingState _climbingState
    {
        get { return _internalClimbingState; }
        set
        {
            _internalClimbingState = value;
            _anchoringTimer = 0f;
            _anchoringStartPosition = Motor.TransientPosition;
            _anchoringStartRotation = Motor.TransientRotation;
        }
    }

    private WallSprintState _internalWallSprintState;

    private WallSprintState _wallSprintState
    {
        get { return _internalWallSprintState; }
        set
        {
            _internalWallSprintState = value;
            _anchoringTimer = 0f;
            _anchoringStartPosition = Motor.TransientPosition;
            _anchoringStartRotation = Motor.TransientRotation;
        }
    }


    private Vector3 _ladderTargetPosition;
    private Quaternion _ladderTargetRotation;
    private float _onLadderSegmentState = 0;
    private float _anchoringTimer = 0f;
    private Vector3 _anchoringStartPosition = Vector3.zero;
    private Quaternion _anchoringStartRotation = Quaternion.identity;
    private Quaternion _rotationBeforeClimbing = Quaternion.identity;

    private Vector3 lastInnerNormal = Vector3.zero;
    private Vector3 lastOuterNormal = Vector3.zero;
    private bool hitSomethingThisSweepIteration;

    public bool HitSomethingThisSweepIteration
    {
        get => hitSomethingThisSweepIteration;
        set => hitSomethingThisSweepIteration = value;
    }

    private void Start()
    {
        // Assign to motor
        Motor.CharacterController = this;
        _animatorController = GetComponent<CharacterAnimatorController>();
        // Handle initial state
        TransitionToState(CharacterState.Default);
    }

    /// <summary>
    /// Handles movement state transitions and enter/exit callbacks
    /// </summary>
    public void TransitionToState(CharacterState newState)
    {
        CharacterState tmpInitialState = CurrentCharacterState;
        OnStateExit(tmpInitialState, newState);
        CurrentCharacterState = newState;
        OnStateEnter(newState, tmpInitialState);
    }

    public void SetAFK(bool _isAFK)
    {
        if (_isAFK)
        {
            TransitionToState(CharacterState.AFK);
        }
        else
        {
            TransitionToState(CharacterState.Default);
        }
    }

    /// <summary>
    /// Event when entering a state
    /// </summary>
    public void OnStateEnter(CharacterState state, CharacterState fromState)
    {
        switch (state)
        {
            case CharacterState.AFK:
            {
                break;
            }
            case CharacterState.Default:
            {
                break;
            }
            case CharacterState.NoClip:
            {
                Motor.SetCapsuleCollisionsActivation(false);
                Motor.SetMovementCollisionsSolvingActivation(false);
                Motor.SetGroundSolvingActivation(false);
                break;
            }
            case CharacterState.Charging:
            {
                _animatorController.PlayDashAnimation();
                _currentChargeVelocity = Motor.CharacterForward * ChargeSpeed;
                _isStopped = false;
                _timeSinceStartedCharge = 0f;
                _timeSinceStopped = 0f;
                break;
            }
            case CharacterState.Swimming:
            {
                Motor.SetGroundSolvingActivation(false);
                break;
            }
            case CharacterState.Climbing:
            {
                _rotationBeforeClimbing = Motor.TransientRotation;

                Motor.SetMovementCollisionsSolvingActivation(false);
                Motor.SetGroundSolvingActivation(false);
                _climbingState = ClimbingState.Anchoring;

                // Store the target position and rotation to snap to
                _ladderTargetPosition =
                    _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                _ladderTargetRotation = _activeLadder.transform.rotation;
                _animatorController.isClimbing = true;
                break;
            }
        }
    }

    /// <summary>
    /// Event when exiting a state
    /// </summary>
    public void OnStateExit(CharacterState state, CharacterState toState)
    {
        switch (state)
        {
            case CharacterState.Default:
            {
                break;
            }
            case CharacterState.NoClip:
            {
                Motor.SetCapsuleCollisionsActivation(true);
                Motor.SetMovementCollisionsSolvingActivation(true);
                Motor.SetGroundSolvingActivation(true);
                break;
            }
            case CharacterState.Climbing:
            {
                Motor.SetMovementCollisionsSolvingActivation(true);
                Motor.SetGroundSolvingActivation(true);
                _animatorController.isClimbing = false;
                break;
            }
            case CharacterState.Charging:
            {
                _animatorController.EndDashAnimation();
                break;
            }
        }
    }

    /// <summary>
    /// This is called every frame by MyPlayer in order to tell the character what its inputs are
    /// </summary>
    public void SetInputs(ref PlayerCharacterInputs inputs)
    {
        _jumpInputIsHeld = inputs.JumpHeld;
        _crouchInputIsHeld = inputs.CrouchHeld;
        _isSprint = inputs.Sprint;

        // Clamp input
        Vector3 moveInputVector =
            Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection =
            Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection =
                Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }

        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

        if (_wallJumpReload >= 0)
        {
            _wallJumpReload -= Time.deltaTime;
        }


        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                // Move and look inputs
                _moveInputVector = cameraPlanarRotation * moveInputVector;
                _lookInputVector = cameraPlanarDirection;
                _isCanDance = true;

                if (_moveInputVector != Vector3.zero)
                {
                    _isCanDance = false;
                }

                // Jumping input
                if (inputs.JumpDown)
                {
                    _timeSinceJumpRequested = 0f;
                    _jumpRequested = true;
                    _isCanDance = false;
                }
                else
                {
                    _jumpRequested = false;
                }


                // Crouching input
                if (inputs.CrouchDown)
                {
                    _shouldBeCrouching = true;
                    _isCanDance = false;
                    if (!_isCrouching)
                    {
                        _isCrouching = true;
                        Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                        MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                    }
                }
                else if (inputs.CrouchUp)
                {
                    _shouldBeCrouching = false;
                }

                if (inputs.Attack)
                {
                    _isCanDance = false;
                }

                if (_isDance && !_isCanDance)
                {
                    _animatorController.EndDanceAnimation();
                    _isDance = false;
                }

                if (inputs.Dance && _isCanDance)
                {
                    if (!_isDance)
                    {
                        _animatorController.PlayDanceAnimation();
                        _isDance = true;
                    }
                }

                break;
            }
            case CharacterState.NoClip:
            {
                _jumpRequested = inputs.JumpHeld;
                _moveInputVector = inputs.CameraRotation * moveInputVector;
                _lookInputVector = cameraPlanarDirection;
                break;
            }
            case CharacterState.Dance:
            {
                break;
            }
            case CharacterState.Swimming:
            {
                _jumpRequested = inputs.JumpHeld;

                _moveInputVector = inputs.CameraRotation * moveInputVector;
                //_lookInputVector = cameraPlanarDirection;
                break;
            }
        }


        if (inputs.NoClipDown)
        {
            if (CurrentCharacterState == CharacterState.Default)
            {
                TransitionToState(CharacterState.NoClip);
            }
        }
        else
        {
            if (CurrentCharacterState == CharacterState.NoClip)
            {
                TransitionToState(CharacterState.Default);
            }
        }


        if (_timeChargeReload > 0)
        {
            _timeChargeReload -= Time.deltaTime;
        }

        if (Motor.GroundingStatus.IsStableOnGround && !_wasOnGround)
        {
            _wasOnGround = true;
        }

        if (inputs.ChargingDown && _timeChargeReload <= 0 && _wasOnGround)
        {
            TransitionToState(CharacterState.Charging);
            _timeChargeReload = ChargeReloadTimer;
            _wasOnGround = false;
        }


        _ladderUpDownInput = inputs.MoveAxisForward;

        if (inputs.ClimbLadder)
        {
            if (Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders,
                    InteractionLayer, QueryTriggerInteraction.Collide) > 0)
            {
                if (_probedColliders[0] != null)
                {
                    // Handle ladders
                    Ladder ladder = _probedColliders[0].gameObject.GetComponent<Ladder>();
                    if (ladder)
                    {
                        // Transition to ladder climbing state
                        if (CurrentCharacterState == CharacterState.Default)
                        {
                            _activeLadder = ladder;
                            TransitionToState(CharacterState.Climbing);
                        }
                        // Transition back to default movement state
                        else if (CurrentCharacterState == CharacterState.Climbing &&
                                 _climbingState == ClimbingState.Climbing)
                        {
                            _climbingState = ClimbingState.DeAnchoring;
                            _ladderTargetPosition = Motor.TransientPosition;
                            _ladderTargetRotation = _rotationBeforeClimbing;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Do a character overlap test to detect water surfaces
        if (Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, WaterLayer,
                QueryTriggerInteraction.Collide) > 0)
        {
            // If a water surface was detected
            if (_probedColliders[0] != null)
            {
                // If the swimming reference point is inside the box, make sure we are in swimming state
                if (Physics.ClosestPoint(SwimmingReferencePoint.position, _probedColliders[0],
                        _probedColliders[0].transform.position, _probedColliders[0].transform.rotation) ==
                    SwimmingReferencePoint.position)
                {
                    if (CurrentCharacterState == CharacterState.Default)
                    {
                        TransitionToState(CharacterState.Swimming);
                        _waterZone = _probedColliders[0];
                    }
                }
                // otherwise; default state
                else
                {
                    if (CurrentCharacterState == CharacterState.Swimming)
                    {
                        Motor.SetGroundSolvingActivation(true);
                        TransitionToState(CharacterState.Default);
                    }
                }
            }
            else
            {
                if (CurrentCharacterState == CharacterState.Swimming)
                {
                    Motor.SetGroundSolvingActivation(true);
                    TransitionToState(CharacterState.Default);
                }
            }
        }
        else
        {
            if (CurrentCharacterState == CharacterState.Swimming)
            {
                Motor.SetGroundSolvingActivation(true);
                TransitionToState(CharacterState.Default);
            }
        }

        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                break;
            }
            case CharacterState.Dance:
            {
                break;
            }
            case CharacterState.AFK:
            {
                break;
            }
            case CharacterState.Charging:
            {
                // Update times
                _timeSinceStartedCharge += deltaTime;
                if (_isStopped)
                {
                    _timeSinceStopped += deltaTime;
                }

                break;
            }
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            case CharacterState.NoClip:
            {
                if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f && _moveInputVector != Vector3.zero)
                {
                    // Smoothly interpolate from current to target look direction
                    Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _moveInputVector,
                        1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                    // Set the current rotation (which will be used by the KinematicCharacterMotor)
                    currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                }

                if (_canWallJump)
                {
                    if (_canWallJump
                        && _isSprint
                       )
                    {
                        Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _wallRunDirection,
                            1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                        // Set the current rotation (which will be used by the KinematicCharacterMotor)
                        currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        // _wallRunDirection - ���������� ����������� ���� �� �����
                    }
                }

                if (OrientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) *
                                      currentRotation;
                }


                break;
            }
            case CharacterState.Climbing:
            {
                switch (_climbingState)
                {
                    case ClimbingState.Climbing:
                        currentRotation = _activeLadder.transform.rotation;
                        break;
                    case ClimbingState.Anchoring:
                    case ClimbingState.DeAnchoring:
                        currentRotation = Quaternion.Slerp(_anchoringStartRotation, _ladderTargetRotation,
                            (_anchoringTimer / AnchoringDuration));
                        break;
                }

                break;
            }
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                Vector3 targetMovementVelocity = Vector3.zero;
                if (Motor.GroundingStatus.IsStableOnGround)
                {
                    // Reorient velocity on slope 
                    currentVelocity =
                        Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) *
                        currentVelocity.magnitude;

                    // Calculate target velocity
                    Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                    Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized *
                                              _moveInputVector.magnitude;
                    targetMovementVelocity = reorientedInput * (_isSprint ? MaxSprintingSpeed : MaxStableMoveSpeed);

                    // Smooth movement Velocity
                    currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                        1 - Mathf.Exp(-StableMovementSharpness * deltaTime));


                    if (_isSprint) _animatorController.isRun = true;
                    else _animatorController.isRun = false;
                    _animatorController.velocity = currentVelocity.magnitude;
                    _animatorController.onGround = true;
                    _animatorController.isJump = false;
                    _animatorController.isWallSprint = false;
                }
                else
                {
                    if (_moveInputVector.sqrMagnitude > 0f)
                    {
                        if (_canWallJump)
                        {
                            if (_canWallJump
                                && _isSprint
                                && currentVelocity.magnitude > 1f)
                            {
                                _animatorController.isJump = false;
                                if (_wallRunDirection == Vector3.zero)
                                {
                                    _wallRunDirection = Vector3.Cross(_wallJumpNormal, Motor.CharacterUp).normalized;

                                    if (Vector3.Dot(_wallRunDirection, currentVelocity) < 0)
                                    {
                                        _wallRunDirection = -_wallRunDirection;
                                        _animatorController.isWallRunRight = true;
                                    }
                                    else
                                    {
                                        _animatorController.isWallRunRight = false;
                                    }
                                }

                                _animatorController.isWallSprint = true;
                                currentVelocity = _wallRunDirection * MaxWallSprintingSpeed;
                            }
                        }
                        else
                        {
                            _animatorController.isWallSprint = false;
                            _animatorController.velocity = 0f;
                            _animatorController.onGround = false;
                            //_animatorController.PlayJumpAnimation();

                            targetMovementVelocity =
                                _moveInputVector * (_isSprint ? MaxInAirSprintingSpeed : MaxAirMoveSpeed);
                            if (Motor.GroundingStatus.FoundAnyGround)
                            {
                                Vector3 perpenticularObstructionNormal = Vector3
                                    .Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal),
                                        Motor.CharacterUp).normalized;
                                targetMovementVelocity =
                                    Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                            }

                            Vector3 velocityDiff =
                                Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                            currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                        }
                    }
                    else
                    {
                        _animatorController.isWallSprint = false;
                        _animatorController.velocity = 0f;
                        _animatorController.onGround = false;
                    }

                    currentVelocity += Gravity * deltaTime;
                    currentVelocity *= (1f / (1f + (Drag * deltaTime)));
                }

                // Handle jumping
                {
                    _jumpedThisFrame = false;
                    _timeSinceJumpRequested += deltaTime;
                    if (_jumpRequested)
                    {
                        // Handle double jump
                        if (AllowDoubleJump)
                        {
                            if (!_canWallJump && _jumpConsumed && !_doubleJumpConsumed && (AllowJumpingWhenSliding
                                    ? !Motor.GroundingStatus.FoundAnyGround
                                    : !Motor.GroundingStatus.IsStableOnGround) && _wallJumpReload <= 0)
                            {
                                Motor.ForceUnground(0.1f);
                                // Add to the return velocity and reset jump state
                                currentVelocity += (Motor.CharacterUp * JumpSpeed) -
                                                   Vector3.Project(currentVelocity, Motor.CharacterUp);
                                _animatorController.isJump = true;
                                _animatorController.PlayDoubleJumpAnimation();
                                _jumpRequested = false;
                                _doubleJumpConsumed = true;
                                _jumpedThisFrame = true;
                            }
                            else
                            {
                            }
                        }

                        // See if we actually are allowed to jump
                        if ((_canWallJump ||
                             (!_jumpConsumed && (
                                 (AllowJumpingWhenSliding
                                     ? Motor.GroundingStatus.FoundAnyGround
                                     : Motor.GroundingStatus.IsStableOnGround) ||
                                 _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                            && _wallJumpReload <= 0)
                        {
                            // Calculate jump direction before ungrounding
                            Vector3 jumpDirection = Motor.CharacterUp;
                            _animatorController.onGround = false;
                            _animatorController.PlayJumpAnimation();
                            if (_canWallJump)
                            {
                                jumpDirection = _wallJumpNormal + (Motor.CharacterUp * WallJumpVectorUpModifire);
                                _wallRunDirection = Vector3.zero;
                            }
                            else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                            {
                                jumpDirection = Motor.GroundingStatus.GroundNormal;
                                _wallRunDirection = Vector3.zero;
                            }

                            _wallJumpReload = WallJumpReloadTime;
                            // Makes the character skip ground probing/snapping on its next update. 
                            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                            Motor.ForceUnground(0.1f);

                            // Add to the return velocity and reset jump state
                            currentVelocity += (jumpDirection * JumpSpeed) -
                                               Vector3.Project(currentVelocity, Motor.CharacterUp);
                            _jumpRequested = false;
                            _jumpConsumed = true;
                            _jumpedThisFrame = true;
                        }
                    }

                    // Reset wall jump  
                    _canWallJump = false;
                }

                // Take into account additive velocity
                if (_internalVelocityAdd.sqrMagnitude > 0f)
                {
                    currentVelocity += _internalVelocityAdd;
                    _internalVelocityAdd = Vector3.zero;
                }

                _animatorController.velocity = currentVelocity.magnitude;
                break;
            }

            case CharacterState.NoClip:
            {
                float verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);

                // Smoothly interpolate to target velocity
                Vector3 targetMovementVelocity = (_moveInputVector + (Motor.CharacterUp * verticalInput)).normalized *
                                                 (_isSprint ? MaxSprintingSpeed : NoClipMoveSpeed);
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                    1 - Mathf.Exp(-NoClipSharpness * deltaTime));
                break;
            }
            case CharacterState.Dance:
            {
                if (_moveInputVector.magnitude > 0)
                {
                    TransitionToState(CharacterState.Default);
                }

                if (!Motor.GroundingStatus.IsStableOnGround)
                {
                    TransitionToState(CharacterState.Default);
                }

                currentVelocity += Gravity * deltaTime;
                break;
            }
            case CharacterState.AFK:
            {
                currentVelocity = Vector3.zero;
                break;
            }
            case CharacterState.Charging:
            {
                // If we have stopped and need to cancel velocity, do it here
                /*if (_mustStopVelocity)
                {
                    currentVelocity = Vector3.zero;
                    _mustStopVelocity = false;
                }*/

                if (_isStopped)
                {
                    // When stopped, do no velocity handling except gravity
                    currentVelocity += Gravity * deltaTime;
                }
                else
                {
                    // When charging, velocity is always constant
                    //float previousY = currentVelocity.y;
                    currentVelocity = _currentChargeVelocity;
                    //currentVelocity.y = previousY;
                    currentVelocity += Motor.CharacterForward * deltaTime;
                }

                break;
            }

            case CharacterState.Climbing:
            {
                currentVelocity = Vector3.zero;

                switch (_climbingState)
                {
                    case ClimbingState.Climbing:
                        currentVelocity = (_ladderUpDownInput * _activeLadder.transform.up).normalized *
                                          (_isSprint ? MaxClimbingSprintingSpeed : ClimbingSpeed);
                        break;
                    case ClimbingState.Anchoring:
                    case ClimbingState.DeAnchoring:
                        Vector3 tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition,
                            (_anchoringTimer / AnchoringDuration));
                        currentVelocity =
                            Motor.GetVelocityForMovePosition(Motor.TransientPosition, tmpPosition, deltaTime);
                        break;
                }

                _animatorController.velocity = currentVelocity.magnitude;

                break;
            }
            case CharacterState.Swimming:
            {
                float verticalInput = 0f + (_jumpInputIsHeld ? 1f : 0f) + (_crouchInputIsHeld ? -1f : 0f);

                // Smoothly interpolate to target swimming velocity
                Vector3 targetMovementVelocity =
                    (_moveInputVector + (Motor.CharacterUp * verticalInput)).normalized *
                    (_isSprint ? MaxSwimingSprintingSpeed : SwimmingSpeed);
                Vector3 smoothedVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                    1 - Mathf.Exp(-SwimmingMovementSharpness * deltaTime));

                // See if our swimming reference point would be out of water after the movement from our velocity has been applied
                {
                    Vector3 resultingSwimmingReferancePosition = Motor.TransientPosition +
                                                                 (smoothedVelocity * deltaTime) +
                                                                 (SwimmingReferencePoint.position -
                                                                  Motor.TransientPosition);
                    Vector3 closestPointWaterSurface = Physics.ClosestPoint(resultingSwimmingReferancePosition,
                        _waterZone, _waterZone.transform.position, _waterZone.transform.rotation);

                    // if our position would be outside the water surface on next update, project the velocity on the surface normal so that it would not take us out of the water
                    if (closestPointWaterSurface != resultingSwimmingReferancePosition)
                    {
                        /*Vector3 waterSurfaceNormal =
                            (resultingSwimmingReferancePosition - closestPointWaterSurface).normalized;
                        smoothedVelocity = Vector3.ProjectOnPlane(smoothedVelocity, waterSurfaceNormal);*/

                        // Jump out of water
                        if (_jumpRequested)
                        {
                            smoothedVelocity += (Motor.CharacterUp * JumpSpeed) -
                                                Vector3.Project(currentVelocity, Motor.CharacterUp);
                        }
                    }
                }

                currentVelocity = smoothedVelocity;
                break;
            }
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                // Handle jump-related values
                {
                    // Handle jumping pre-ground grace period
                    if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                    {
                        _jumpRequested = false;
                    }

                    if (AllowJumpingWhenSliding
                            ? Motor.GroundingStatus.FoundAnyGround
                            : Motor.GroundingStatus.IsStableOnGround || _canWallJump)
                    {
                        // If we're on a ground surface, reset jumping values
                        if (!_jumpedThisFrame)
                        {
                            _doubleJumpConsumed = false;
                            _jumpConsumed = false;
                        }

                        _timeSinceLastAbleToJump = 0f;
                    }
                    else
                    {
                        // Keep track of time since we were last able to jump (for grace period)
                        _timeSinceLastAbleToJump += deltaTime;
                    }
                }

                // Handle uncrouching
                if (_isCrouching && !_shouldBeCrouching)
                {
                    // Do an overlap test with the character's standing height to see if there are any obstructions
                    Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                    if (Motor.CharacterOverlap(
                            Motor.TransientPosition,
                            Motor.TransientRotation,
                            _probedColliders,
                            Motor.CollidableLayers,
                            QueryTriggerInteraction.Ignore) > 0)
                    {
                        // If obstructions, just stick to crouching dimensions
                        Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                    }
                    else
                    {
                        // If no obstructions, uncrouch
                        MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                        _isCrouching = false;
                    }
                }

                break;
            }
            case CharacterState.AFK:
            {
                break;
            }
            case CharacterState.Charging:
            {
                // Detect being stopped by elapsed time
                if (!_isStopped && _timeSinceStartedCharge > MaxChargeTime)
                {
                    _mustStopVelocity = true;
                    _isStopped = true;
                }

                // Detect end of stopping phase and transition back to default movement state
                if (_timeSinceStopped > StoppedTime)
                {
                    TransitionToState(CharacterState.Default);
                }

                break;
            }
            case CharacterState.Climbing:
            {
                switch (_climbingState)
                {
                    case ClimbingState.Climbing:
                        // Detect getting off ladder during climbing
                        _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                        if (Mathf.Abs(_onLadderSegmentState) > 0.05f)
                        {
                            _climbingState = ClimbingState.DeAnchoring;

                            // If we're higher than the ladder top point
                            if (_onLadderSegmentState > 0)
                            {
                                _ladderTargetPosition = _activeLadder.TopReleasePoint.position;
                                _ladderTargetRotation = _activeLadder.TopReleasePoint.rotation;
                            }
                            // If we're lower than the ladder bottom point
                            else if (_onLadderSegmentState < 0)
                            {
                                _ladderTargetPosition = _activeLadder.BottomReleasePoint.position;
                                _ladderTargetRotation = _activeLadder.BottomReleasePoint.rotation;
                            }
                        }

                        break;
                    case ClimbingState.Anchoring:
                    case ClimbingState.DeAnchoring:
                        // Detect transitioning out from anchoring states
                        if (_anchoringTimer >= AnchoringDuration)
                        {
                            if (_climbingState == ClimbingState.Anchoring)
                            {
                                _climbingState = ClimbingState.Climbing;
                            }
                            else if (_climbingState == ClimbingState.DeAnchoring)
                            {
                                TransitionToState(CharacterState.Default);
                            }
                        }

                        // Keep track of time since we started anchoring
                        _anchoringTimer += deltaTime;
                        break;
                }

                break;
            }
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (IgnoredColliders.Contains(coll))
        {
            return false;
        }

        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                // We can wall jump only if we are not stable on ground and are moving against an obstruction
                if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                {
                    /*_wallJumpNormal = hitNormal;
                    _canWallJump = true;*/
                }

                if (AllowWallJump
                    && hitCollider.CompareTag("SprintWall"))
                {
                    if (_previousWallJumpHitCollider == null ||
                        _previousWallJumpHitCollider.gameObject != hitCollider.gameObject)
                    {
                        _previousWallJumpHitCollider = hitCollider;
                        _wallRunDirection = Vector3.zero;
                    }

                    _wallJumpNormal = hitNormal;
                }

                break;
            }
        }
    }

    public void AddVelocity(Vector3 velocity)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                _internalVelocityAdd += velocity;
                break;
            }
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        switch (CurrentCharacterState)
        {
            case CharacterState.Default:
            {
                if (AllowWallJump
                    && hitCollider.CompareTag("SprintWall")
                    //&& !Motor.GroundingStatus.IsStableOnGround 
                   )
                {
                    _canWallJump = true;
                }
                else
                {
                    _previousWallJumpHitCollider = null;
                    _canWallJump = false;
                }

                break;
            }
        }
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index],
                    transform.TransformPoint(Motor.CharacterTransformToCapsuleCenter), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip,
                transform.TransformPoint(Motor.CharacterTransformToCapsuleCenter), FootstepAudioVolume);
        }
    }
}
