using Godot;
using Virabis.Movement.Core;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.GodotBridge;

/// <summary>
/// C# Modifier'ları GDScript'ten erişilebilir yapar.
/// </summary>
[GlobalClass]
public partial class MovementBridge : Node
{
    [Export] public NodePath MovementNodePath { get; set; }
    
    private MovementNode _movement;
    
    public override void _Ready()
    {
        _movement = GetNode<MovementNode>(MovementNodePath);
    }
    
    // ── VAULT ───────────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyVault(float minSpeed, float maxHeight, float duration, float boost = 1.2f)
    {
        _movement.ApplyVault(minSpeed, maxHeight, duration, boost);
    }
    
    // ── SLIDE ───────────────────────────────────────────────────────────────
    [Rpc]
    public void ApplySlide(float friction = 3f, float maxDuration = 2f, float exitMomentum = 0.6f,
        float attackDamageMult = 2f, float attackSpeedBoost = 1.5f)
    {
        _movement.ApplySlide(friction, maxDuration, exitMomentum, attackDamageMult, attackSpeedBoost);
    }
    
    // ── GRAPPLE ─────────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyGrapple(Vector3 anchorPoint, float springStrength = 150f, float damping = 8f,
        float maxLength = 30f, float minLength = 2f, float pullSpeed = 15f, float launchBoost = 1.5f)
    {
        _movement.ApplyGrapple(springStrength, damping, maxLength, minLength, pullSpeed, launchBoost);
        // Anchor point should be set in GrappleModifier via a different mechanism
    }
    
    // ── TIME DILATION ───────────────────────────────────────────────────────
    [Rpc]
    public void ApplyTimeDilation(float healthThreshold = 0.25f, float slowScale = 0.5f,
        float duration = 2f, float cooldown = 30f)
    {
        _movement.ApplyTimeDilation(healthThreshold, slowScale, duration, cooldown);
    }
    
    // ── RECOIL ───────────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyRecoil(Vector3 weaponDirection, float knockbackForce = 8f, 
        float airControlImmunity = 0.1f, float verticalBias = 0.3f, float decay = 5f)
    {
        _movement.ApplyRecoil(knockbackForce, airControlImmunity, verticalBias, decay);
    }
    
    // ── WALL JUMP ───────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyWallJump(float bounceForce = 12f, float maxWallAngle = 30f,
        float airControlBonus = 1.5f, int comboResetCount = 3, float comboWindow = 1.5f)
    {
        _movement.ApplyWallJump(bounceForce, maxWallAngle, airControlBonus, comboResetCount, comboWindow);
    }
    
    // ── CROUCH JUMP ─────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyCrouchJump(float heightMultiplier = 1.5f, float timingWindow = 0.1f,
        float chargeTime = 0.3f, float minChargePercent = 0.3f)
    {
        _movement.ApplyCrouchJump(heightMultiplier, timingWindow, chargeTime, minChargePercent);
    }
    
    // ── EXPLOSION BOOST ─────────────────────────────────────────────────────
    [Rpc]
    public void ApplyExplosionBoost(Vector3 explosionCenter, float explosionDamage,
        float radius = 5f, float maxForce = 20f, float airControlImmunity = 0.3f, 
        float damageThreshold = 10f, bool isFriendly = false)
    {
        _movement.ApplyExplosionBoost(radius, maxForce, airControlImmunity, damageThreshold);
    }
    
    // ── ADS GLIDE ────────────────────────────────────────────────────────────
    [Rpc]
    public void ApplyADSGlide(float friction = 2f, float momentumRetention = 0.9f,
        float minSpeed = 6f, float maxDuration = 1.5f, float aimSensitivityMult = 0.6f)
    {
        _movement.ApplyADSGlide(friction, momentumRetention, minSpeed, maxDuration, aimSensitivityMult);
    }
    
    // ── AIR MOMENTUM TRANSFER ────────────────────────────────────────────────
    [Rpc]
    public void ApplyAirMomentumTransfer(float momentumRetention = 0.85f, float chainMultiplier = 1.2f,
        float transferWindow = 0.25f, int maxChainCount = 3)
    {
        _movement.ApplyAirMomentumTransfer(momentumRetention, chainMultiplier, transferWindow, maxChainCount);
    }
    
    // ── DEBUG INFO ───────────────────────────────────────────────────────────
    public Godot.Collections.Dictionary GetAdvancedDebugInfo()
    {
        return _movement.GetDebugInfo();
    }
}
