using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── TACTICAL TIME DILATION (Low Health Slow-Mo) ─────────────────────────────
/// <summary>
/// Max Payne tarzı bullet time. %25 sağlık altında otomatik aktive olur.
/// Dünya yavaşlar, oyuncu normal hızda kalır.
/// </summary>
public sealed class TimeDilationModifier : IMovementModifier
{
    private readonly float _healthThreshold;      // Aktivasyon eşiği (%0.0-1.0)
    private readonly float _slowScale;          // Dünya yavaşlama oranı
    private readonly float _duration;             // Süre
    private readonly float _cooldown;             // Cooldown
    
    private float _remaining;
    private float _cooldownRemaining;
    private bool _isActive;
    private float _currentHealthPercent;
    
    public TimeDilationModifier(
        float healthThreshold = 0.25f,
        float slowScale = 0.5f,
        float duration = 2f,
        float cooldown = 30f)
    {
        _healthThreshold = healthThreshold;
        _slowScale = slowScale;
        _duration = duration;
        _cooldown = cooldown;
        _remaining = 0f;
        _cooldownRemaining = 0f;
    }
    
    public bool IsExpired => false;  // Manuel kontrol
    public bool IsActive => _isActive;
    public float CurrentTimeScale => _isActive ? _slowScale : 1f;
    public float RemainingDuration => _remaining;
    public float CooldownProgress => 1f - (_cooldownRemaining / _cooldown);
    
    public string DebugLabel => _isActive 
        ? $"TimeDilation({_remaining:F1}s)" 
        : $"TimeDilation(cooldown={_cooldownRemaining:F1}s)";
    
    /// <summary>
    /// Her frame health kontrolü
    /// </summary>
    public void UpdateHealth(float healthPercent)
    {
        _currentHealthPercent = healthPercent;
        
        // Auto-activate koşulu
        if (!_isActive && _cooldownRemaining <= 0f && healthPercent <= _healthThreshold)
        {
            Activate();
        }
    }
    
    public void Activate()
    {
        if (_cooldownRemaining > 0f) return;
        _isActive = true;
        _remaining = _duration;
    }
    
    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;
        _cooldownRemaining = _cooldown;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Cooldown yönetimi
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= ctx.DeltaTime;
        }
        
        if (!_isActive) return velocity;
        
        _remaining -= ctx.DeltaTime;
        
        if (_remaining <= 0f)
        {
            Deactivate();
            return velocity;
        }
        
        // Time Dilation aktif: oyuncu velocity'sini scale up yap
        // (Dünya yavaşladığı için oyuncu daha hızlı hareket etmeli relative olarak)
        var timeCompensation = 1f / _slowScale;
        return velocity * timeCompensation;
    }
    
    /// <summary>
    /// Godot TimeScale için değer
    /// </summary>
    public float GetEngineTimeScale()
    {
        return _isActive ? _slowScale : 1f;
    }
}
