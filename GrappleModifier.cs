using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── GRAPPLE HOOK (Gerçek İp Fiziği) ─────────────────────────────────────────
/// <summary>
/// Just Cause 2 tarzı grapple hook. Yaylanma, momentum korunumu, fırlatma.
/// </summary>
public sealed class GrappleModifier : IMovementModifier
{
    // Fizik parametreleri
    private readonly float _springStrength;       // Yay sabiti (k)
    private readonly float _damping;              // Sönümleme
    private readonly float _maxLength;            // Max ip uzunluğu
    private readonly float _minLength;            // Min ip uzunluğu (çarpmaması için)
    private readonly float _pullSpeed;            // Çekilme hızı
    private readonly float _launchBoost;          // Fırlatma boost çarpanı
    
    // Durum
    private Vector3 _anchorPoint;                  // İp bağlama noktası
    private float _currentLength;
    private bool _isGrappling;
    private bool _isRetracting;                  // İp çekiliyor mu
    private bool _launchPending;                   // Fırlatma bekleniyor mu
    private float _launchWindow;                   // Fırlatma pencere süresi
    
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
    
    public bool IsExpired => false;  // Manuel kontrol
    public bool IsGrappling => _isGrappling;
    public bool CanLaunch => _launchPending && _launchWindow > 0f;
    public float CurrentLength => _currentLength;
    public float MaxLength => _maxLength;
    
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
        if (_isGrappling && _currentLength < _maxLength * 0.8f)
        {
            // Erken release = fırlatma penceresi aç
            _launchPending = true;
            _launchWindow = 0.3f;  // 300ms fırlatma penceresi
        }
        _isGrappling = false;
        _isRetracting = false;
    }
    
    public void SetRetracting(bool retracting)
    {
        _isRetracting = retracting;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Fırlatma penceresi kontrolü
        if (_launchPending)
        {
            _launchWindow -= ctx.DeltaTime;
            if (_launchWindow <= 0f) _launchPending = false;
        }
        
        if (!_isGrappling) return velocity;
        
        // Oyuncu pozisyonunu velocity'den tahmin et (gerçek pozisyon Godot'da)
        // Burada velocity'ye ip fizikçisi uyguluyoruz
        
        var toAnchor = _anchorPoint - ctx.CurrentPosition;  // Anchor'a doğru vektör
        var distance = toAnchor.Length();
        _currentLength = distance;
        
        // Max uzunluk kontrolü
        if (distance > _maxLength)
        {
            _isGrappling = false;
            return velocity;
        }
        
        // İp çekilme
        if (_isRetracting && distance > _minLength)
        {
            distance = Mathf.Max(distance - _pullSpeed * ctx.DeltaTime, _minLength);
            _currentLength = distance;
        }
        
        // Yaylanma kuvveti (Hooke's Law: F = -k * x)
        var stretch = distance - (_minLength + (_maxLength - _minLength) * 0.3f);  // Optimal uzunluk
        var springForce = Vector3.Normalize(toAnchor) * (_springStrength * stretch * ctx.DeltaTime);
        
        // Sönümleme (damping)
        var dampForce = velocity * (_damping * ctx.DeltaTime);
        
        // Fırlatma penceresi aç (swing apex detection)
        if (velocity.Y > 0f && Mathf.Abs(velocity.Y) < 2f)
        {
            _launchPending = true;
            _launchWindow = 0.2f;
        }
        
        return velocity + springForce - dampForce;
    }
    
    /// <summary>
    /// Fırlatma momentumu - release timing kritik
    /// </summary>
    public Vector3 GetLaunchVelocity(Vector3 currentVelocity)
    {
        if (!CanLaunch) return currentVelocity;
        
        _launchPending = false;
        return currentVelocity * _launchBoost;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Max(float a, float b) => a > b ? a : b;
    public static float Abs(float a) => a < 0 ? -a : a;
    public static float Distance(Vector3 a, Vector3 b) => (a - b).Length();
}
