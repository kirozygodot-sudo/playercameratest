# Virabis Movement System Godot 4.6 Addon: Kullanım Kılavuzu ve Debug Notları

Bu belge, orijinal `playercameratest` projesinin Godot 4.6 (.NET/C#) için profesyonel bir eklentiye dönüştürülmesi ve tespit edilen kritik hataların giderilmesi sürecini detaylandırmaktadır.

## 1. Eklenti Yapısı ve Kurulum

Proje, Godot'un standart eklenti (`addons/`) klasör yapısına uygun hale getirilmiştir. Eklentiyi kullanmak için:

1.  Bu klasörü (`virabis_movement`) Godot projenizin `res://addons/` dizinine kopyalayın.
2.  Godot Editöründe `Proje > Proje Ayarları > Eklentiler` sekmesine gidin.
3.  "Virabis Movement System" eklentisini bulun ve durumunu "Etkin" olarak ayarlayın.

Eklenti etkinleştirildiğinde, `MovementNode` ve `MovementBridge` adında iki yeni düğüm tipi Godot Editöründe kullanılabilir olacaktır.

## 2. Temel Kullanım

Orijinal projedeki gibi, `PlayerController` (GDScript) `MovementNode` (C#) ve `CameraRig` (GDScript) düğümlerini yönetir. Sahne hiyerarşiniz aşağıdaki gibi olmalıdır:

```
Player (CharacterBody3D)
├── PlayerController (Node)
├── MovementNode (Node)          ← C# Movement Core
└── CameraRig (Node3D)           ← Camera Controller
```

-   **MovementNode:** Karakterin tüm hareket mantığını (hız, ivme, sürtünme, zıplama hakları, modifiye ediciler) yöneten C# çekirdeğidir. `PlayerController` bu düğümün API'sini çağırır.
-   **MovementBridge:** `MovementNode` üzerindeki C# modifiye edicilerini (Grapple, Slide, Wall Jump vb.) GDScript'ten daha kolay erişilebilir kılan bir aracı düğümdür. `PlayerController` bu düğüm üzerinden modifiye edicileri tetikler.

## 3. Yapılan Kritik Bug Düzeltmeleri ve Geliştirmeler

### 3.1. Grapple (Kanca Atma) Mekaniği Düzeltmeleri

**Sorun:** Orijinal projede `GrappleModifier` C# tarafında tanımlı olmasına rağmen, `MovementContext`'e karakterin anlık pozisyon bilgisi aktarılmadığı için kanca atma mekaniği işlevsizdi. Ayrıca `player_controller.gd`'den `GrappleModifier`'ın `StartGrapple` metodu doğru şekilde çağrılmıyordu.

**Çözüm:**

-   `MovementNode.cs` içinde `MovementContext` oluşturulurken `CurrentPosition` alanı, `Character.GlobalPosition` ile dolduruldu. Bu sayede C# çekirdeği, karakterin uzaydaki konumunu bilerek kanca mekaniğini doğru hesaplayabilir hale geldi.
-   `MovementNode.cs` içindeki `ApplyGrapple` metodu, artık `anchorPoint` (kancanın takıldığı nokta) parametresini alacak şekilde güncellendi ve `GrappleModifier`'ın `StartGrapple` metodunu doğru parametrelerle çağırıyor.
-   `MovementNode.cs`'e `ReleaseGrapple` metodu eklendi. Bu metod, aktif `GrappleModifier`'ı bulup kancayı serbest bırakmasını sağlıyor.
-   `MovementSystem.cs`'e `GetModifiers()` metodu eklendi. Bu, `MovementNode`'un aktif modifiye edicilere erişmesini ve özel işlemler yapmasını (örneğin `ReleaseGrapple` veya `TryWallJump` gibi) sağlıyor.
-   `player_controller.gd` içindeki `_update_grapple` fonksiyonu, RayCast sonucundaki `anchor_point`'i `movement.call("ApplyGrapple", anchor_point)` ile `MovementNode`'a iletiyor ve kanca bırakıldığında `movement.call("ReleaseGrapple")` çağrısını yapıyor.

**Test:** Artık `grapple` aksiyonuna basıldığında karakterin kanca atıp hedefe doğru çekilmesi ve bırakıldığında serbest kalması beklenmektedir.

### 3.2. Crouch Jump (Çömelme Zıplaması) Mekaniği Entegrasyonu

**Sorun:** `CrouchJumpModifier` C# tarafında tanımlı olmasına rağmen, `player_controller.gd` tarafından tetiklenmiyordu.

**Çözüm:**

-   `player_controller.gd` içindeki `_handle_jump` fonksiyonu güncellendi. Eğer karakter çömelme durumundaysa ve yeterli şarj süresi varsa, `movement.call("ApplyCrouchJump", ...)` çağrısı yapılarak çömelme zıplaması tetikleniyor.

**Test:** Karakter çömelip zıpladığında daha yüksek bir zıplama gerçekleştirmesi beklenmektedir.

### 3.3. Wall Jump (Duvar Zıplaması) Mekaniği Entegrasyonu

**Sorun:** `WallJumpModifier` C# tarafında tanımlı olmasına rağmen, `player_controller.gd` tarafından tetiklenmiyordu ve duvar algılama mantığı C# modifiye edicisi ile entegre değildi.

**Çözüm:**

-   `MovementNode.cs`'e `TryWallJump` metodu eklendi. Bu metod, `WallJumpModifier`'ın `TryWallBounce` metodunu çağırarak duvar zıplamasını C# çekirdeği üzerinden yönetiyor.
-   `player_controller.gd` içindeki `_handle_wall_jump` fonksiyonu güncellendi. Duvar algılandığında, `movement.call("TryWallJump", wall_normal, current_velocity, true)` çağrısı yapılarak C# tarafındaki duvar zıplama mantığı tetikleniyor.

**Test:** Karakter duvara yakınken zıplama tuşuna bastığında duvardan sekerek zıplaması beklenmektedir.

### 3.4. Slide (Kayma) Mekaniği Basitleştirmesi

**Sorun:** `player_controller.gd` içindeki `_handle_slide` fonksiyonu, C# `SlideModifier`'ın iç state'ini tekrar eden bir `_is_sliding` ve `_slide_timer` değişkenleri ile kendi içinde yönetiyordu. Bu, kod tekrarına ve potansiyel senkronizasyon sorunlarına yol açıyordu.

**Çözüm:**

-   `MovementNode.cs`'e `PerformSlideAttack` metodu eklendi. Bu metod, aktif `SlideModifier`'ı bulup `PerformAttack` metodunu çağırıyor.
-   `player_controller.gd` içindeki `_handle_slide` fonksiyonundan `_is_sliding` ve `_slide_timer` değişkenleri kaldırıldı. Artık slide başlatma ve slide saldırısı tetikleme doğrudan `MovementNode` üzerindeki `ApplySlide` ve `PerformSlideAttack` metotları aracılığıyla yapılıyor. `SlideModifier`'ın kendi iç mantığı, slide süresini ve durumunu yönetiyor.

**Test:** Karakter sprint yaparken çömeldiğinde kaymaya başlaması ve kayarken saldırı tuşuna bastığında slide saldırısı yapması beklenmektedir.

## 4. Sonraki Adımlar ve Öneriler

Bu eklenti artık Godot 4.6 projenizde daha kararlı ve işlevsel bir hareket sistemi sunmaktadır. Ancak geliştirme süreci devam edebilir:

-   **Test Kapsamı:** Tüm 10 uzman mekaniğin (Vault, Time Dilation, Recoil Propulsion vb.) `player_controller.gd` veya benzeri bir giriş yöneticisi tarafından tetiklendiğinden emin olun.
-   **Animasyon Entegrasyonu:** Hareket durumlarına (Idle, Moving, Sprinting, Airborne, Grappling, Sliding vb.) göre animasyonları tetiklemek için `MovementNode`'un `StateChangedEventHandler` sinyalini kullanın.
-   **Görsel Geri Bildirim:** Grapple kancası için görsel bir ip, slide için parçacık efektleri gibi görsel geri bildirimler ekleyin.
-   **Ayarlanabilirlik:** `MovementNode` ve `MovementBridge` üzerindeki `[Export]` değişkenlerini kullanarak Godot Editöründen mekanik parametrelerini kolayca ayarlayın.

Bu eklenti, Godot projelerinizde gelişmiş karakter hareket sistemleri oluşturmanız için sağlam bir temel sağlamaktadır. İyi geliştirmeler!
