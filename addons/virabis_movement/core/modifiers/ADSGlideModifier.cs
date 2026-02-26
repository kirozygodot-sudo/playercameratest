using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── ADS GLIDE (Nişan Kayması / Aim Down Sight Gliding) ──────────────────────
/// <summary>
/// Apex Legends tarzı ADS glide. Sağ tık nişan + hareket = momentum kayması.
/// Slide hissi nişan alırken, kayma mesafesi sprint momentumuna bağlı.
/// </summary>
public sealed class ADSGlideModifier : IMovementModifier
{
    private readonly float _glideFriction;        // Kayma sürtünmesi (düşük)
    private readonly float _momentumRetention;    // Momentum korunumu
    private readonly float _minSpeedThreshold;    // Glide için min hız
    private readonly float _maxGlideDuration;     // Max glide süresi
    private readonly float _aimSensitivityMult;   // Glide sırasında sens.
    
    private bool _isGliding;
    private float _remaining;
    private Vector3 _glideDirection;
    private float _entrySpeed;
    
    public ADSGlideModifier(
        float friction = 2f,
        float momentumRetention = 0.9f,
        float minSpeed = 6f,
        float maxDuration = 1.5f,
        float aimSensitivityMult = 0.6f)
    {
        _glideFriction = friction;
        _momentumRetention = momentumRetention;
        _minSpeedThreshold = minSpeed;
        _maxGlideDuration = maxDuration;
        _aimSensitivityMult = aimSensitivityMult;
    }
    
    public bool IsExpired => false;  // Manuel kontrol
    public bool IsGliding => _isGliding;
    public float RemainingDuration => _remaining;
    public float CurrentSensitivityMultiplier => _isGliding ? _aimSensitivityMult : 1f;
    public float MomentumPercent => _isGliding ? (_entrySpeed > 0 ? (_remaining / _maxGlideDuration) : 0f) : 0f;
    
    public string DebugLabel => _isGliding 
        ? $"ADSGlide({_remaining:F1}s, sens={_aimSensitivityMult:P0})" 
        : "ADSGlide(ready)";
    
    /// <summary>
    /// Glide başlat (ADS aktif olduğunda)
    /// </summary>
    public bool StartGlide(Vector3 currentVelocity, Vector3 aimDirection)
    {
        var speed = currentVelocity.Length();
        if (speed < _minSpeedThreshold) return false;
        
        _isGliding = true;
        _remaining = _maxGlideDuration;
        _entrySpeed = speed;
        
        // Glide yönü = hareket yönü + hafif nişan yönü bias
        _glideDirection = Vector3.Normalize(currentVelocity);
        var aimBias = Vector3.Normalize(aimDirection) * 0.3f;
        _glideDirection = Vector3.Normalize(_glideDirection + aimBias);
        
        return true;
    }
    
    /// <summary>
    /// Glide sonlandır (ADS bırakıldığında)
    /// </summary>
    public Vector3 EndGlide(Vector3 currentVelocity)
    {
        if (!_isGliding) return currentVelocity;
        
        _isGliding = false;
        
        // Glide sonu momentum - ne kadar süre glide yaptıysa o kadar korunur
        var retention = Mathf.Lerp(_momentumRetention, 1f, _remaining / _maxGlideDuration);
        return currentVelocity * retention;
    }
    
    public void StopGlide()
    {
        _isGliding = false;
        _remaining = 0f;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        if (!_isGliding) return velocity;
        
        _remaining -= ctx.DeltaTime;
        
        if (_remaining <= 0f || ctx.IsOnFloor && velocity.Length() < _minSpeedThreshold * 0.5f)
        {
            _isGliding = false;
            return velocity * _momentumRetention;
        }
        
        // Glide physics - düşük sürtünme, momentum korunumu
        var result = _glideDirection * velocity.Length();
        result = Vector3.Lerp(result, Vector3.Zero, _glideFriction * ctx.DeltaTime);
        
        // Y ekseninde smooth geçiş (hero landing hissi)
        result.Y = velocity.Y;
        
        return result;
    }
    
    /// <summary>
    /// Aim sensitivity multiplier (kamera için)
    /// </summary>
    public float GetAimSensitivityMultiplier()
    {
        return _isGliding ? _aimSensitivityMult : 1f;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
