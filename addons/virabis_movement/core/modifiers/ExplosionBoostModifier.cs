using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── ENVIRONMENTAL PROPULSION (Patlama Knockback) ───────────────────────────
/// <summary>
/// Quake tarzı patlama itkisi. Yakındaki patlama = knockback + air control immunity.
/// Stratejik: Kendi bombanla boost, düşman bombasından kaç.
/// </summary>
public sealed class ExplosionBoostModifier : IMovementModifier
{
    private readonly float _explosionRadius;      // Etki yarıçapı
    private readonly float _maxForce;             // Max itki kuvveti
    private readonly float _airControlImmunity;   // Havada kontrol bağışıklığı
    private readonly float _damageThreshold;        // Hasar eşiği (dost/düşman)
    
    private Vector3 _pendingForce;
    private float _immunityTimer;
    private bool _isFriendly;                      // Dost patlama mı
    
    public ExplosionBoostModifier(
        float radius = 5f,
        float maxForce = 20f,
        float airControlImmunity = 0.3f,
        float damageThreshold = 10f)
    {
        _explosionRadius = radius;
        _maxForce = maxForce;
        _airControlImmunity = airControlImmunity;
        _damageThreshold = damageThreshold;
    }
    
    public bool IsExpired => _pendingForce.Length() < 0.01f && _immunityTimer <= 0f;
    public bool HasAirControlImmunity => _immunityTimer > 0f;
    public float ImmunityRemaining => _immunityTimer;
    public bool IsFromFriendlyExplosion => _isFriendly;
    
    public string DebugLabel => HasAirControlImmunity 
        ? $"Explosion(immunity={_immunityTimer:F1}s, force={_pendingForce.Length():F1})" 
        : $"Explosion(force={_pendingForce.Length():F1})";
    
    /// <summary>
    /// Patlama algılandığında çağrılır
    /// </summary>
    /// <param name="explosionCenter">Patlama merkezi</param>
    /// <param name="playerPosition">Oyuncu pozisyonu</param>
    /// <param name="explosionDamage">Patlama hasarı (dost/düşman ayrımı için)</param>
    /// <param name="isFriendly">Dost ateşi mi</param>
    public void ApplyExplosion(
        Vector3 explosionCenter, 
        Vector3 playerPosition, 
        float explosionDamage,
        bool isFriendly = false)
    {
        var toPlayer = playerPosition - explosionCenter;
        var distance = toPlayer.Length();
        
        if (distance > _explosionRadius) return;
        
        // Mesafe bazlı kuvvet (inverse square law)
        var forceFactor = 1f - (distance / _explosionRadius);
        forceFactor *= forceFactor;  // Quadratic falloff
        
        // Dost ateşi daha kontrollü
        var friendlyMult = isFriendly ? 0.8f : 1.2f;
        
        var force = Vector3.Normalize(toPlayer) * _maxForce * forceFactor * friendlyMult;
        
        // Diverse boost
        force.Y = Mathf.Abs(force.Y) * 0.5f;  // Yukarı itki
        
        _pendingForce += force;
        _isFriendly = isFriendly;
        
        // Havada kontrol bağışıklığı
        _immunityTimer = _airControlImmunity;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Immunity countdown
        if (_immunityTimer > 0f)
        {
            _immunityTimer -= ctx.DeltaTime;
        }
        
        // Force uygula
        if (_pendingForce.LengthSquared() > 0.0001f)
        {
            velocity += _pendingForce;
            
            // Force sönümleme
            _pendingForce = Vector3.Lerp(_pendingForce, Vector3.Zero, 3f * ctx.DeltaTime);
        }
        
        return velocity;
    }
    
    /// <summary>
    /// Havada kontrol var mı (immunity kontrolü)
    /// </summary>
    public bool CanControlInAir()
    {
        return _immunityTimer <= 0f;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Abs(float a) => a < 0 ? -a : a;
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
