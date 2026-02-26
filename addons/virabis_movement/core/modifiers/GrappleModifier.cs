using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── GRAPPLE HOOK (Geliştirilmiş Spring-Damper Fiziği) ────────────────────────
/// <summary>
/// Just Cause tarzı grapple hook. 
/// GÜNCELLEME: Damping kuvveti çekim kuvvetini sıfırlamaz, her zaman hedefe yönelim sağlar.
/// </summary>
public sealed class GrappleModifier : IMovementModifier
{
    // Fizik parametreleri
    private readonly float _springStrength;       // Yay sabiti (k)
    private readonly float _damping;              // Sönümleme (b)
    private readonly float _maxLength;            // Max ip uzunluğu
    private readonly float _minLength;            // Min ip uzunluğu
    private readonly float _pullSpeed;            // Çekilme hızı
    private readonly float _launchBoost;          // Fırlatma boost çarpanı
    
    // Durum
    private Vector3 _anchorPoint;                  
    private float _currentLength;
    private bool _isGrappling;
    private bool _isRetracting;                  
    private bool _launchPending;                   
    private float _launchWindow;                   
    
    public GrappleModifier(
        float springStrength = 150f,
        float damping = 8f,
        float maxLength = 30f,
        float minLength = 2f,
        float pullSpeed = 15f,
        float launchBoost = 1.5f)
    {
        _springStrength = springStrength;
        _damping = damping;
        _maxLength = maxLength;
        _minLength = minLength;
        _pullSpeed = pullSpeed;
        _launchBoost = launchBoost;
        _isGrappling = false;
    }
    
    public bool IsExpired => false;
    public bool IsGrappling => _isGrappling;
    public bool CanLaunch => _launchPending && _launchWindow > 0f;
    
    public string DebugLabel => _isGrappling 
        ? $"Grapple({_currentLength:F1}m, launch={_launchPending})" 
        : "Grapple(inactive)";
    
    public void StartGrapple(Vector3 anchorPoint, Vector3 currentPosition)
    {
        _anchorPoint = anchorPoint;
        _currentLength = Vector3.Distance(currentPosition, anchorPoint);
        _isGrappling = true;
        _isRetracting = false;
        _launchPending = false;
        _launchWindow = 0f;
    }
    
    public void ReleaseGrapple()
    {
        if (_isGrappling)
        {
            _launchPending = true;
            _launchWindow = 0.3f;
        }
        _isGrappling = false;
        _isRetracting = false;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        if (_launchPending)
        {
            _launchWindow -= ctx.DeltaTime;
            if (_launchWindow <= 0f) _launchPending = false;
        }
        
        if (!_isGrappling) return velocity;
        
        var toAnchor = _anchorPoint - ctx.CurrentPosition;
        var distance = toAnchor.Length();
        _currentLength = distance;
        
        if (distance > _maxLength * 1.2f) // %20 tolerans
        {
            _isGrappling = false;
            return velocity;
        }

        var direction = Vector3.Normalize(toAnchor);
        
        // ── KRİTİK GÜNCELLEME: Spring-Damper Modeli ──────────────────────────
        // F = -k * x - b * v
        // Burada x = (mevcut_uzunluk - hedef_uzunluk)
        // Hedef yönelimini korumak için damping kuvvetini sadece hıza değil, 
        // hızın ip yönündeki bileşenine (radial velocity) uygulamalıyız.
        
        float currentSpringLength = _isRetracting ? _minLength : (_maxLength * 0.5f);
        float stretch = distance - currentSpringLength;
        
        // Yay kuvveti (her zaman merkeze çeker)
        Vector3 springForce = direction * (_springStrength * stretch);
        
        // Sönümleme (Damping)
        // Hızın ip yönündeki izdüşümünü bul (radial velocity)
        float radialVelocity = Vector3.Dot(velocity, direction);
        Vector3 dampForce = direction * (_damping * radialVelocity);
        
        // Toplam grapple ivmesi
        Vector3 grappleAccel = springForce - dampForce;
        
        // Swing (salınım) mekaniği için teğetsel hızı koru, radyal hızı yayla yönet
        return velocity + grappleAccel * ctx.DeltaTime;
    }
    
    public Vector3 GetLaunchVelocity(Vector3 currentVelocity)
    {
        if (!CanLaunch) return currentVelocity;
        _launchPending = false;
        return currentVelocity * _launchBoost;
    }
}

file static class Mathf
{
    public static float Distance(Vector3 a, Vector3 b) => (a - b).Length();
}
