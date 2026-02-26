using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

/// <summary>
/// Modifier pipeline contract.
///
/// Pipeline temel velocity hesabından SONRA çalışır.
/// Sıra önemli: listede önce eklenen önce çalışır.
/// Override (Dash, Stun) → yeni vector döndür.
/// Scale (Slow, Boost)   → velocity * multiplier döndür.
///
/// Core: expired olanları otomatik temizler (RemoveAll).
/// </summary>
public interface IMovementModifier
{
    Vector3 ModifyVelocity(Vector3 velocity, MovementContext context);

    /// <summary>True olunca Core listeden çıkarır. Duration bitince true yap.</summary>
    bool IsExpired { get; }

    /// <summary>Debug overlay için. Örn: "Slow(0.6x, 1.8s)"</summary>
    string DebugLabel { get; }
}
