namespace Virabis.Movement.Core;

/// <summary>
/// Karakterin anlık hareket durumu.
///
/// KARAR: Enum kullanıyoruz, State Pattern değil.
/// Sebep: sade, debug kolay, YAGNI. 10+ karmaşık state gelirse refactor.
///
/// Flying: DisableGravity sinyali verir → Bridge gravity uygulamaz.
/// </summary>
public enum MovementState
{
    Idle,
    Moving,
    Sprinting,
    Airborne,
    Flying
}
