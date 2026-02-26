using System.Numerics;

namespace Virabis.Movement.Core;

/// <summary>
/// Core'un her frame ürettiği çıktı. Bridge bunu Godot'a uygular.
/// Vertical velocity (Y) burada yok — Bridge yönetir.
/// </summary>
public readonly record struct MovementResult
{
    /// <summary>Hesaplanmış yatay velocity. Y her zaman 0.</summary>
    public Vector3       NewVelocity    { get; init; }

    public MovementState State          { get; init; }

    /// <summary>True → Bridge JumpsRemaining azaltır ve jump force uygular.</summary>
    public bool          JumpConsumed   { get; init; }

    /// <summary>True (Flying state) → Bridge bu frame gravity uygulamaz.</summary>
    public bool          DisableGravity { get; init; }
}
