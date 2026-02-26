using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── AIR-MOMENTUM TRANSFER (Dash-into-Jump Combo) ────────────────────────────
/// <summary>
/// Warframe tarzı momentum transfer. Dash bitişinde zıplama = dash momentum zıplamaya aktarılır.
/// Dash→Jump→DoubleJump = 3x normal mesafe. Momentum korunumu + chain combo bonus.
/// </summary>
public sealed class AirMomentumTransferModifier : IMovementModifier
{
    private readonly float _momentumRetention;    // Momentum korunum yüzdesi
    private readonly float _chainMultiplier;      // Chain combo çarpanı (her adımda artar)
    private readonly float _transferWindow;       // Momentum transfer penceresi
    private readonly int _maxChainCount;        // Max chain sayısı
    
    private bool _dashJustEnded;
    private float _transferTimer;
    private Vector3 _savedDashMomentum;
    private float _savedDashSpeed;
    private int _chainCount;
    private bool _isChaining;
    
    public AirMomentumTransferModifier(
        float momentumRetention = 0.85f,
        float chainMultiplier = 1.2f,
        float transferWindow = 0.25f,
        int maxChainCount = 3)
    {
        _momentumRetention = momentumRetention;
        _chainMultiplier = chainMultiplier;
        _transferWindow = transferWindow;
        _maxChainCount = maxChainCount;
    }
    
    public bool IsExpired => false;  // Manuel kontrol
    public bool CanTransferMomentum => _dashJustEnded && _transferTimer > 0f;
    public float TransferWindowRemaining => _transferTimer;
    public int ChainCount => _chainCount;
    public bool IsChaining => _isChaining;
    public float CurrentMultiplier => Mathf.Pow(_chainMultiplier, _chainCount);
    
    public string DebugLabel => _isChaining 
        ? $"MomentumTransfer(chain={_chainCount}/{_maxChainCount}, mult={CurrentMultiplier:F1}x)" 
        : (CanTransferMomentum ? "MomentumTransfer(READY!)" : "MomentumTransfer(idle)");
    
    /// <summary>
    /// Dash bittiğinde çağrılır
    /// </summary>
    public void OnDashEnded(Vector3 exitVelocity)
    {
        _dashJustEnded = true;
        _transferTimer = _transferWindow;
        _savedDashMomentum = exitVelocity;
        _savedDashSpeed = exitVelocity.Length();
        
        // Chain yönetimi
        if (!_isChaining)
        {
            _chainCount = 0;
        }
    }
    
    /// <summary>
    /// Zıplama talebi - momentum transfer burada gerçekleşir
    /// </summary>
    public Vector3? TryJumpTransfer(Vector3 currentVelocity, bool isDoubleJump = false)
    {
        if (!CanTransferMomentum && !_isChaining) return null;
        
        // Transfer gerçekleşti
        _dashJustEnded = false;
        _transferTimer = 0f;
        _isChaining = true;
        
        // Chain artır
        if (_chainCount < _maxChainCount)
        {
            _chainCount++;
        }
        
        // Momentum hesapla
        var retention = isDoubleJump ? _momentumRetention * 0.7f : _momentumRetention;
        var chainMult = Mathf.Pow(_chainMultiplier, _chainCount);
        
        var transferredMomentum = _savedDashMomentum * retention * chainMult;
        
        // Yeni velocity = mevcut + transfer edilmiş
        // Dikey boost
        var result = currentVelocity + transferredMomentum;
        result.Y = Mathf.Max(result.Y, _savedDashSpeed * 0.5f * chainMult);
        
        return result;
    }
    
    /// <summary>
    /// Chain sonlandır (yere değince veya zaman aşımı)
    /// </summary>
    public void EndChain()
    {
        _isChaining = false;
        _chainCount = 0;
        _dashJustEnded = false;
        _transferTimer = 0f;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Transfer window countdown
        if (_transferTimer > 0f)
        {
            _transferTimer -= ctx.DeltaTime;
            if (_transferTimer <= 0f && !_isChaining)
            {
                _dashJustEnded = false;
            }
        }
        
        // Yere değince chain sonlanır
        if (ctx.IsOnFloor && _isChaining)
        {
            EndChain();
        }
        
        // Havada sürtünme azaltma (chain sırasında)
        if (_isChaining && !ctx.IsOnFloor)
        {
            // Chain derinliğine göre air control bonus
            var controlBonus = 1f + (_chainCount * 0.1f);
            velocity *= controlBonus;
        }
        
        return velocity;
    }
    
    /// <summary>
    /// Momentum transfer aktif mi (UI/animasyon için)
    /// </summary>
    public bool ShouldShowTransferIndicator()
    {
        return CanTransferMomentum || _isChaining;
    }
}

// Mathf helper
file static class Mathf
{
    public static float Pow(float baseValue, float exponent) => (float)Math.Pow(baseValue, exponent);
    public static float Max(float a, float b) => a > b ? a : b;
}
