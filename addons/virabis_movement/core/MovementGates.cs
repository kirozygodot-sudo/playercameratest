using System;
using System.Collections.Generic;

namespace Virabis.Movement.Core;

/// <summary>
/// "Ghost Logic" - Logic-Gate tabanlı yetki ve kural sistemi.
/// Minimal kodla esnek yetki yönetimi sağlar.
/// </summary>
public static class MovementGates
{
    public enum GateType
    {
        CanFly,
        CanInfiniteJump,
        CanDash,
        CanGrapple,
        IsInvulnerable
    }

    private static readonly Dictionary<GateType, Func<bool>> _gates = new();

    public static void SetGate(GateType type, Func<bool> condition)
    {
        _gates[type] = condition;
    }

    public static bool Check(GateType type)
    {
        if (_gates.TryGetValue(type, out var condition))
        {
            return condition?.Invoke() ?? false;
        }
        return false;
    }
}
