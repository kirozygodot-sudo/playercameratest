# Virabis Movement System: Teknik Dokümantasyon (Godot 4.6 .NET)

Bu dokümantasyon, **Godot 4.6** motoru üzerinde geliştirilen, C# çekirdekli ve GDScript arayüzlü gelişmiş bir karakter hareket sistemini detaylandırmaktadır. Sistem, yüksek performanslı bir hareket çekirdeği ile 10 adet uzman seviye mekaniği bir araya getirir. Bu güncelleme ile kamera sistemi önemli ölçüde geliştirilmiş ve kritik hatalar giderilmiştir.

## 1. Sistem Mimarisi

Sistem, **Hibrit Mimari** prensibiyle tasarlanmıştır. Bu yapı, C#'ın hesaplama gücü ile GDScript'in esnekliğini birleştirir.

| Katman | Teknoloji | Sorumluluk |
| :--- | :--- | :--- |
| **Core (Çekirdek)** | Pure C# | Fizik hesaplamaları, ivmelenme, sürtünme ve modifiye edici pipeline yönetimi. |
| **Bridge (Köprü)** | C# (.NET) | Godot düğümleri (Nodes) ile çekirdek arasındaki veri akışı ve sinyal yönetimi. |
| **Controller (Kontrolcü)** | GDScript | Kullanıcı girdileri (Input), kamera yönü hesaplamaları ve görsel efekt tetikleyicileri. |

---

## 2. Uzman Hareket Mekanikleri

Sistem, `IMovementModifier` arayüzü üzerinden çalışan 10 adet dinamik mekanik içerir.

### 2.1. Temel Mekanikler
*   **Grapple Hook (Kanca):** Geliştirilmiş fizik tabanlı çekme ve fırlatma sistemi. `anchorPoint` üzerinden yay kuvveti hesaplar ve damping kuvveti çekim kuvvetini tamamen sıfırlamaz, her zaman hedefe yönelim sağlar.
*   **Slide (Kayma):** Koşma sırasında tetiklenen, düşük sürtünmeli hareket. Slide sırasında yapılan saldırılar hasar bonusu alır.
*   **Crouch Jump:** Çömelme sırasında şarj edilen ve normalden %150 daha yüksek zıplama sağlayan mekanik.

### 2.2. İleri Seviye Mekanikler
*   **Wall Jump Combo:** Duvarlardan sekerek zıplama ve her başarılı sekmede ivme kazanma.
*   **Vaulting:** Belirli bir yükseklikteki engellerin üzerinden otomatik olarak aşma.
*   **Air Momentum Transfer:** Dash veya patlama sonrası kazanılan ivmenin havada korunması ve zincirlenmesi.
*   **Time Dilation:** Düşük can durumunda veya özel yetenek tetiklendiğinde zamanı yavaşlatma (Slow-mo).

---

## 3. Kamera Sistemi Geliştirmeleri

Kamera sistemi, daha dinamik ve tepkisel bir deneyim sunmak üzere önemli ölçüde geliştirilmiştir.

### 3.1. Gelişmiş Kamera Sarsıntısı (Camera Shake)
`orbit_camera_mode.gd` içerisinde `FastNoiseLite` tabanlı, organik ve sürekli titreme hissi veren bir sarsıntı sistemi entegre edilmiştir. Farklı oyun içi olaylar için önceden tanımlanmış sarsıntı profilleri mevcuttur:

| Profil Adı | Açıklama | Frekans (Hz) | Pozisyon Genliği (m) | Rotasyon Genliği (derece) | Azalma Hızı |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **LANDING** | Orta genlik, düşük frekans — ağır iniş hissi | 8.0 | 0.06 | 0.8 | 4.5 |
| **EXPLOSION** | Yüksek genlik, yüksek frekans — patlama sarsıntısı | 20.0 | 0.12 | 1.5 | 6.0 |
| **JUMP** | Düşük genlik, orta frekans — hafif zıplama titremesi | 12.0 | 0.03 | 0.4 | 7.0 |
| **IMPACT** | Orta-yüksek genlik, çok yüksek frekans — ani darbe | 30.0 | 0.08 | 1.0 | 8.0 |
| **CUSTOM** | Dışarıdan tam kontrol edilebilir profil | Değişken | Değişken | Değişken | Değişken |

