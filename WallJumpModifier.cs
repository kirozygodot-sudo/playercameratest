using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── WALL-BOUNCE / WALL-JUMP COMBO ──────────────────────────────────────────
/// <summary>
/// Celeste/Quake tarzı duvar zıplama. 3 wall-jump = air-dash reset.
/// Momentum yön değiştirme + air control bonus.
/// </summary>
public sealed class WallJumpModifier : IMovementModifier
{
    private readonly float _bounceForce;          // Duvar sıçrama kuvveti
    private readonly float _maxWallAngle;         // Duvar açısı toleransı (derece)
    private readonly float _airControlBonus;      // Wall-jump sonrası hava kontrolü
    private readonly int _comboResetCount;      // Combo kaçta dash reset?
    private readonly float _comboWindow;          // Combo pencere süresi
    
    private int _comboCount;
    private float _comboTimer;
    private bool _justBounced;
    private float _bonusControlTimer;
    private Vector3 _lastWallNormal;
    
    public WallJumpModifier(
        float bounceForce = 12f,
        float maxWallAngle = 30f,
        float airControlBonus = 1.5f,
        int comboResetCount = 3,
        float comboWindow = 1.5f)
    {
        _bounceForce = bounceForce;
        _maxWallAngle = maxWallAngle * (MathF.PI / 180f);  // Radyana çevir
        _airControlBonus = airControlBonus;
        _comboResetCount = comboResetCount;
        _comboWindow = comboWindow;
        _comboCount = 0;
    }
    
    public bool IsExpired => false;  // Manuel kontrol
    public int ComboCount => _comboCount;
    public bool CanResetDash => _comboCount >= _comboResetCount;
    public bool HasAirControlBonus => _bonusControlTimer > 0f;
    public bool JustBounced => _justBounced;
    
    public string DebugLabel => _comboCount > 0 
        ? $"WallJump(combo={_comboCount}/{_comboResetCount}, bonus={_bonusControlTimer:F1}s)" 
        : "WallJump(ready)";
    
    /// <summary>
    /// Duvara çarpma algılandığında çağrılır
    /// </summary>
    /// <param name="wallNormal">Duvar yüzey normali</param>
    /// <param name="velocity">Mevcut hız</param>
    /// <param name="isWallJumpRequested">Zıplama tuşuna basıldı mı</param>
    public bool TryWallBounce(Vector3 wallNormal, Vector3 velocity, bool isWallJumpRequested)
    {
        _justBounced = false;
        
        // Duvar açısı kontrolü (yatay duvarlar için)
        var upDot = Vector3.Dot(wallNormal, Vector3.UnitY);
        if (MathF.Abs(upDot) > 0.7f) return false;  // Çok dik veya tavan
        
        if (!isWallJumpRequested) return false;
        
        // Bounce hesapla
        var bounceDir = Vector3.Normalize(wallNormal + Vector3.UnitY * 0.5f);  // Yukarı ve dışarı
        _lastWallNormal = wallNormal;
        
        // Combo yönetimi
        if (_comboTimer > 0f)
        {
            _comboCount++;
        }
        else
        {
            _comboCount = 1;
        }
        _comboTimer = _comboWindow;
        
        // Combo reset kontrolü
        if (_comboCount >= _comboResetCount)
        {
            _comboCount = 0;  // Reset
            // Dash reset sinyali (MovementNode'e iletilecek)
        }
        
        // Air control bonus
        _bonusControlTimer = 0.5f;
        
        _justBounced = true;
        return true;
    }
    
    public Vector3 GetBounceVelocity(Vector3 currentVelocity)
    {
        var bounceDir = Vector3.Normalize(_lastWallNormal + Vector3.UnitY * 0.6f);
        return bounceDir * _bounceForce;
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        // Combo timer countdown
        if (_comboTimer > 0f)
        {
            _comboTimer -= ctx.DeltaTime;
        }
        
        // Air control bonus countdown
        if (_bonusControlTimer > 0f)
        {
            _bonusControlTimer -= ctx.DeltaTime;
        }
        
        // Wall-bounce uygula
        if (_justBounced)
        {
            _justBounced = false;
            return GetBounceVelocity(velocity);
        }
        
        // Air control bonus uygula
        if (HasAirControlBonus && !ctx.IsOnFloor)
        {
            velocity *= _airControlBonus;
        }
        
        return velocity;
    }
    
    /// <summary>
    /// Dash reset talebi
    /// </summary>
    public bool ConsumeDashReset()
    {
        if (!CanResetDash) return false;
        _comboCount = 0;
        return true;
    }
}
