using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── RECOIL PROPULSION (Silah İtkisi) ───────────────────────────────────────
/// <summary>
/// Quake tarzı rocket jumping light. Heavy weapons = strafe boost.
/// Ateş etme yönünün tersine momentum.
/// </summary>
public sealed class RecoilModifier : IMovementModifier
{
    private readonly float _knockbackForce;       // İtki kuvveti
    private readonly float _airControlImmunity;   // Havada kontrol kaybı süresi
    private readonly float _verticalBias;         // Dikey itki payı
    
    private Vector3 _pendingRecoil;
    private float _controlImmunityTimer;
    private float _recoilDecay;
    
    public RecoilModifier(
        float knockbackForce = 8f,
        float airControlImmunity = 0.1f,
        float verticalBias = 0.3f,
        float decay = 5f)
    {
        _knockbackForce = knockbackForce;
        _airControlImmunity = airControlImmunity;
        _verticalBias = Mathf.Clamp(verticalBias, 0f, 1f);
        _recoilDecay = decay;
        _pendingRecoil = Vector3.Zero;
    }
    
    public bool IsExpired => _pendingRecoil.Length() < 0.01f && _controlImmunityTimer <= 0f;
    public bool HasControlImmunity => _controlImmunityTimer > 0f;
    public float ImmunityRemaining => _controlImmunityTimer;
    
    public string DebugLabel => HasControlImmunity 
        ? $"Recoil({_controlImmunityTimer:F2}s, no-control)" 
        : $"Recoil({_pendingRecoil.Length():F1})";
    
    /// <summary>
    /// Ateş etme çağrısı - MovementNode veya WeaponSystem tarafından çağrılır
    /// </summary>
    /// <param name="fireDirection">Ateş yönü (geri tepkinin tersi)</param>
    /// <param name="weaponWeight">Silah ağırlığı (0-1, heavy = daha çok knockback)</param>
    /// <param name="isAirborne">Havada mı</param>
    public void Fire(Vector3 fireDirection, float weaponWeight = 0.5f, bool isAirborne = false)
    {
        // Geri tepki ters yönde
        var recoilDir = -Vector3.Normalize(fireDirection);
        
        // Silah ağırlığı etkisi
        var weightMult = 0.5f + weaponWeight;  // 0.5 - 1.5 arası
        
        // Havada boost bonus
        var airMult = isAirborne ? 1.3f : 1f;
        
        // Dikey komponent ekle (rocket jump hissi)
        recoilDir.Y = Mathf.Lerp(recoilDir.Y, 1f, _verticalBias);
        recoilDir = Vector3.Normalize(recoilDir);
        
        _pendingRecoil += recoilDir * _knockbackForce * weightMult * airMult;
        
        // Havada kontrol kaybı
        if (isAirborne)
        {
            _controlImmunityTimer = _airControlImmunity;
        }
    }
    
    /// <summary>
    /// Strafe jump boost - hafif silahlar için yan hareket bonusu
    /// </summary>
    public void StrafeBoost(Vector3 strafeDirection, float intensity = 1f)
    {
        _pendingRecoil += Vector3.Normalize(strafeDirection) * _knockbackForce * 0.5f * intensity;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Control immunity countdown
        if (_controlImmunityTimer > 0f)
        {
            _controlImmunityTimer -= ctx.DeltaTime;
        }
        
        // Recoil uygula
        if (_pendingRecoil.LengthSquared() > 0.0001f)
        {
            velocity += _pendingRecoil;
            
            // Decay
            _pendingRecoil = Vector3.Lerp(_pendingRecoil, Vector3.Zero, _recoilDecay * ctx.DeltaTime);
        }
        
        return velocity;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);
    public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0f, 1f);
}