Sarsıntı, `add_shake(amount, profile)` metodu ile tetiklenir ve `_shake_amount` değeri 0.0 ile 1.0 arasında normalize edilerek mevcut sarsıntıya eklenir. Her profil için ayrı `FastNoiseLite` örnekleri kullanılarak daha çeşitli ve doğal sarsıntı efektleri elde edilir.

### 3.2. Dinamik FOV (Field of View)
Kamera FOV'u artık karakterin hızına ve durumuna (nişan alma, sprint) göre dinamik olarak ayarlanmaktadır. Öncelik sırası şu şekildedir: Nişan Alma (Aim) > Sprint > Hız Bazlı FOV. Hız bazlı FOV, karakterin hızı arttıkça FOV'u genişletir ve bir ease-in eğrisi ile yumuşak geçişler sağlar.

### 3.3. Kamera Modları ve Geçişler
*   **FirstPersonCameraMode:** `CameraModeBase`'den türetilmiş yeni bir birinci şahıs kamera modu eklenmiştir. Bu modda `SpringArm3D`'nin uzunluğu sıfırlanır ve kamera doğrudan karakterin göz hizasına (head_offset) yerleştirilir. Daha tepkisel bir `rotation_smooth` değeri kullanır.
*   **Pürüzsüz Kamera Geçişleri:** `CameraController.gd` içerisinde kamera modları arasında geçiş yaparken (örneğin TPS'ten FPS'e) `Tween` kullanarak FOV değerleri arasında pürüzsüz `lerp` animasyonları sağlanmıştır.

### 3.4. Hedef Kilitlenme (Target Lock-on)
`CameraController.gd`'ye düşmanlara kilitlenme mekaniği entegre edilmiştir. `toggle_lock_on()` metodu ile en yakın düşman (belirlenen `lock_on_group` içindeki) hedeflenir ve kamera hedefe doğru yumuşak bir şekilde döner. Kilitlenme sırasında kamera inputları hedefe yönelimi koruyacak şekilde adapte edilir.

### 3.5. SpringArm3D Optimizasyonu
`SpringArm3D`'nin `collision_margin` değeri 0.5'ten 0.1'e düşürülmüş ve `shape` özelliği `SphereShape3D` olarak ayarlanmıştır. Bu optimizasyonlar, dar alanlarda kameranın titremesini ve takılmasını önleyerek daha akıcı bir deneyim sunar.

---

## 4. Kritik Bug Çözümleri

Projenin kararlılığını ve oynanabilirliğini artıran önemli hata düzeltmeleri yapılmıştır.

### 4.1. Property 2 (Grapple) - Geliştirilmiş Spring-Damper Fiziği
`GrappleModifier.cs` içerisindeki spring-damper fiziği güncellenmiştir. Artık damping kuvveti, çekim kuvvetini tamamen sıfırlamak yerine, hızın ip yönündeki bileşenine (radial velocity) uygulanır. Bu sayede, grappling sırasında karakterin hedefe doğru yönelimi her zaman korunur ve daha gerçekçi bir salınım (swing) mekaniği elde edilir. Ayrıca, ipin maksimum uzunluğuna %20'lik bir tolerans eklenerek ani kopmaların önüne geçilmiştir.

### 4.2. Property 12 (Config) - `MovementConfig.cs` Validasyonu
`MovementConfig.cs` içerisindeki `[Export]` değerlerine C# tarafında `Mathf.Clamp` ve `Mathf.Max` ile validasyon eklenmiştir. Bu sayede, Godot editöründen veya kod üzerinden geçersiz (negatif hız, sıfır ivme vb.) değerlerin sisteme girmesi engellenerek, hareket sisteminin beklenmedik davranışlar sergilemesinin önüne geçilmiştir. Örneğin, hız değerleri için minimum 0.1f, ivme ve sürtünme değerleri için minimum 0.0f, `AirControlMultiplier` için 0.0f ile 2.0f arası ve `MaxJumps` için minimum 0 gibi kısıtlamalar getirilmiştir.

