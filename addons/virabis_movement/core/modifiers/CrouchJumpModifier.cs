using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── CROUCH-JUMP HEIGHT MODIFIER ───────────────────────────────────────────
/// <summary>
/// Super Mario 2 tarzı crouch-jump. Çömelme + zıplama = %150 zıplama yüksekliği.
/// Input timing kritik: 0.1sn pencere.
/// </summary>
public sealed class CrouchJumpModifier : IMovementModifier
{
    private readonly float _heightMultiplier;     // Zıplama çarpanı
    private readonly float _timingWindow;         // Input pencere süresi
    private readonly float _chargeTime;           // Max charge süresi
    private readonly float _minChargePercent;     // Min charge gerekli
    
    private bool _isCharging;
    private float _chargeProgress;
    private bool _jumpQueued;
    private float _jumpQueueTimer;
    private bool _applyBoost;
    
    public CrouchJumpModifier(
        float heightMultiplier = 1.5f,
        float timingWindow = 0.1f,
        float chargeTime = 0.3f,
        float minChargePercent = 0.3f)
    {
        _heightMultiplier = heightMultiplier;
        _timingWindow = timingWindow;
        _chargeTime = chargeTime;
        _minChargePercent = minChargePercent;
    }
    
    public bool IsExpired => false;  // Manuel kontrol
    public bool IsCharging => _isCharging;
    public float ChargeProgress => _chargeProgress / _chargeTime;
    public bool IsJumpQueued => _jumpQueued;
    public bool HasBoost => _applyBoost;
    
    public string DebugLabel => _isCharging 
        ? $"CrouchJump(charging={ChargeProgress:P0})" 
        : (_applyBoost ? "CrouchJump(BOOST!)" : "CrouchJump(ready)");
    
    /// <summary>
    /// Çömelme başlat/sonlandır
    /// </summary>
    public void SetCrouching(bool crouching)
    {
        if (crouching && !_isCharging)
        {
            // Charge başlat
            _isCharging = true;
            _chargeProgress = 0f;
        }
        else if (!crouching && _isCharging)
        {
            // Çömelme bırakıldı
            _isCharging = false;
            
            // Jump queue aktifse ve charge yeterliyse boost uygula
            if (_jumpQueued && ChargeProgress >= _minChargePercent)
            {
                _applyBoost = true;
            }
        }
    }
    
    /// <summary>
    /// Zıplama talebi
    /// </summary>
    public void RequestJump()
    {
        if (_isCharging)
        {
            // Çömelme devam ediyor - charge devam etsin, jump sonrası
            _jumpQueued = true;
            _jumpQueueTimer = _timingWindow;
        }
        else
        {
            // Normal zıplama - timing penceresi kontrolü
            if (_chargeProgress > 0f && _chargeProgress < _timingWindow)
            {
                _applyBoost = true;
            }
        }
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Charge progress
        if (_isCharging)
        {
            _chargeProgress += ctx.DeltaTime;
            if (_chargeProgress > _chargeTime)
            {
                _chargeProgress = _chargeTime;  // Max charge
            }
        }
        
        // Jump queue countdown
        if (_jumpQueued)
        {
            _jumpQueueTimer -= ctx.DeltaTime;
            if (_jumpQueueTimer <= 0f)
            {
                _jumpQueued = false;
            }
        }
        
        // Boost uygulama
        if (_applyBoost)
        {
            _applyBoost = false;
            _jumpQueued = false;
            _chargeProgress = 0f;
            
            // Yatay hız korunur, dikey boost
            var boostMult = Mathf.Lerp(1f, _heightMultiplier, ChargeProgress);
            velocity.Y *= boostMult;
            
            return velocity;
        }
        
        return velocity;
    }
    
    /// <summary>
    /// Jump boost değerini al ve resetle
    /// </summary>
    public float ConsumeJumpBoost()
    {
        if (!_applyBoost) return 1f;
        
        _applyBoost = false;
        return Mathf.Lerp(1f, _heightMultiplier, ChargeProgress);
    }
}

// Mathf helper
file static class Mathf
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
