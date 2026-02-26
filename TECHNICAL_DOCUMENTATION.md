# Virabis Movement System: Teknik Dokümantasyon (Godot 4.6 .NET)

Bu dokümantasyon, **Godot 4.6** motoru üzerinde geliştirilen, C# çekirdekli ve GDScript arayüzlü gelişmiş bir karakter hareket sistemini detaylandırmaktadır. Sistem, yüksek performanslı bir hareket çekirdeği ile 10 adet uzman seviye mekaniği bir araya getirir.

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
*   **Grapple Hook (Kanca):** Fizik tabanlı çekme ve fırlatma sistemi. `anchorPoint` üzerinden yay kuvveti hesaplar.
*   **Slide (Kayma):** Koşma sırasında tetiklenen, düşük sürtünmeli hareket. Slide sırasında yapılan saldırılar hasar bonusu alır.
*   **Crouch Jump:** Çömelme sırasında şarj edilen ve normalden %150 daha yüksek zıplama sağlayan mekanik.

### 2.2. İleri Seviye Mekanikler
*   **Wall Jump Combo:** Duvarlardan sekerek zıplama ve her başarılı sekmede ivme kazanma.
*   **Vaulting:** Belirli bir yükseklikteki engellerin üzerinden otomatik olarak aşma.
*   **Air Momentum Transfer:** Dash veya patlama sonrası kazanılan ivmenin havada korunması ve zincirlenmesi.
*   **Time Dilation:** Düşük can durumunda veya özel yetenek tetiklendiğinde zamanı yavaşlatma (Slow-mo).

---

## 3. Teknik Uygulama Detayları

### 3.1. MovementConfig (Resource) ve Veri Akışı
**Arda (Godot Core Uzmanı & Muhasebeci)** ve **Elif (Gameplay Programcısı & Bahçıvan)**'ın önerileri doğrultusunda, tüm hareket parametreleri artık Godot `Resource` olarak tanımlanan `MovementConfig` sınıfı üzerinden yönetilmektedir. Bu, parametrelerin Godot editöründen kolayca ayarlanabilmesini, farklı karakterler için farklı konfigürasyonlar oluşturulabilmesini ve kodun daha modüler olmasını sağlar.

Her fizik karesinde (`_PhysicsProcess`), `MovementNode` bir `MovementContext` nesnesi oluşturur. Bu nesne şunları içerir:
*   `InputDirection`: Kamera açısına göre normalize edilmiş yön.
*   `CurrentPosition`: Karakterin dünya üzerindeki anlık konumu (Grapple için kritik).
*   `IsOnFloor`: Zemin teması bilgisi.
*   `DeltaTime`: Kareler arası süre.

### 3.2. Modifiye Edici Pipeline (Modifier Pipeline)
C# çekirdeği, aktif olan tüm modifiye edicileri bir liste içinde tutar. Her karede:
1.  Süresi dolan modifiye ediciler temizlenir.
2.  Kalanlar, karakterin hızını (Velocity) sırayla işler.
3.  Sonuç hız, Godot'un `MoveAndSlide()` fonksiyonuna iletilir.

---

## 4. Debug ve İzleme Sistemi

Sistem, geliştirme sürecini kolaylaştırmak için kapsamlı bir debug altyapısı sunar.

### 4.1. Görsel Debug Paneli
`debug_panel.gd` aracılığıyla ekranda canlı olarak izlenebilen veriler:
*   **Current State:** `Idle`, `Moving`, `Sprinting`, `Airborne`, `Flying`.
*   **Active Modifiers:** O an çalışan tüm mekaniklerin listesi ve iç değişkenleri.
*   **Jump Buffer:** Kalan zıplama hakları ve Coyote Time durumu.

### 4.2. Hata Ayıklama (Debugging)
*   **C# Tarafı:** `GD.Print()` ile konsol çıktıları ve VS Code üzerinden .NET Debugger desteği.
*   **GDScript Tarafı:** `print_rich()` ile renkli konsol logları ve yerleşik breakpoint desteği.

---

### 5. Kurulum ve Entegrasyon

1.  `addons/virabis_movement` klasörünü projenize kopyalayın.
2.  **Project Settings > Plugins** sekmesinden eklentiyi aktif edin.
3.  Karakterinize `MovementNode` ekleyin ve `Character` referansını bağlayın.
4.  `MovementNode` üzerindeki `Config` slotuna yeni bir `MovementConfig` Resource oluşturup atayın ve parametreleri ayarlayın.
5.  `PlayerController.gd` üzerinden girdi (input) yönlendirmelerini yapın.
6.  **Emre (Optimizasyon Gurusu & Minimalist Yaşam Koçu)** ve **Zeynep (QA/Test Uzmanı & Dedektif)**'in önerileri doğrultusunda, `player_controller.gd` içindeki `_check_landing` fonksiyonu `MovementNode`'un `StateChanged` sinyali ile tetiklenecek şekilde optimize edilmiştir. Ayrıca `_handle_crouch` fonksiyonu kaldırılmış, `_crouch_charge` yönetimi `_physics_process` içine taşınarak daha event-driven bir yapıya geçilmiştir.
---

## 6. Referanslar ve Kaynaklar
*   [Godot 4.6 .NET Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
*   [Virabis Movement Core API Reference](res://addons/virabis_movement/core/README.md)

> **Not:** Bu sistem Manus AI tarafından Virabis projesi için özel olarak optimize edilmiştir.