---

## 5. Teknik Uygulama Detayları

### 5.1. MovementConfig (Resource) ve Veri Akışı
**Arda (Godot Core Uzmanı & Muhasebeci)** ve **Elif (Gameplay Programcısı & Bahçıvan)**'ın önerileri doğrultusunda, tüm hareket parametreleri artık Godot `Resource` olarak tanımlanan `MovementConfig` sınıfı üzerinden yönetilmektedir. Bu, parametrelerin Godot editöründen kolayca ayarlanabilmesini, farklı karakterler için farklı konfigürasyonlar oluşturulabilmesini ve kodun daha modüler olmasını sağlar.

Her fizik karesinde (`_PhysicsProcess`), `MovementNode` bir `MovementContext` nesnesi oluşturur. Bu nesne şunları içerir:
*   `InputDirection`: Kamera açısına göre normalize edilmiş yön.
*   `CurrentPosition`: Karakterin dünya üzerindeki anlık konumu (Grapple için kritik).
*   `IsOnFloor`: Zemin teması bilgisi.
*   `DeltaTime`: Kareler arası süre.

### 5.2. Modifiye Edici Pipeline (Modifier Pipeline)
C# çekirdeği, aktif olan tüm modifiye edicileri bir liste içinde tutar. Her karede:
1.  Süresi dolan modifiye ediciler temizlenir.
2.  Kalanlar, karakterin hızını (Velocity) sırayla işler.
3.  Sonuç hız, Godot'un `MoveAndSlide()` fonksiyonuna iletilir.

---

## 6. Debug ve İzleme Sistemi

Sistem, geliştirme sürecini kolaylaştırmak için kapsamlı bir debug altyapısı sunar.

### 6.1. Görsel Debug Paneli
`debug_panel.gd` aracılığıyla ekranda canlı olarak izlenebilen veriler:
*   **Current State:** `Idle`, `Moving`, `Sprinting`, `Airborne`, `Flying`.
*   **Active Modifiers:** O an çalışan tüm mekaniklerin listesi ve iç değişkenleri.
*   **Jump Buffer:** Kalan zıplama hakları ve Coyote Time durumu.

### 6.2. Hata Ayıklama (Debugging)
*   **C# Tarafı:** `GD.Print()` ile konsol çıktıları ve VS Code üzerinden .NET Debugger desteği.
*   **GDScript Tarafı:** `print_rich()` ile renkli konsol logları ve yerleşik breakpoint desteği.

---

### 7. Kurulum ve Entegrasyon

1.  `addons/virabis_movement` klasörünü projenize kopyalayın.
2.  **Project Settings > Plugins** sekmesinden eklentiyi aktif edin.
3.  Karakterinize `MovementNode` ekleyin ve `Character` referansını bağlayın.
4.  `MovementNode` üzerindeki `Config` slotuna yeni bir `MovementConfig` Resource oluşturup atayın ve parametreleri ayarlayın.
5.  `PlayerController.gd` üzerinden girdi (input) yönlendirmelerini yapın.
6.  **Emre (Optimizasyon Gurusu & Minimalist Yaşam Koçu)** ve **Zeynep (QA/Test Uzmanı & Dedektif)**'in önerileri doğrultusunda, `player_controller.gd` içindeki `_check_landing` fonksiyonu `MovementNode`'un `StateChanged` sinyali ile tetiklenecek şekilde optimize edilmiştir. Ayrıca `_handle_crouch` fonksiyonu kaldırılmış, `_crouch_charge` yönetimi `_physics_process` içine taşınarak daha event-driven bir yapıya geçilmiştir.

---

## 8. Referanslar ve Kaynaklar
*   [Godot 4.6 .NET Documentation][1]
*   [Virabis Movement Core API Reference][2]

[1]: https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html
[2]: res://addons/virabis_movement/core/README.md

> **Not:** Bu sistem Manus AI tarafından Virabis projesi için özel olarak optimize edilmiştir.
