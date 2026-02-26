using System.Numerics;

namespace Virabis.Movement.Core;

/// <summary>
/// Core'un bir frame'de ihtiyacı olan tüm dış bilgi.
/// Bridge her _PhysicsProcess'te bu snapshot'ı oluşturur.
///
/// KURAL: Kamera buraya girmez.
/// InputDirection zaten camera-relative hesaplanmış olarak gelir.
/// Bunu PlayerController.gd yapar. Core kaynağını bilmez, umursamaz.
/// </summary>
public readonly record struct MovementContext
{
    /// <summary>
    /// Camera-relative, normalize edilmiş yatay yön (Y = 0).
    /// PlayerController, kamera forward/right'ından hesaplar ve Bridge'e verir.
    /// Zero Vector3 = girdi yok.
    /// </summary>
    public Vector3 InputDirection  { get; init; }

    /// <summary>Godot: CharacterBody3D.IsOnFloor()</summary>
    public bool    IsOnFloor       { get; init; }

    public bool    IsSprinting     { get; init; }

    /// <summary>True → Core Flying state döner → Bridge gravity uygulamaz.</summary>
    public bool    IsFlying        { get; init; }

    /// <summary>
    /// Single-frame jump isteği.
    /// PlayerController "jump" action'ı algılayınca true gönderir, sonraki frame false.
    /// </summary>
    public bool    JumpRequested   { get; init; }

    /// <summary>Kalan zıplama hakkı. Bridge takip eder, Core kullanır.</summary>
    public int     JumpsRemaining  { get; init; }

    public float   DeltaTime       { get; init; }

    /// <summary>Sadece horizontal bileşen (Y = 0). Bridge vertical'ı ayrı yönetir.</summary>
    public Vector3 CurrentVelocity { get; init; }
}
