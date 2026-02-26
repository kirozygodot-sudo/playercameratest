using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

// ── VAULTING (Otomatik Engel Aşma) ─────────────────────────────────────────
/// <summary>
/// Yüksek hızla küçük engellere çarptığında otomatik atlar.
/// Titanfall/DOOM Eternal tarzı seamless parkour.
/// </summary>
public sealed class VaultModifier : IMovementModifier
{
    private readonly float _minSpeedThreshold;    // Vault için min hız (örn: 8 m/s)
    private readonly float _maxObstacleHeight;    // Aşılabilir max engel yüksekliği
    private readonly float _vaultDuration;
    private readonly float _vaultBoost;           // Vault çıkış hızı çarpanı
    
    private float _remaining;
    private bool _isVaulting;
    private Vector3 _vaultDirection;
    
    public VaultModifier(float minSpeed, float maxHeight, float duration, float boost = 1.2f)
    {
        _minSpeedThreshold = minSpeed;
        _maxObstacleHeight = maxHeight;
        _vaultDuration = duration;
        _vaultBoost = boost;
        _remaining = 0f;
    }
    
    public bool IsExpired => !_isVaulting && _remaining <= 0f;
    public bool IsVaulting => _isVaulting;
    public string DebugLabel => _isVaulting ? $"Vault({_remaining:F1}s)" : "Vault(ready)";
    
    /// <summary>
    /// Vault aktivasyon kontrolü - MovementNode tarafından çağrılır
    /// </summary>
    public bool CanVault(float currentSpeed, float obstacleHeight, Vector3 moveDirection)
    {
        if (_isVaulting) return false;
        if (currentSpeed < _minSpeedThreshold) return false;
        if (obstacleHeight > _maxObstacleHeight) return false;
        return true;
    }
    
    public void StartVault(Vector3 direction)
    {
        _isVaulting = true;
        _remaining = _vaultDuration;
        _vaultDirection = Vector3.Normalize(direction);
    }
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext ctx)
    {
        if (!_isVaulting) return velocity;
        
        _remaining -= ctx.DeltaTime;
        
        if (_remaining <= 0f)
        {
            _isVaulting = false;
            // Vault çıkışında momentum boost
            return velocity * _vaultBoost;
        }
        
        // Vault sırasında yatay hız korunur, dikey smooth geçiş
        var result = _vaultDirection * velocity.Length();
        result.Y = Mathf.Lerp(velocity.Y, 0f, 0.5f);  // Yumuşak yükseklik geçişi
        
        return result;
    }
}

// Mathf helper (System.Numerics'de Lerp yok)
file static class Mathf
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
