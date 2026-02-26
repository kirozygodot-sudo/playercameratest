using System;

namespace Virabis.Movement.Core;

/// <summary>
/// "The Pulse" - Merkezi, event-driven zamanlayıcı ve durum buffer sistemi.
/// Minimal CPU kullanımı için sadece aktif zamanlayıcıları takip eder.
/// </summary>
public sealed class MovementPulse
{
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    
    public bool IsCoyoteActive => _coyoteTimer > 0f;
    public bool IsJumpBuffered => _jumpBufferTimer > 0f;

    public event Action OnCoyoteExpired;
    public event Action OnJumpBufferExpired;

    public void Update(float dt)
    {
        if (_coyoteTimer > 0f)
        {
            _coyoteTimer -= dt;
            if (_coyoteTimer <= 0f) OnCoyoteExpired?.Invoke();
        }

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= dt;
            if (_jumpBufferTimer <= 0f) OnJumpBufferExpired?.Invoke();
        }
    }

    public void StartCoyote(float duration) => _coyoteTimer = duration;
    public void StartJumpBuffer(float duration) => _jumpBufferTimer = duration;
    
    public void ClearCoyote() => _coyoteTimer = 0f;
    public void ClearJumpBuffer() => _jumpBufferTimer = 0f;
}
