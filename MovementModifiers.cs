using System;
using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── SLOW ─────────────────────────────────────────────────────────────────────
/// <summary>Hızı belirli süre düşürür. Örn: burning debuff, mud terrain.</summary>
public sealed class SlowModifier : IMovementModifier
{
    private readonly float _multiplier;
    private float          _remaining;

    public SlowModifier(float multiplier, float duration)
    {
        _multiplier = Math.Clamp(multiplier, 0f, 1f);
        _remaining  = duration;
    }

    public bool   IsExpired  => _remaining <= 0f;
    public string DebugLabel => $"Slow({_multiplier:F1}x, {_remaining:F1}s)";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        _remaining -= ctx.DeltaTime;
        return velocity * _multiplier;
    }
}

// ── STUN ─────────────────────────────────────────────────────────────────────
/// <summary>Süre boyunca tüm yatay hareketi sıfırlar.</summary>
public sealed class StunModifier : IMovementModifier
{
    private float _remaining;

    public StunModifier(float duration) => _remaining = duration;

    public bool   IsExpired  => _remaining <= 0f;
    public string DebugLabel => $"Stun({_remaining:F1}s)";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        _remaining -= ctx.DeltaTime;
        return Vector3.Zero;
    }
}

// ── KNOCKBACK ────────────────────────────────────────────────────────────────
/// <summary>Dış kuvvet ekler. Decay ile zamanla söner.</summary>
public sealed class KnockbackModifier : IMovementModifier
{
    private Vector3      _force;
    private readonly float _decay;
    private const float    _threshold = 0.05f;

    public KnockbackModifier(Vector3 force, float decay = 10f)
    {
        _force = force;
        _decay = decay;
    }

    public bool   IsExpired  => _force.Length() < _threshold;
    public string DebugLabel => $"Knockback({_force.Length():F1})";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        var result = velocity + _force;
        _force = Vector3.Lerp(_force, Vector3.Zero, _decay * ctx.DeltaTime);
        return result;
    }
}

// ── DASH ─────────────────────────────────────────────────────────────────────
/// <summary>Kısa süre velocity override eder. Bitti → kısmi momentum bırakır.</summary>
public sealed class DashModifier : IMovementModifier
{
    private readonly Vector3 _dashVel;
    private float            _remaining;
    private readonly float   _exitMomentum;

    public DashModifier(Vector3 direction, float speed, float duration, float exitMomentum = 0.4f)
    {
        _dashVel      = Vector3.Normalize(direction) * speed;
        _remaining    = duration;
        _exitMomentum = exitMomentum;
    }

    public bool   IsExpired  => _remaining <= 0f;
    public string DebugLabel => $"Dash({_remaining:F2}s)";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        _remaining -= ctx.DeltaTime;
        return _remaining > 0f ? _dashVel : _dashVel * _exitMomentum;
    }
}

// ── SPEED BOOST ──────────────────────────────────────────────────────────────
/// <summary>Hızı artırır. Powerup, ability reward, rune vb.</summary>
public sealed class SpeedBoostModifier : IMovementModifier
{
    private readonly float _multiplier;
    private float          _remaining;

    public SpeedBoostModifier(float multiplier, float duration)
    {
        _multiplier = multiplier;
        _remaining  = duration;
    }

    public bool   IsExpired  => _remaining <= 0f;
    public string DebugLabel => $"SpeedBoost({_multiplier:F1}x, {_remaining:F1}s)";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        _remaining -= ctx.DeltaTime;
        return velocity * _multiplier;
    }
}
