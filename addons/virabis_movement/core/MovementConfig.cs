using Godot;

namespace Virabis.Movement.Core;

/// <summary>
/// Tüm movement parametreleri. Data-driven: JSON'dan yüklenebilir.
/// Core logic değiştirilmeden sadece bu dosyayı tweakleyerek hissi değiştirirsin.
/// Farklı karakter/araç/ejderha için farklı config instance'ı oluştur.
/// </summary>
[GlobalClass]
public partial record MovementConfig : Resource
{
    // ── Hızlar ──────────────────────────────────────────────────────────────
    [Export] public float WalkSpeed             { get; init; } = 6.0f;
    [Export] public float SprintSpeed           { get; init; } = 9.5f;
    [Export] public float FlyingSpeed           { get; init; } = 8.0f;

    // ── Zemin ───────────────────────────────────────────────────────────────
    [Export] public float GroundAcceleration    { get; init; } = 20.0f;
    [Export] public float GroundFriction        { get; init; } = 18.0f;

    // ── Hava (High Air Control — combat suitability) ─────────────────────
    [Export] public float AirAcceleration       { get; init; } = 14.0f;
    [Export] public float AirFriction           { get; init; } = 2.0f;
    [Export] public float AirControlMultiplier  { get; init; } = 0.85f;  // 1.0 = tam arcade

    // ── Yön / Turn (Max Payne hissi) ─────────────────────────────────────
    [Export] public float TurnResponsiveness    { get; init; } = 1.2f;   // Yüksek = ani dönüş daha ağır
    [Export] public float SprintTurnMultiplier  { get; init; } = 0.6f;   // Sprint'te ek yön cezası

    // ── Zıplama ───────────────────────────────────────────────────────────
    /// <summary>1 = normal, 2 = double jump, 0 = zıplama yok.</summary>
    [Export] public int   MaxJumps              { get; init; } = 1;

    // ── Güvenlik ──────────────────────────────────────────────────────────
    [Export] public float MaxSpeedCap           { get; init; } = 20.0f;
    /// <summary>Aynı anda max aktif modifier. Stacking exploit + perf guard.</summary>
    [Export] public int   MaxModifiers          { get; init; } = 8;

    // ── Presets ───────────────────────────────────────────────────────────

}
