using Godot;
using System.Numerics;
using Virabis.Movement.Core;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Bridge;

/// <summary>
/// Godot ↔ Core adapter. GDScript'ten erişilebilir.
/// </summary>
[GlobalClass]
public partial class MovementNode : Node
{
    [Export] public CharacterBody3D Character    { get; set; } = null!;
    [Export] public float           JumpForce    { get; set; } = 5.5f;
    [Export] public float           GravityScale { get; set; } = 1.0f;
    [Export] public float           CoyoteTime   { get; set; } = 0.1f;
    [Export] public float           JumpBuffer   { get; set; } = 0.1f;
    [Export] public float           GroundSlamGravity { get; set; } = 3.0f;  // Ground slam çarpan

    // ── Core ──────────────────────────────────────────────────────────────────
    private MovementSystem _system = null!;

    // ── Vertical (Bridge yönetir, Core bilmez) ───────────────────────────────
    private float _verticalVelocity;
    private float _gravity;   // _Ready'de set edilir

    // ── Coyote Time & Jump Buffer ────────────────────────────────────────────
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool  _wasOnFloor;

    // ── Ground Slam ───────────────────────────────────────────────────────────
    private bool _groundSlamRequested;

    // ── Input (PlayerController veya AI set eder) ────────────────────────────
    private Godot.Vector3 _inputDir;
    private bool          _sprinting;
    private bool          _flying;
    private bool          _jumpThisFrame;

    // ── Enable/disable (araç biniş) ──────────────────────────────────────────
    private bool _enabled = true;

    // ── Signals ───────────────────────────────────────────────────────────────
    [Signal] public delegate void StateChangedEventHandler(int newState);
    private MovementState _lastState = MovementState.Idle;

