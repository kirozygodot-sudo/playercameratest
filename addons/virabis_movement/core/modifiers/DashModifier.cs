using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

public sealed class DashModifier : IMovementModifier
{
    private readonly Vector3 _direction;
    private readonly float   _speed;
    private          float   _duration;
    private          bool    _isExpired;
    private          bool    _started;

    public DashModifier(Vector3 direction, float speed, float duration)
    {
        _direction = direction.LengthSquared() > 0.01f ? Vector3.Normalize(direction) : Vector3.Zero;
        _speed     = speed;
        _duration  = duration;
    }

    public bool IsExpired => _isExpired;
    public string DebugLabel => $"Dash({_duration:F2}s)";

    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        if (!_started)
        {
            _started = true;
            MovementEvents.Emit(MovementEvents.EventType.DashStarted, ctx);
        }

        if (_duration <= 0f)
        {
            if (!_isExpired)
            {
                _isExpired = true;
                MovementEvents.Emit(MovementEvents.EventType.DashEnded, ctx);
            }
            return velocity;
        }

        _duration -= ctx.DeltaTime;
        return _direction * _speed;
    }
}
