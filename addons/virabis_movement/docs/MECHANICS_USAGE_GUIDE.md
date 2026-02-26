# Virabis Movement: 10 Uzman Mekanik Kullanım Rehberi

Bu rehber, sistemdeki 10 gelişmiş hareket mekaniğinin her biri için teknik detayları, tetikleme yöntemlerini ve kod örneklerini içerir.

---

## 1. Grapple Hook (Kanca Atma)
Karakteri bir noktaya doğru fiziksel olarak çeker ve bırakıldığında ivme kazandırır. **GÜNCELLEME:** Geliştirilmiş spring-damper fiziği sayesinde, damping kuvveti çekim kuvvetini tamamen sıfırlamaz, her zaman hedefe yönelim sağlar. Bu, daha gerçekçi ve kontrol edilebilir bir salınım (swing) mekaniği sunar.
*   **Tetikleme:** `ApplyGrapple(anchor_point)`
*   **Bırakma:** `ReleaseGrapple()`

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("grapple"):
    var result = get_raycast_result() # Raycast ile hedef nokta bul
    if result:
        movement.call("ApplyGrapple", result.position)

if Input.is_action_just_released("grapple"):
    movement.call("ReleaseGrapple")
```

---

## 2. Slide & Melee Combo (Kayma ve Saldırı)
Hızlı hareket ederken yere çömelerek kaymanızı sağlar. Kayma sırasında saldırı yapılabilir.
*   **Tetikleme:** `ApplySlide(friction, duration, exit_momentum, damage_mult, speed_boost)`

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("crouch") and is_sprinting:
    movement.call("ApplySlide", 3.0, 2.0, 0.6, 2.0, 1.5)

if Input.is_action_just_pressed("attack"):
    movement.call("PerformSlideAttack")
```

---

## 3. Crouch Jump (Yüksek Zıplama)
Çömelme süresine bağlı olarak daha yükseğe zıplamanızı sağlar.
*   **Tetikleme:** `ApplyCrouchJump(height_mult, window, charge_time, min_charge)`

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("jump") and is_crouching:
    movement.call("ApplyCrouchJump", 1.5, 0.1, crouch_timer, 0.3)
```

---

## 4. Wall Jump (Duvar Zıplaması)
Duvarlara çarparak zıplamanızı ve ivme kazanmanızı sağlar.
*   **Tetikleme:** `TryWallJump(wall_normal, velocity, is_requested)`

```gdscript
# GDScript Örneği
if is_on_wall() and Input.is_action_just_pressed("jump"):
    var normal = get_wall_normal()
    movement.call("TryWallJump", normal, velocity, true)
```

---

## 5. Vaulting (Engel Aşma)
Karakterin önündeki alçak engellerin üzerinden otomatik veya manuel olarak aşmasını sağlar.
*   **Tetikleme:** `ApplyVault(min_speed, max_height, duration, boost)`

```gdscript
# GDScript Örneği
if is_near_ledge and velocity.length() > 5.0:
    movement.call("ApplyVault", 5.0, 1.5, 0.4, 1.2)
```

---

## 6. Time Dilation (Zaman Yavaşlatma)
Oyun hızını yavaşlatarak oyuncuya avantaj sağlar (Örn: Düşük can veya özel yetenek).
*   **Tetikleme:** `ApplyTimeDilation(health_threshold, slow_scale, duration, cooldown)`

```gdscript
# GDScript Örneği
if current_health < max_health * 0.2:
    movement.call("ApplyTimeDilation", 0.2, 0.5, 2.0, 30.0)
```

---

## 7. Recoil Propulsion (Silah İtkisi)
Ateş edilen silahın geri tepmesini karakteri zıt yöne itmek için kullanır.
*   **Tetikleme:** `ApplyRecoil(force, immunity, vertical_bias, decay)`

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("fire"):
    var recoil_dir = -camera.get_forward_direction()
    movement.call("ApplyRecoil", 8.0, 0.1, 0.3, 5.0)
```

---

## 8. Explosion Boost (Patlama İtkisi)
Yakındaki patlamalardan alınan ivmeyi karakteri fırlatmak için kullanır.
*   **Tetikleme:** `ApplyExplosionBoost(radius, max_force, immunity, damage_threshold)`

```gdscript
# GDScript Örneği
func _on_explosion_nearby(explosion_pos):
    movement.call("ApplyExplosionBoost", 5.0, 20.0, 0.3, 10.0)
```

---

## 9. ADS Glide (Nişan Kayması)
Havada nişan alırken (ADS) düşüş hızını yavaşlatır ve havada süzülmeyi sağlar.
*   **Tetikleme:** `ApplyADSGlide(friction, retention, min_speed, duration, sensitivity_mult)`

```gdscript
# GDScript Örneği
if Input.is_action_pressed("aim") and not is_on_floor():
    movement.call("ApplyADSGlide", 2.0, 0.9, 6.0, 1.5, 0.6)
```

---

## 10. Air Momentum Transfer (İvme Zincirleme)
Yerdeki bir dash veya patlama ivmesini zıplayarak havaya aktarmanızı ve korumanızı sağlar.
*   **Tetikleme:** `ApplyAirMomentumTransfer(retention, chain_mult, window, max_chain)`

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("jump") and was_dashing:
    movement.call("ApplyAirMomentumTransfer", 0.85, 1.2, 0.25, 3)
```

---

## Önemli Notlar
1.  **MovementConfig (Resource):** **Arda (Godot Core Uzmanı & Muhasebeci)** ve **Elif (Gameplay Programcısı & Bahçıvan)**`ın önerileri doğrultusunda, karakterin temel hareket parametreleri (`WalkSpeed`, `SprintSpeed`, `MaxJumps` vb.) artık Godot `Resource` olarak tanımlanan `MovementConfig` üzerinden yönetilmektedir. Bu `Resource``u `MovementNode``un Inspector panelindeki `Config` slotuna atayarak tüm temel hareket parametrelerini Godot editöründen kolayca ayarlayabilirsiniz. **GÜNCELLEME:** `MovementConfig.cs` içerisindeki `[Export]` değerlerine C# tarafında `Mathf.Clamp` ve `Mathf.Max` ile validasyon eklenerek geçersiz değerlerin sisteme girmesi engellenmiştir.
2.  **Mekanik Parametreleri:** Her bir uzman mekaniğin kendine özgü parametreleri (örneğin `ApplyGrapple``daki `springStrength` veya `ApplySlide``daki `friction`) hala GDScript`ten `call()` metodu ile doğrudan `MovementNode``a iletilir. Bu, her mekaniğin anlık duruma göre dinamik olarak ayarlanabilmesini sağlar.
3.  **Sinyaller:** Mekanikler tetiklendiğinde `PlayerController.gd` içindeki ilgili sinyalleri (`jumped`, `slide_started`, `grapple_fired` vb.) `emit` etmeyi unutmayın. Bu, animasyon ve ses sistemleri için gereklidir.
4.  **C# Erişimi:** Tüm bu metodlar `MovementNode.cs` içinde tanımlıdır ve GDScript`ten `call()` metodu ile güvenli bir şekilde çağrılabilir.
