using Godot;
using System.Numerics;
using Virabis.Movement.Core;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Bridge;

/// <summary>
/// Godot ↔ Core adapter.
///
/// KURAL: Kamerayı bilmez. Input okumaz.
/// PlayerController.gd → SetInputDirection() + SetSprinting() + RequestJump() çağırır.
/// AI da aynı üç metodu çağırarak aynı physics'i kullanır.
///
/// Gravity: _Ready'de init edilir (field init'te Godot project settings
/// henüz yüklü olmayabilir — toplantı kararı).
/// </summary>
public partial class MovementNode : Node
{
    [Export] public CharacterBody3D Character    { get; set; } = null!;
    [Export] public float           JumpForce    { get; set; } = 5.5f;
    [Export] public float           GravityScale { get; set; } = 1.0f;

    // ── Core ──────────────────────────────────────────────────────────────────
    private MovementSystem _system = null!;

    // ── Vertical (Bridge yönetir, Core bilmez) ───────────────────────────────
    private float _verticalVelocity;
    private float _gravity;   // _Ready'de set edilir

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

        ApplyVertical(dt);

        var ctx = new MovementContext
        {
            InputDirection  = new System.Numerics.Vector3(_inputDir.X, 0f, _inputDir.Z),
            CurrentVelocity = new System.Numerics.Vector3(Character.Velocity.X, 0f, Character.Velocity.Z),
            IsOnFloor       = Character.IsOnFloor(),
            IsSprinting     = _sprinting,
            IsFlying        = _flying,
            JumpRequested   = _jumpThisFrame,
            JumpsRemaining  = _system.JumpsRemaining,
            DeltaTime       = dt
        };

        var result = _system.Update(ctx);

        if (result.JumpConsumed)
            _verticalVelocity = JumpForce;

        if (result.DisableGravity)
            _verticalVelocity = 0f;

        Character.Velocity = new Godot.Vector3(
            result.NewVelocity.X,
            _verticalVelocity,
            result.NewVelocity.Z
        );
        Character.MoveAndSlide();

        _jumpThisFrame = false;  // single-frame flag reset

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

        if (!Character.IsOnFloor())
            _verticalVelocity -= _gravity * GravityScale * dt;
        else if (_verticalVelocity < 0f)
            _verticalVelocity = 0f;
    }
}
