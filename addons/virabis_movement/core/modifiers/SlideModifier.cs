using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── SLIDE (Kayma + Melee Combo) ────────────────────────────────────────────
/// <summary>
/// Yere kayma durumu. Slide sırasında melee attack = 2x damage + boost.
/// Exit momentum = hızlı çıkış hızı.
/// </summary>
public sealed class SlideModifier : IMovementModifier
{
    private readonly float _slideFriction;        // Düşük sürtünme
    private readonly float _maxSlideDuration;
    private readonly float _exitMomentum;         // Çıkış momentum çarpanı
    private readonly float _attackDamageMultiplier; // Slide attack multiplier
    private readonly float _attackSpeedBoost;     // Attack sonrası hız boost
    
    private float _remaining;
    private bool _isSliding;
    private bool _attackUsed;                       // Attack kullanıldı mı
    private Vector3 _slideDirection;
    
    public SlideModifier(
        float friction = 3f, 
        float maxDuration = 2f, 
        float exitMomentum = 0.6f,
        float attackDamageMult = 2f,
        float attackSpeedBoost = 1.5f)
    {
        _slideFriction = friction;
        _maxSlideDuration = maxDuration;
        _exitMomentum = exitMomentum;
        _attackDamageMultiplier = attackDamageMult;
        _attackSpeedBoost = attackSpeedBoost;
        _remaining = 0f;
    }
    
    public bool IsExpired => !_isSliding && _remaining <= 0f;
    public bool IsSliding => _isSliding;
    public bool CanAttack => _isSliding && !_attackUsed;
    public float DamageMultiplier => _attackUsed ? 1f : _attackDamageMultiplier;
    
    public string DebugLabel => _isSliding 
        ? $"Slide({_remaining:F1}s, attack={_attackUsed})" 
        : $"Slide(exit={_exitMomentum:F1}x)";
    
    public void StartSlide(Vector3 direction, float initialSpeed)
    {
        _isSliding = true;
        _remaining = _maxSlideDuration;
        _attackUsed = false;
        _slideDirection = Vector3.Normalize(direction);
    }
    
    public void PerformAttack()
    {
        if (!_isSliding || _attackUsed) return;
        _attackUsed = true;
        // Attack sonrası hız boost için remaining'i artır
        _remaining = Mathf.Max(_remaining, 0.3f);  // Min slide süresi
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        if (!_isSliding) return velocity;
        
        _remaining -= ctx.DeltaTime;
        
        if (_remaining <= 0f || ctx.IsOnFloor && velocity.Length() < 2f)
        {
            _isSliding = false;
            // Slide çıkış momentum
            var exitSpeed = velocity.Length() * (_attackUsed ? _attackSpeedBoost : _exitMomentum);
            return Vector3.Normalize(_slideDirection) * exitSpeed;
        }
        
        // Slide sürtünmesi (düşük friction)
        var result = Vector3.Normalize(_slideDirection) * velocity.Length();
        result = Vector3.Lerp(result, Vector3.Zero, _slideFriction * ctx.DeltaTime);
        
        // Attack kullanıldıysa ek speed boost
        if (_attackUsed)
        {
            result *= 1.1f;
        }
        
        return result;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Max(float a, float b) => a > b ? a : b;
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
