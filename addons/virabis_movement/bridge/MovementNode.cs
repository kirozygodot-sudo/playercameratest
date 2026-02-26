using Godot;
using System.Numerics;
using System.Linq;
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
    [Export] public float           GroundSlamGravity { get; set; } = 3.0f;

    // ── Core ──────────────────────────────────────────────────────────────────
    [Export] public MovementConfig Config { get; set; } = null!;
    private IMovementSystem _system = null!;
    private SudoMovementDecorator _sudoDecorator = null!;

    // ── Vertical ──────────────────────────────────────────────────────────────
    private float _verticalVelocity;
    private float _gravity;

    // ── Coyote Time & Jump Buffer ────────────────────────────────────────────
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool  _wasOnFloor;

    // ── Ground Slam ───────────────────────────────────────────────────────────
    private bool _groundSlamRequested;

    // ── Input ────────────────────────────────────────────────────────────────
    private Godot.Vector3 _inputDir;
    private bool          _sprinting;
    private bool          _flying;
    private bool          _jumpThisFrame;

    private bool _enabled = true;

    [Signal] public delegate void StateChangedEventHandler(int newState);
    private MovementState _lastState = MovementState.Idle;

    public override void _Ready()
    {
        var coreSystem = new MovementSystem(Config);
        _sudoDecorator = new SudoMovementDecorator(coreSystem);
        _system = _sudoDecorator; // Varsayılan olarak decorator üzerinden çalışır
        
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_enabled || Character is null) return;

        float dt = (float)delta;

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

        bool canJump = isOnFloor || _coyoteTimer > 0f || _sudoDecorator.InfiniteJumps;
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
            DeltaTime       = dt,
            CurrentPosition = new System.Numerics.Vector3(Character.GlobalPosition.X, Character.GlobalPosition.Y, Character.GlobalPosition.Z)
        };

        var result = _system.Update(ctx);

        if (result.JumpConsumed)
        {
            _verticalVelocity = JumpForce;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }

        if (result.DisableGravity)
            _verticalVelocity = 0f;

        Character.Velocity = new Godot.Vector3(
            result.NewVelocity.X,
            _verticalVelocity,
            result.NewVelocity.Z
        );
        
        // NoClip durumunda collision kapatılabilir (GDScript tarafında da yönetilebilir)
        Character.MoveAndSlide();

        if (result.State != _lastState)
        {
            _lastState = result.State;
            EmitSignal(SignalName.StateChanged, (int)result.State);
        }
    }

    // ── SUDO API ─────────────────────────────────────────────────────────────
    public void SetGodMode(bool value) => _sudoDecorator.GodMode = value;
    public void SetInfiniteJumps(bool value) => _sudoDecorator.InfiniteJumps = value;
    public void SetNoClip(bool value) => _sudoDecorator.NoClip = value;
    public void SetSpeedMultiplier(float value) => _sudoDecorator.SpeedMultiplier = value;

    // ── REST OF API ──────────────────────────────────────────────────────────
    public void SetInputDirection(Godot.Vector3 direction) => _inputDir = direction;
    public void SetSprinting(bool value) => _sprinting = value;
    public void SetFlying(bool value)    => _flying    = value;
    public void RequestJump() => _jumpThisFrame = true;
    public void RequestGroundSlam() => _groundSlamRequested = true;
    public bool IsFlying() => _flying;

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

    public void ApplySlow(float multiplier, float duration) => _system.AddModifier(new SlowModifier(multiplier, duration));
    public void ApplyStun(float duration) => _system.AddModifier(new StunModifier(duration));
    public void ApplyDash(Godot.Vector3 dir, float speed, float duration)
        => _system.AddModifier(new DashModifier(new System.Numerics.Vector3(dir.X, dir.Y, dir.Z), speed, duration));

    public void ApplyGrapple(Godot.Vector3 anchorPoint, float springStrength = 150f, float damping = 8f, 
        float maxLength = 30f, float minLength = 2f, float pullSpeed = 15f, float launchBoost = 1.5f)
    {
        var modifier = new GrappleModifier(springStrength, damping, maxLength, minLength, pullSpeed, launchBoost);
        if (_system.AddModifier(modifier))
        {
            modifier.StartGrapple(new System.Numerics.Vector3(anchorPoint.X, anchorPoint.Y, anchorPoint.Z), 
                                  new System.Numerics.Vector3(Character.GlobalPosition.X, Character.GlobalPosition.Y, Character.GlobalPosition.Z));
        }
    }

    public void ApplySlide(float friction = 3f, float maxDuration = 2f, float exitMomentum = 0.6f,
        float attackDamageMult = 2f, float attackSpeedBoost = 1.5f)
    {
        var modifier = new SlideModifier(friction, maxDuration, exitMomentum, attackDamageMult, attackSpeedBoost);
        if (_system.AddModifier(modifier))
        {
            modifier.StartSlide(
                new System.Numerics.Vector3(Character.Velocity.X, 0f, Character.Velocity.Z),
                new System.Numerics.Vector3(Character.Velocity.X, 0f, Character.Velocity.Z).Length()
            );
        }
    }

    public void ReleaseGrapple()
    {
        foreach (var modifier in _system.GetModifiers())
        {
            if (modifier is GrappleModifier grappleModifier)
            {
                grappleModifier.ReleaseGrapple();
                break;
            }
        }
    }

    public void SetConfig(MovementConfig cfg) => _system.SetConfig(cfg);
    public MovementConfig GetConfig() => _system.GetConfig();

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

    private void ApplyVertical(float dt)
    {
        if (_flying || _sudoDecorator.NoClip)  { _verticalVelocity = 0f; return; }

        if (_groundSlamRequested && !Character.IsOnFloor())
        {
            _verticalVelocity -= _gravity * GroundSlamGravity * dt;
            if (Character.IsOnFloor()) _groundSlamRequested = false;
        }
        else if (!Character.IsOnFloor())
            _verticalVelocity -= _gravity * GravityScale * dt;
        else if (_verticalVelocity < 0f)
            _verticalVelocity = 0f;
    }
}
