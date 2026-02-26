using System;
using System.Numerics;
using System.Collections.Generic;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Core;

/// <summary>
/// Movement Core — Pure C#. Godot yok. Gravity yok. Node yok.
///
/// Pipeline (her frame, bu sırayla):
///   1. State belirle
///   2. Hedef hız hesapla
///   3. Acceleration uygula
///   4. Turn penalty uygula  (Max Payne hissi)
///   5. Friction uygula
///   6. Jump consume
///   7. Modifier pipeline   (expired temizle → uygula)
///   8. Max speed clamp
///   9. Sonuç döndür
/// </summary>
public sealed class MovementSystem
{
    private MovementConfig                  _config;
    private readonly List<IMovementModifier> _modifiers = new(8);

    public MovementState CurrentState   { get; private set; } = MovementState.Idle;
    public int           JumpsRemaining { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────

    public MovementSystem(MovementConfig? config = null)
    {
        _config        = config ?? MovementConfig.Default;
        JumpsRemaining = _config.MaxJumps;
    }

    public void           SetConfig(MovementConfig cfg)
    {
        _config = cfg;
        // JumpsRemaining korunur ama negatife düşmez
        JumpsRemaining = Math.Max(JumpsRemaining, 0);
    }
    public MovementConfig GetConfig() => _config;

    // ─────────────────────────────────────────────────────────────────────────
    // ANA UPDATE
    // ─────────────────────────────────────────────────────────────────────────

    public MovementResult Update(MovementContext ctx)
    {
        // Yere değince jump hakkını resetle
        if (ctx.IsOnFloor)
            JumpsRemaining = _config.MaxJumps;

        var   state    = DetermineState(ctx);
        CurrentState   = state;

        float targetSpeed = GetTargetSpeed(state);
        var   velocity    = ComputeVelocity(ctx, state, targetSpeed);

        bool jumpConsumed = false;
        if (ctx.JumpRequested && JumpsRemaining > 0)
        {
            JumpsRemaining--;
            jumpConsumed = true;
            // Yatay velocity korunur — vertical (jump force) Bridge ekler
        }

        velocity = ApplyModifierPipeline(velocity, ctx);
        velocity = ClampSpeed(velocity);

        return new MovementResult
        {
            NewVelocity    = velocity,
            State          = state,
            JumpConsumed   = jumpConsumed,
            DisableGravity = state == MovementState.Flying
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODIFIER API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>False döndürürse MaxModifiers dolmuştur.</summary>
    public bool AddModifier(IMovementModifier modifier)
    {
        if (_modifiers.Count >= _config.MaxModifiers) return false;
        _modifiers.Add(modifier);
        return true;
    }

    public void RemoveModifier(IMovementModifier modifier) => _modifiers.Remove(modifier);
    public void ClearModifiers()                           => _modifiers.Clear();
    public bool HasModifier<T>() where T : IMovementModifier
        => _modifiers.Exists(m => m is T);

    /// <summary>Debug overlay için aktif modifier etiketleri.</summary>
    public IReadOnlyList<string> GetModifierLabels()
    {
        var labels = new List<string>(_modifiers.Count);
        foreach (var m in _modifiers) labels.Add(m.DebugLabel);
        return labels;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE — Pipeline adımları (her biri tek sorumluluk)
    // ─────────────────────────────────────────────────────────────────────────

    private MovementState DetermineState(MovementContext ctx)
    {
        if (ctx.IsFlying)                               return MovementState.Flying;
        if (!ctx.IsOnFloor)                             return MovementState.Airborne;
        if (ctx.InputDirection.LengthSquared() < 1e-4f) return MovementState.Idle;
        if (ctx.IsSprinting)                            return MovementState.Sprinting;
        return MovementState.Moving;
    }

    private float GetTargetSpeed(MovementState state) => state switch
    {
        MovementState.Sprinting => _config.SprintSpeed,
        MovementState.Moving    => _config.WalkSpeed,
        MovementState.Airborne  => _config.WalkSpeed,
        MovementState.Flying    => _config.FlyingSpeed,
        _                       => 0f
    };

    private Vector3 ComputeVelocity(MovementContext ctx, MovementState state, float targetSpeed)
    {
        var current = ctx.CurrentVelocity;

        if (ctx.InputDirection.LengthSquared() < 1e-4f)
            return ApplyFriction(current, ctx);

        float accel  = GetAcceleration(ctx, state);
        accel        = ApplyTurnPenalty(accel, current, ctx.InputDirection, state);

        var target   = ctx.InputDirection * targetSpeed;
        return MoveToward(current, target, accel * ctx.DeltaTime);
    }

    private float GetAcceleration(MovementContext ctx, MovementState state)
    {
        if (state == MovementState.Flying) return _config.AirAcceleration;
        return ctx.IsOnFloor
            ? _config.GroundAcceleration
            : _config.AirAcceleration * _config.AirControlMultiplier;
    }

    /// <summary>
    /// Ani 180° dönüşte acceleration azaltır — Max Payne "direction commitment" hissi.
    /// Sprint'te ek ceza (risk-reward).
    /// </summary>
    private float ApplyTurnPenalty(float accel, Vector3 current, Vector3 inputDir, MovementState state)
    {
        if (current.LengthSquared() < 0.01f) return accel;

        float dot     = Vector3.Dot(Vector3.Normalize(current), inputDir);
        float penalty = dot < -0.5f ? _config.TurnResponsiveness : 1.0f;
        if (state == MovementState.Sprinting) penalty /= _config.SprintTurnMultiplier;

        return accel / penalty;
    }

    private Vector3 ApplyFriction(Vector3 velocity, MovementContext ctx)
    {
        float friction = ctx.IsOnFloor ? _config.GroundFriction : _config.AirFriction;
        return MoveToward(velocity, Vector3.Zero, friction * ctx.DeltaTime);
    }

    private Vector3 ApplyModifierPipeline(Vector3 velocity, MovementContext ctx)
    {
        _modifiers.RemoveAll(m => m.IsExpired);
        foreach (var mod in _modifiers)
            velocity = mod.ModifyVelocity(velocity, ctx);
        return velocity;
    }

    private Vector3 ClampSpeed(Vector3 velocity)
    {
        float speed = velocity.Length();
        return speed > _config.MaxSpeedCap
            ? Vector3.Normalize(velocity) * _config.MaxSpeedCap
            : velocity;
    }

    // Godot move_toward'ın C# karşılığı
    private static Vector3 MoveToward(Vector3 from, Vector3 to, float delta)
    {
        var   diff = to - from;
        float dist = diff.Length();
        if (dist <= delta || dist < 1e-4f) return to;
        return from + diff / dist * delta;
    }
}