    // ─────────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _system  = new MovementSystem(MovementConfig.Default);
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_enabled || Character is null) return;

        float dt = (float)delta;

        // Coyote Time: yerde olmadan önceki kısa sürede zıplama izni
        bool isOnFloor = Character.IsOnFloor();
        if (isOnFloor)
        {
            _coyoteTimer = CoyoteTime;
            _wasOnFloor = true;
        }
        else if (_wasOnFloor)
        {
            _coyoteTimer -= dt;
            if (_coyoteTimer <= 0f) _wasOnFloor = false;
        }

        // Jump Buffer: zıplama tuşuna basıldığında kısa süre bekle
        if (_jumpThisFrame)
        {
            _jumpBufferTimer = JumpBuffer;
            _jumpThisFrame = false;
        }
        else if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= dt;
        }

        ApplyVertical(dt);

        // Coyote time aktifse veya yerdeyse zıplama mümkün
        bool canJump = isOnFloor || _coyoteTimer > 0f;
        bool jumpRequested = _jumpBufferTimer > 0f && canJump;

        var ctx = new MovementContext
        {
            InputDirection  = new System.Numerics.Vector3(_inputDir.X, 0f, _inputDir.Z),
            CurrentVelocity = new System.Numerics.Vector3(Character.Velocity.X, 0f, Character.Velocity.Z),
            IsOnFloor       = isOnFloor,
            IsSprinting     = _sprinting,
            IsFlying        = _flying,
            JumpRequested   = jumpRequested,
            JumpsRemaining  = _system.JumpsRemaining,
            DeltaTime       = dt
        };

        var result = _system.Update(ctx);

        if (result.JumpConsumed)
        {
            _verticalVelocity = JumpForce;
            _jumpBufferTimer = 0f;  // Buffer'ı temizle
            _coyoteTimer = 0f;      // Coyote time'ı temizle
        }

        if (result.DisableGravity)
            _verticalVelocity = 0f;

        Character.Velocity = new Godot.Vector3(
            result.NewVelocity.X,
            _verticalVelocity,
            result.NewVelocity.Z
        );
        Character.MoveAndSlide();

        if (result.State != _lastState)
        {
            _lastState = result.State;
            EmitSignal(SignalName.StateChanged, (int)result.State);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INPUT API — PlayerController.gd veya AI çağırır
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Camera-relative, normalize edilmiş yatay yön. PlayerController hesaplar.</summary>
    public void SetInputDirection(Godot.Vector3 direction) => _inputDir = direction;

    public void SetSprinting(bool value) => _sprinting = value;
    public void SetFlying(bool value)    => _flying    = value;

    /// <summary>Single-frame jump. PlayerController input algılayınca çağırır.</summary>
    public void RequestJump() => _jumpThisFrame = true;

    /// <summary>Ground Slam - hızlı yere iniş. PlayerController input algılayınca çağırır.</summary>
    public void RequestGroundSlam() => _groundSlamRequested = true;

    public bool IsFlying() => _flying;

    // ─────────────────────────────────────────────────────────────────────────
    // ENABLE / DISABLE — Araça/mount'a biniş
    // ─────────────────────────────────────────────────────────────────────────

    public void SetEnabled(bool value)
    {
        _enabled = value;
        if (!value && Character is not null)
        {
            Character.Velocity = Godot.Vector3.Zero;
            _verticalVelocity  = 0f;
            _jumpThisFrame     = false;
        }
    }
    public bool IsEnabled() => _enabled;

    // ─────────────────────────────────────────────────────────────────────────
    // MODIFIER API
    // ─────────────────────────────────────────────────────────────────────────

    public void ApplySlow(float multiplier, float duration)
        => _system.AddModifier(new SlowModifier(multiplier, duration));

    public void ApplyStun(float duration)
        => _system.AddModifier(new StunModifier(duration));

    public void ApplyKnockback(Godot.Vector3 force, float decay = 10f)
        => _system.AddModifier(new KnockbackModifier(
               new System.Numerics.Vector3(force.X, force.Y, force.Z), decay));

    public void ApplyDash(Godot.Vector3 dir, float speed, float duration)
        => _system.AddModifier(new DashModifier(
               new System.Numerics.Vector3(dir.X, dir.Y, dir.Z), speed, duration));

    public void ApplySpeedBoost(float multiplier, float duration)
        => _system.AddModifier(new SpeedBoostModifier(multiplier, duration));

    // ── ADVANCED MECHANICS API ────────────────────────────────────────────────
    // 10 yeni uzman mekanik için API'ler

    /// <summary>Momentum Vaulting - yüksek hızla engel aşma</summary>
    public void ApplyVault(float minSpeed, float maxHeight, float duration, float boost = 1.2f)
        => _system.AddModifier(new VaultModifier(minSpeed, maxHeight, duration, boost));

    /// <summary>Slide Melee Combo - kayma + saldırı</summary>
    public void ApplySlide(float friction = 3f, float maxDuration = 2f, float exitMomentum = 0.6f,
        float attackDamageMult = 2f, float attackSpeedBoost = 1.5f)
        => _system.AddModifier(new SlideModifier(friction, maxDuration, exitMomentum, 
            attackDamageMult, attackSpeedBoost));

    /// <summary>Grapple Hook - ip fizikçisi</summary>
    public void ApplyGrapple(float springStrength = 150f, float damping = 8f, 
        float maxLength = 30f, float minLength = 2f, float pullSpeed = 15f, float launchBoost = 1.5f)
        => _system.AddModifier(new GrappleModifier(springStrength, damping, maxLength, 
            minLength, pullSpeed, launchBoost));

    /// <summary>Time Dilation - low health slow-mo</summary>
    public void ApplyTimeDilation(float healthThreshold = 0.25f, float slowScale = 0.5f, 
        float duration = 2f, float cooldown = 30f)
        => _system.AddModifier(new TimeDilationModifier(healthThreshold, slowScale, duration, cooldown));

    /// <summary>Recoil Propulsion - silah itkisi</summary>
    public void ApplyRecoil(float knockbackForce = 8f, float airControlImmunity = 0.1f, 
        float verticalBias = 0.3f, float decay = 5f)
        => _system.AddModifier(new RecoilModifier(knockbackForce, airControlImmunity, 
            verticalBias, decay));

    /// <summary>Wall Jump Combo - duvar zıplama + chain</summary>
    public void ApplyWallJump(float bounceForce = 12f, float maxWallAngle = 30f, 
        float airControlBonus = 1.5f, int comboResetCount = 3, float comboWindow = 1.5f)
        => _system.AddModifier(new WallJumpModifier(bounceForce, maxWallAngle, airControlBonus, 
            comboResetCount, comboWindow));

    /// <summary>Crouch Jump - %150 zıplama</summary>
    public void ApplyCrouchJump(float heightMultiplier = 1.5f, float timingWindow = 0.1f, 
        float chargeTime = 0.3f, float minChargePercent = 0.3f)
        => _system.AddModifier(new CrouchJumpModifier(heightMultiplier, timingWindow, 
            chargeTime, minChargePercent));

    /// <summary>Explosion Boost - patlama itki</summary>
    public void ApplyExplosionBoost(float radius = 5f, float maxForce = 20f, 
        float airControlImmunity = 0.3f, float damageThreshold = 10f)
        => _system.AddModifier(new ExplosionBoostModifier(radius, maxForce, 
            airControlImmunity, damageThreshold));

    /// <summary>ADS Glide - nişan kayması</summary>
    public void ApplyADSGlide(float friction = 2f, float momentumRetention = 0.9f, 
        float minSpeed = 6f, float maxDuration = 1.5f, float aimSensitivityMult = 0.6f)
        => _system.AddModifier(new ADSGlideModifier(friction, momentumRetention, minSpeed, 
            maxDuration, aimSensitivityMult));

    /// <summary>Air Momentum Transfer - dash→jump combo</summary>
    public void ApplyAirMomentumTransfer(float momentumRetention = 0.85f, float chainMultiplier = 1.2f, 
        float transferWindow = 0.25f, int maxChainCount = 3)
        => _system.AddModifier(new AirMomentumTransferModifier(momentumRetention, chainMultiplier, 
            transferWindow, maxChainCount));

    public void ClearAllModifiers() => _system.ClearModifiers();

    // ─────────────────────────────────────────────────────────────────────────
    // CONFIG
    // ─────────────────────────────────────────────────────────────────────────

    public void           SetConfig(MovementConfig cfg) => _system.SetConfig(cfg);
    public MovementConfig GetConfig()                   => _system.GetConfig();

    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────────────────────────────────

    public Godot.Collections.Dictionary GetDebugInfo()
    {
        var modifiers = new Godot.Collections.Array();
        foreach (var label in _system.GetModifierLabels())
            modifiers.Add(label);

        return new Godot.Collections.Dictionary
        {
            ["state"]           = _system.CurrentState.ToString(),
            ["jumps_remaining"] = _system.JumpsRemaining,
            ["is_flying"]       = _flying,
            ["enabled"]         = _enabled,
            ["modifier_count"]  = modifiers.Count,
            ["modifiers"]       = modifiers
        };
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyVertical(float dt)
    {
        if (_flying)  { _verticalVelocity = 0f; return; }

        // Ground Slam: hızlı düşüş
        if (_groundSlamRequested && !Character.IsOnFloor())
        {
            _verticalVelocity -= _gravity * GroundSlamGravity * dt;
            if (Character.IsOnFloor())
            {
                _groundSlamRequested = false;  // Yere değince bitir
            }
        }
        else if (!Character.IsOnFloor())
            _verticalVelocity -= _gravity * GravityScale * dt;
        else if (_verticalVelocity < 0f)
            _verticalVelocity = 0f;
    }
}
