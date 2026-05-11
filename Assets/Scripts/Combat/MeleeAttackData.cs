using System;
using UnityEngine;

[Serializable]
public class MeleeAttackData
{
    [Min(0)] public int Damage = 1;
    [Min(0f)] public float Startup = 0.08f;
    [Min(0f)] public float ActiveTime = 0.10f;
    [Min(0f)] public float Recovery = 0.22f;
    [Min(0f)] public float Radius = 0.85f;
    [Min(0f)] public float ForwardOffset = 1.1f;
    [Min(0f)] public float ForwardLunge = 1.5f;
    [Min(0f)] public float KnockbackForce = 2.5f;
    public bool RequireGrounded = false;
    public LayerMask TargetLayers = ~0;
    public AudioClip SwingSfx;
    public AudioClip HitSfx;
    public GameObject HitVfx;

    public float TotalDuration => Startup + ActiveTime + Recovery;
}
