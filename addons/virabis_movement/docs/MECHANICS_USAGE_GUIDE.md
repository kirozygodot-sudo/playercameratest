# Virabis Movement: 10 Uzman Mekanik Kullanım Rehberi (Tak-Çalıştır)

Bu rehber, Virabis Movement System içindeki 10 gelişmiş hareket mekaniğinin her birini, farklı uzmanlık alanlarından profesyonellerin bakış açısıyla detaylandırır. Amacımız, her mekaniğin nasıl çalıştığını, nasıl entegre edileceğini ve oyun deneyimine nasıl katkıda bulunduğunu "Tak-Çalıştır" prensibiyle açıklamaktır.

## 1. Grapple Hook (Kanca Atma)

**Mekanik Özeti:** Karakteri bir hedef noktaya doğru fiziksel olarak çeker ve bırakıldığında ivme kazandırır. Geliştirilmiş spring-damper fiziği sayesinde, damping kuvveti çekim kuvvetini tamamen sıfırlamaz, her zaman hedefe yönelim sağlar. Bu, daha gerçekçi ve kontrol edilebilir bir salınım (swing) mekaniği sunar.

**Core Developer (C#) Notu:** `GrappleModifier.cs` içinde `_springStrength`, `_damping`, `_maxLength`, `_minLength`, `_pullSpeed` ve `_launchBoost` parametreleri ile ayarlanır. Damping hesaplaması, hızın ip yönündeki bileşenine (radial velocity) uygulanarak hedefe yönelimi korur. Maksimum ip uzunluğuna %20 tolerans eklenmiştir.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyGrapple", anchor_point)`
*   **Bırakma:** `movement.call("ReleaseGrapple")`
*   `anchor_point`, genellikle bir raycast sonucu elde edilen `Vector3` pozisyonudur.

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("grapple"):
    var result = get_raycast_result() # Raycast ile hedef nokta bul
    if result:
        movement.call("ApplyGrapple", result.position)

if Input.is_action_just_released("grapple"):
    movement.call("ReleaseGrapple")
```

**Technical Designer Notu:** `GrappleModifier` parametreleri, `ApplyGrapple` metoduna doğrudan geçirilerek veya `MovementConfig` içinde varsayılan değerler tanımlanarak ayarlanabilir. `springStrength` ve `damping` değerleri, kancanın ne kadar "sert" veya "yumuşak" hissettireceğini belirler. `maxLength` ve `minLength` ise ipin fiziksel sınırlarını tanımlar.

**UX Designer Notu:** Oyuncuya kancanın hedefe kilitlendiğini gösteren görsel bir geri bildirim (örneğin, nişangah değişimi) ve başarılı bir çekişte tatmin edici bir ses efekti eklenmesi, mekaniğin hissiyatını artıracaktır.

---

## 2. Slide & Melee Combo (Kayma ve Saldırı)

**Mekanik Özeti:** Hızlı hareket ederken yere çömelerek kaymanızı sağlar. Kayma sırasında yapılan saldırılar hasar bonusu alabilir.

**Core Developer (C#) Notu:** `SlideModifier.cs` içinde `_friction`, `_maxDuration`, `_exitMomentum`, `_attackDamageMult` ve `_attackSpeedBoost` parametreleri ile yönetilir. Kayma başladığında karakterin mevcut yatay hızını korur ve sürtünme ile yavaşlatır.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplySlide", friction, duration, exit_momentum, damage_mult, speed_boost)`
*   Kayma sırasında saldırı yapmak için ayrı bir `PerformSlideAttack()` metodu çağrılabilir.

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("crouch") and is_sprinting:
    movement.call("ApplySlide", 3.0, 2.0, 0.6, 2.0, 1.5)

# Eğer kayma sırasında özel bir saldırı varsa
if Input.is_action_just_pressed("attack") and movement.is_sliding(): # is_sliding() MovementNode'a eklenebilir
    movement.call("PerformSlideAttack")
```

**Technical Designer Notu:** `friction` değeri kaymanın ne kadar hızlı yavaşlayacağını, `maxDuration` ise kaymanın maksimum süresini belirler. `exitMomentum` kayma bittiğinde karakterin ne kadar hızla devam edeceğini kontrol eder. `attackDamageMult` ve `attackSpeedBoost` değerleri, kayma saldırılarını dengelemek için önemlidir.

**Animator Notu:** Kayma başlangıcı, devamı ve bitişi için akıcı animasyon geçişleri, mekaniğin görsel kalitesini artırır. Kayma sırasında karakterin pozisyonu ve rotasyonu doğru bir şekilde yansıtılmalıdır.

---

## 3. Crouch Jump (Yüksek Zıplama)

**Mekanik Özeti:** Çömelme süresine bağlı olarak daha yükseğe zıplamanızı sağlar.

**Core Developer (C#) Notu:** `CrouchJumpModifier.cs` (varsa) veya `MovementSystem` içinde çömelme süresi ve zıplama yüksekliği arasındaki ilişkiyi yönetir. Şarj süresi arttıkça uygulanan zıplama kuvvetini artırır.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyCrouchJump", height_mult, window, charge_time, min_charge)`
*   `charge_time`, çömelme tuşuna basılı tutulan süredir.

```gdscript
# GDScript Örneği
var crouch_timer = 0.0

func _process(delta):
    if Input.is_action_pressed("crouch"):
        crouch_timer += delta
    else:
        crouch_timer = 0.0

if Input.is_action_just_pressed("jump") and is_crouching:
    movement.call("ApplyCrouchJump", 1.5, 0.1, crouch_timer, 0.3)
```

**UX Designer Notu:** Çömelme sırasında şarj olduğunu gösteren görsel bir geri bildirim (örneğin, karakterin hafifçe alçalması veya bir şarj barı) ve maksimum şarja ulaşıldığında sesli bir uyarı, oyuncuya mekaniği daha iyi hissettirir.

---

## 4. Wall Jump (Duvar Zıplaması)

**Mekanik Özeti:** Duvarlara çarparak zıplamanızı ve her başarılı sekmede ivme kazanmanızı sağlar.

**Core Developer (C#) Notu:** `WallJumpModifier.cs` (varsa) veya `MovementSystem` içinde duvar normali ve mevcut hıza göre zıplama kuvvetini hesaplar. `TryWallJump` metodu, duvarın yüzey normalini kullanarak karakteri duvardan iter.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("TryWallJump", wall_normal, velocity, is_requested)`
*   `wall_normal`, çarpılan duvarın yüzey normalidir. `is_requested` oyuncunun zıplama tuşuna basıp basmadığını belirtir.

```gdscript
# GDScript Örneği
if is_on_wall() and Input.is_action_just_pressed("jump"):
    var normal = get_wall_normal() # Karakterin çarptığı duvarın normalini al
    movement.call("TryWallJump", normal, velocity, true)
```

**Level Designer Notu:** Duvar zıplamasının etkili olabilmesi için seviye tasarımında dikey engeller ve duvar yüzeyleri uygun şekilde yerleştirilmelidir. Oyuncunun duvar zıplaması ile ulaşabileceği alanlar tasarlanırken mekaniğin menzili göz önünde bulundurulmalıdır.

---

## 5. Vaulting (Engel Aşma)

**Mekanik Özeti:** Karakterin önündeki alçak engellerin üzerinden otomatik veya manuel olarak aşmasını sağlar.

**Core Developer (C#) Notu:** `VaultModifier.cs` (varsa) veya `MovementSystem` içinde engel yüksekliği, karakter hızı ve aşma süresi gibi parametreleri yönetir. Karakterin engelin üzerinden pürüzsüz bir eğri ile geçmesini sağlar.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyVault", min_speed, max_height, duration, boost)`
*   `min_speed`, vaulting için gereken minimum hızı; `max_height`, aşılacak engelin maksimum yüksekliğini; `duration`, aşma animasyonunun süresini; `boost`, aşma sonrası kazanılacak ek ivmeyi belirtir.

```gdscript
# GDScript Örneği
# Karakterin önünde bir engel olup olmadığını kontrol et
if is_near_ledge() and velocity.length() > 5.0:
    movement.call("ApplyVault", 5.0, 1.5, 0.4, 1.2)
```

**Animator Notu:** Vaulting animasyonu, karakterin engelin üzerinden doğal ve akıcı bir şekilde geçmesini sağlamalıdır. Animasyonun süresi (`duration`) ve karakterin pozisyonu (`boost`) arasındaki senkronizasyon önemlidir.

---

## 6. Time Dilation (Zaman Yavaşlatma)

**Mekanik Özeti:** Oyun hızını yavaşlatarak oyuncuya avantaj sağlar (Örn: Düşük can durumu veya özel yetenek tetiklendiğinde).

**Core Developer (C#) Notu:** `TimeDilationModifier.cs` (varsa) veya `MovementSystem` içinde `Engine.time_scale` değerini manipüle eder. Belirli bir süre boyunca veya bir koşul karşılanana kadar zamanı yavaşlatır.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyTimeDilation", health_threshold, slow_scale, duration, cooldown)`
*   `health_threshold`, zaman yavaşlatmanın tetikleneceği can yüzdesini; `slow_scale`, zamanın ne kadar yavaşlayacağını (0.0-1.0 arası); `duration`, yavaşlatmanın süresini; `cooldown`, tekrar tetiklenebilmesi için geçmesi gereken süreyi belirtir.

```gdscript
# GDScript Örneği
if current_health < max_health * 0.2 and not movement.is_time_dilated():
    movement.call("ApplyTimeDilation", 0.2, 0.5, 2.0, 30.0)
```

**UX Designer Notu:** Zaman yavaşlatma efekti, görsel (ekran efektleri, bulanıklık) ve işitsel (ses perdesi değişimi, yavaşlayan sesler) geri bildirimlerle desteklenmelidir. Oyuncuya ne zaman aktif olduğunu ve ne zaman biteceğini açıkça bildirmelidir.

---

## 7. Recoil Propulsion (Silah İtkisi)

**Mekanik Özeti:** Ateş edilen silahın geri tepmesini karakteri zıt yöne itmek için kullanır.

**Core Developer (C#) Notu:** `RecoilModifier.cs` (varsa) veya `MovementSystem` içinde uygulanan kuvveti, geri tepme yönünü ve etki süresini yönetir. Karakterin hızına anlık bir itki ekler.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyRecoil", force, immunity, vertical_bias, decay)`
*   `force`, geri tepme kuvvetinin büyüklüğünü; `immunity`, geri tepmeye karşı kısa süreli bağışıklık süresini; `vertical_bias`, dikey yöndeki itkinin oranını; `decay`, kuvvetin ne kadar hızlı azalacağını belirtir.

```gdscript
# GDScript Örneği
if Input.is_action_just_pressed("fire"):
    var recoil_dir = -camera.get_forward_direction() # Kameranın baktığı yönün tersi
    movement.call("ApplyRecoil", 8.0, 0.1, 0.3, 5.0)
```

**Sound Designer Notu:** Silahın ateşlenme sesiyle senkronize, kısa ve keskin bir geri tepme sesi, mekaniğin etkisini artırır. Karakterin hareketine bağlı olarak sesin şiddeti veya tonu değişebilir.

---

## 8. Explosion Boost (Patlama İtkisi)

**Mekanik Özeti:** Yakındaki patlamalardan alınan ivmeyi karakteri fırlatmak için kullanır.

**Core Developer (C#) Notu:** `ExplosionBoostModifier.cs` (varsa) veya `MovementSystem` içinde patlama merkezine olan uzaklığa ve patlama kuvvetine göre karakterin hızına bir itki uygular. `immunity` süresi boyunca tekrar tetiklenmesini engeller.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyExplosionBoost", radius, max_force, immunity, damage_threshold)`
*   `radius`, patlamanın etki alanını; `max_force`, maksimum itki kuvvetini; `immunity`, tekrar tetiklenmeye karşı bağışıklık süresini; `damage_threshold`, itki için gereken minimum patlama hasarını belirtir.

```gdscript
# GDScript Örneği
func _on_explosion_nearby(explosion_pos):
    # Patlama pozisyonu ve karakterin pozisyonuna göre mesafe hesaplanabilir
    movement.call("ApplyExplosionBoost", 5.0, 20.0, 0.3, 10.0)
```

**Level Designer Notu:** Patlama itkisinin seviye tasarımında stratejik olarak kullanılabileceği alanlar (örneğin, yüksek platformlara ulaşmak için) oluşturulabilir. Patlamaların yerleşimi ve kuvveti, oyuncunun bu mekaniği nasıl kullanacağını etkiler.

---

## 9. ADS Glide (Nişan Kayması)

**Mekanik Özeti:** Havada nişan alırken (ADS - Aim Down Sights) düşüş hızını yavaşlatır ve havada süzülmeyi sağlar.

**Core Developer (C#) Notu:** `ADSGlideModifier.cs` (varsa) veya `MovementSystem` içinde karakterin dikey hızını azaltır ve yatay hızını korur. `friction` ve `retention` parametreleri ile süzülme hissiyatı ayarlanır.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyADSGlide", friction, retention, min_speed, duration, sensitivity_mult)`
*   `friction`, süzülme sırasındaki hava sürtünmesini; `retention`, yatay hızın ne kadar korunacağını; `min_speed`, süzülme için gereken minimum hızı; `duration`, süzülmenin maksimum süresini; `sensitivity_mult`, nişan alma hassasiyet çarpanını belirtir.

```gdscript
# GDScript Örneği
if Input.is_action_pressed("aim") and not is_on_floor():
    movement.call("ApplyADSGlide", 2.0, 0.9, 6.0, 1.5, 0.6)
```

**UX Designer Notu:** ADS Glide aktif olduğunda ekranda hafif bir görsel efekt (örneğin, rüzgar çizgileri) ve süzülme sesi, oyuncuya mekaniğin aktif olduğunu ve etkisini hissettirir. Nişan alma hassasiyetinin değişimi, oyuncunun daha hassas atışlar yapmasına olanak tanır.

---

## 10. Air Momentum Transfer (İvme Zincirleme)

**Mekanik Özeti:** Yerdeki bir dash veya patlama ivmesini zıplayarak havaya aktarmanızı ve korumanızı sağlar.

**Core Developer (C#) Notu:** `AirMomentumTransferModifier.cs` (varsa) veya `MovementSystem` içinde yerdeki ivmeyi yakalar ve `retention` ile `chain_mult` parametrelerine göre havada korur veya artırır. `window` ve `max_chain` ile zincirleme mekaniğini yönetir.

**Gameplay Programmer (GDScript) Notu:**
*   **Tetikleme:** `movement.call("ApplyAirMomentumTransfer", retention, chain_mult, window, max_chain)`
*   `retention`, ivmenin ne kadar korunacağını; `chain_mult`, her zincirlemede ivmenin ne kadar artacağını; `window`, ivmeyi yakalamak için zaman penceresini; `max_chain`, maksimum zincirleme sayısını belirtir.

```gdscript
# GDScript Örneği
# Varsayalım ki 'was_dashing' bir önceki frame'de dash yapıldığını tutuyor
if Input.is_action_just_pressed("jump") and was_dashing:
    movement.call("ApplyAirMomentumTransfer", 0.85, 1.2, 0.25, 3)
```

**Animator Notu:** İvme zincirleme sırasında karakterin hızına ve yönüne uygun, dinamik animasyonlar (örneğin, havada süzülme veya hızlanma animasyonları) mekaniğin görsel etkisini artırır. Her zincirlemede hafif bir görsel veya işitsel geri bildirim, oyuncuya başarısını hissettirir.

---

## Genel Kurulum ve Entegrasyon (Tüm Uzmanlar İçin)

1.  `addons/virabis_movement` klasörünü projenize kopyalayın.
2.  **Project Settings > Plugins** sekmesinden eklentiyi aktif edin.
3.  Karakterinize `MovementNode` ekleyin ve `Character` referansını bağlayın.
4.  `MovementNode` üzerindeki `Config` slotuna yeni bir `MovementConfig` Resource oluşturup atayın veya hazır presetlerden (`ninja_preset.tres`, `tank_preset.tres`, `god_preset.tres`) birini seçerek parametreleri ayarlayın.
5.  `PlayerController.gd` üzerinden girdi (input) yönlendirmelerini yapın.

---

## Referanslar ve Kaynaklar
*   [Godot 4.6 .NET Documentation][1]
*   [Virabis Movement Core API Reference][2]

[1]: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html
[2]: res://addons/virabis_movement/core/README.md

> **Not:** Bu sistem Manus AI tarafından Virabis projesi için özel olarak optimize edilmiştir.
