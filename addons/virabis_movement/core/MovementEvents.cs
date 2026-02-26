using System;
using System.Collections.Generic;

namespace Virabis.Movement.Core;

/// <summary>
/// "Kinetic Chain" - Mekanikler arası hafif event bus sistemi.
/// </summary>
public static class MovementEvents
{
    public enum EventType
    {
        DashStarted,
        DashEnded,
        SlideStarted,
        SlideEnded,
        GrappleStarted,
        GrappleReleased,
        JumpPerformed,
        LandPerformed
    }

    private static readonly Dictionary<EventType, Action<MovementContext>> _listeners = new();

    public static void Subscribe(EventType type, Action<MovementContext> listener)
    {
        if (!_listeners.ContainsKey(type)) _listeners[type] = null;
        _listeners[type] += listener;
    }

    public static void Unsubscribe(EventType type, Action<MovementContext> listener)
    {
        if (_listeners.ContainsKey(type)) _listeners[type] -= listener;
    }

    public static void Emit(EventType type, MovementContext ctx)
    {
        if (_listeners.TryGetValue(type, out var action))
        {
            action?.Invoke(ctx);
        }
    }
}
