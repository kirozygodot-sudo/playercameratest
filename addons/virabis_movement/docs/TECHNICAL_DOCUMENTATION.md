# Virabis Movement System: Tak-Çalıştır Teknik Dokümantasyon (Godot 4.6 .NET)

Bu dokümantasyon, **Godot 4.6** motoru üzerinde geliştirilen, C# çekirdekli ve GDScript arayüzlü gelişmiş bir karakter hareket sistemini, farklı uzmanlık alanlarından 10 profesyonelin bakış açısıyla detaylandırmaktadır. Amacımız, sistemin her bileşenini "Tak-Çalıştır" prensibiyle açıklamak, böylece her ekip üyesinin kendi sorumluluk alanındaki parçayı kolayca anlayıp entegre edebilmesini sağlamaktır.

## 1. Proje Yöneticisi Perspektifi: Sisteme Genel Bakış

**Sorumluluk Alanı:** Proje hedefleri, ekip koordinasyonu, zaman çizelgesi ve kaynak yönetimi.

Virabis Movement System, Godot 4.6 için geliştirilmiş, yüksek performanslı ve modüler bir karakter hareket çözümüdür. C# tabanlı bir çekirdek (Core) ile GDScript tabanlı bir kontrol katmanını (Controller) birleştirerek esneklik ve performans dengesi sunar. Sistem, 10 uzman seviye hareket mekaniği, dinamik kamera sistemleri ve geliştirilmiş hata ayıklama araçları içerir. Son güncellemelerle birlikte, mimari olarak daha da güçlendirilmiş, "sudo" yetkileri için Decorator Pattern, akıcı kamera geçişleri için bir Mikser, kolay konfigürasyon için Data-Driven Preset Kütüphanesi, optimize edilmiş state yönetimi için "The Pulse", mekanikler arası iletişim için "Kinetic Chain" ve esnek yetki yönetimi için "Ghost Logic" eklenmiştir. Bu yapı, hızlı prototipleme ve uzun vadeli sürdürülebilirlik için idealdir.

**Temel Avantajlar:**
*   **Hibrit Mimari:** C# performansı ve GDScript esnekliği bir arada.
*   **Modüler Tasarım:** Yeni mekanikler ve özellikler mevcut sistemi bozmadan eklenebilir.
*   **AAA Kalitesinde Kamera:** Dinamik FOV, sarsıntı ve pürüzsüz geçişler.
*   **Veri Odaklı Konfigürasyon:** `MovementConfig` presetleri ile hızlı iterasyon.
*   **Gelişmiş Hata Ayıklama:** Kapsamlı debug araçları ve görsel panel.
*   **Zekice Mimari:** "The Pulse", "Kinetic Chain" ve "Ghost Logic" ile optimize edilmiş, esnek ve maliyet etkin yapı.

## 2. Core Developer (C#) Perspektifi: Çekirdek ve Mimari

**Sorumluluk Alanı:** C# Core katmanının geliştirilmesi, mimari tasarım, performans optimizasyonu.

C# Core, tüm hareket fiziği hesaplamalarının yapıldığı, Godot bağımlılıklarından arındırılmış saf bir katmandır. `IMovementSystem` arayüzü, hareket sisteminin soyutlanmasını sağlar ve **Decorator Pattern** için temel oluşturur. `MovementSystem.cs` bu arayüzü uygulayan ana sınıftır.

### 2.1. `IMovementSystem` ve `MovementSystem.cs`
`IMovementSystem` arayüzü, `MovementSystem` sınıfının dış dünyaya sunduğu API'yi tanımlar. Bu sayede, `MovementSystem`'in yerine `SudoMovementDecorator` gibi farklı uygulamalar geçirilebilir. `MovementSystem.cs`, `MovementContext` üzerinden gelen verilere göre karakterin hızını hesaplar, modifiye edicileri uygular ve yeni hızı döndürür.

### 2.2. `IMovementModifier` ve Modifiye Edici Pipeline
`IMovementModifier` arayüzü, `GrappleModifier`, `SlideModifier` gibi tüm hareket mekaniklerinin uyguladığı bir sözleşmedir. Her fizik karesinde, `MovementSystem` aktif modifiye edicileri bir pipeline içinde işler. Bu, mekaniklerin birbirini etkilemesini ve zincirlenmesini sağlar.

### 2.3. `MovementConfig.cs` ve Validasyon
`MovementConfig.cs`, karakterin tüm temel hareket parametrelerini (hızlar, ivmeler, sürtünmeler) içeren bir Godot `Resource`'udur. **ÖNEMLİ:** `[Export]` değerlerine C# tarafında `Mathf.Clamp` ve `Mathf.Max` ile validasyon eklenmiştir. Bu, editörden veya koddan girilen geçersiz değerlerin (negatif hız vb.) sistemin kararlılığını bozmasını engeller.

### 2.4. `SudoMovementDecorator.cs` (Decorator Pattern)
`SudoMovementDecorator`, `IMovementSystem` arayüzünü uygulayan ve mevcut `MovementSystem`'i sarmalayan bir sınıftır. Bu decorator, `GodMode`, `InfiniteJumps`, `NoClip` ve `SpeedMultiplier` gibi "sudo" yetkilerini ana hareket mantığından bağımsız olarak yönetir. `MovementNode.cs` artık `IMovementSystem` üzerinden çalışır ve `SudoMovementDecorator`'ı varsayılan olarak kullanır. Bu sayede, admin yetkileri ana hareket mantığından ayrılmış, kod daha temiz ve yönetilebilir hale gelmiştir. **GÜNCELLEME:** `SudoMovementDecorator` artık yetki kontrolleri için "Ghost Logic" (`MovementGates`) sistemini kullanır.

### 2.5. `GrappleModifier.cs` Bug Fix
`GrappleModifier.cs` içerisindeki spring-damper fiziği güncellenmiştir. Damping kuvveti artık çekim kuvvetini tamamen sıfırlamak yerine, hızın ip yönündeki bileşenine (radial velocity) uygulanır. Bu, grappling sırasında karakterin hedefe doğru yönelimini korurken daha gerçekçi bir salınım mekaniği sağlar.

### 2.6. "The Pulse" (`MovementPulse.cs`)
Emre'nin önerisi doğrultusunda geliştirilen `MovementPulse.cs`, `MovementNode` içindeki `_coyoteTimer`, `_jumpBufferTimer` gibi tüm zamanlayıcıları ve geçici durumları tek bir merkezi, event-driven sistem altında toplar. Sadece aktif olan zamanlayıcıları güncelleyerek CPU kullanımını optimize eder ve kod karmaşıklığını azaltır. `MovementContext` artık bu sistemden gelen güncel durum bilgilerini (`IsCoyoteActive`, `IsJumpBuffered`) içerir.

### 2.7. "Kinetic Chain" (`MovementEvents.cs`)
Elif'in önerisi doğrultusunda geliştirilen `MovementEvents.cs`, mekanikler arası hafif bir event bus sistemi sunar. Her hareket mekaniği (Modifier), tamamlandığında veya belirli bir eşiğe ulaştığında bir "Kinetic Event" yayınlayabilir (`DashStarted`, `DashEnded` gibi). Bu eventler, diğer mekaniklerin tetiklenmesi veya parametrelerinin ayarlanması için kullanılır, böylece mekaniklerin organik olarak birbirini beslemesini sağlar.

### 2.8. "Ghost Logic" (`MovementGates.cs`)
Arda'nın önerisi doğrultusunda geliştirilen `MovementGates.cs`, Logic-Gate tabanlı bir yetki ve kural sistemidir. `SudoMovementDecorator` içindeki `if (GodMode)` gibi doğrudan kontroller yerine, `CanFly`, `CanInfiniteJump`, `IsInvulnerable` gibi yetkiler `Func<bool>` delegeleri aracılığıyla tanımlanır ve `MovementGates.Check()` metodu ile sorgulanır. Bu, yetki yönetimini daha esnek, genişletilebilir ve maliyet etkin hale getirir.

## 3. Gameplay Programmer (GDScript) Perspektifi: Kontrol ve Entegrasyon

**Sorumluluk Alanı:** Kullanıcı girdileri, kamera kontrolü, animasyon ve ses entegrasyonu, GDScript tarafındaki oyun mantığı.

GDScript katmanı, oyuncu girdilerini alır, kamera davranışını yönetir ve C# Core ile iletişim kurar. `MovementNode.cs` (Bridge) bu iki katman arasındaki ana bağlantıdır.

### 3.1. `MovementNode.cs` (Bridge)
`MovementNode`, Godot `Node`'u olarak C# Core sistemini Godot ortamına entegre eder. GDScript'ten gelen `ApplyGrapple()`, `SetSprinting()` gibi çağrıları C# Core'daki `IMovementSystem`'e iletir. `_PhysicsProcess` içinde `MovementContext` oluşturur ve `_system.Update()` çağrısını yapar. `SudoMovementDecorator`'ı varsayılan olarak kullanır ve `SetGodMode()`, `SetInfiniteJumps()`, `SetNoClip()`, `SetSpeedMultiplier()` gibi sudo yetkilerini GDScript'e açar. **GÜNCELLEME:** `MovementNode` artık zamanlayıcılar için `MovementPulse` sistemini kullanır ve `MovementEvents` aracılığıyla olayları dinleyebilir veya yayınlayabilir.

### 3.2. `player_controller.gd`
Bu script, oyuncu girdilerini okur ve `MovementNode`'a iletir. `MovementNode`'dan gelen `StateChanged` sinyallerini dinleyerek animasyonları veya sesleri tetikleyebilir. `_check_landing` fonksiyonu `MovementNode`'un `StateChanged` sinyali ile optimize edilmiştir.

### 3.3. `camera_controller.gd`
Kamera sisteminin ana kontrolcüsüdür. `CameraModeBase`'den türetilmiş modları (Orbit, FirstPerson) yönetir. Dinamik FOV, kamera sarsıntısı ve hedef kilitleme (lock-on) mekaniklerini içerir.

### 3.4. `orbit_camera_mode.gd` ve `first_person_camera_mode.gd`
Bunlar `CameraModeBase`'den türetilmiş kamera modlarıdır. `orbit_camera_mode.gd` içinde `FastNoiseLite` tabanlı, farklı olaylar için profillerle (Landing, Explosion, Jump, Impact, Custom) gelişmiş kamera sarsıntısı ve hıza duyarlı dinamik FOV sistemi bulunur. `first_person_camera_mode.gd` ise birinci şahıs kamera davranışını tanımlar.

### 3.5. Kamera Mikseri (Blending System)
`camera_controller.gd` içerisinde kamera modları arasında geçiş yaparken (örneğin TPS'ten FPS'e veya araçtan inerken) `blend_duration` parametresi ile belirlenen süre boyunca iki modun kamera verileri (FOV, pozisyon, rotasyon) pürüzsüz bir şekilde `lerp` (doğrusal enterpolasyon) edilerek karıştırılır. Bu, ani kamera sıçramalarını önleyerek "AAA" kalitesinde akıcı geçişler sağlar.

## 4. Technical Designer / Level Designer Perspektifi: Konfigürasyon ve Ayarlama

**Sorumluluk Alanı:** Oyun mekaniklerinin ve seviye tasarımının teknik ayarları, dengeleme.

Sistem, tasarımcıların kod yazmadan hareket ve kamera davranışını kolayca ayarlayabilmesi için veri odaklı bir yaklaşım benimser.

### 4.1. `MovementConfig` Preset Kütüphanesi
`addons/virabis_movement/presets/` dizini altında `ninja_preset.tres`, `tank_preset.tres` ve `god_preset.tres` gibi örnek `MovementConfig` presetleri oluşturulmuştur. Bu `.tres` dosyaları, `MovementNode` üzerindeki `Config` slotuna atanarak karakterin tüm hareket parametrelerini tek bir dosya değişikliği ile anında değiştirmeye olanak tanır. Bu, farklı karakter tipleri veya oyun durumları için hızlıca hareket parametrelerini ayarlama ve deneme esnekliği sunar.

### 4.2. `SpringArm3D` Optimizasyonu
`SpringArm3D`'nin `collision_margin` değeri 0.5'ten 0.1'e düşürülmüş ve `shape` özelliği `SphereShape3D` olarak ayarlanmıştır. Bu ayarlar, dar alanlarda kameranın titremesini ve takılmasını önleyerek daha akıcı bir deneyim sunar. Tasarımcılar, bu değerleri Godot editöründen kendi seviye tasarımlarına göre optimize edebilir.

### 4.3. Kamera Sarsıntı Profilleri
`orbit_camera_mode.gd` içindeki `LANDING`, `EXPLOSION`, `JUMP`, `IMPACT` gibi önceden tanımlanmış sarsıntı profilleri, farklı oyun içi olaylar için kamera geri bildirimini kolayca ayarlamanızı sağlar. Tasarımcılar, bu profillerin frekans, genlik ve azalma hızı gibi parametrelerini oyunun hissiyatına göre değiştirebilir.

### 4.4. "Ghost Logic" ile Yetki Yönetimi
`MovementGates.SetGate()` metodu kullanılarak, belirli oyun durumlarına veya karakter özelliklerine göre yetkiler dinamik olarak açılıp kapatılabilir. Örneğin, bir güçlendirme toplandığında `CanFly` yetkisini aktif etmek veya bir alana girildiğinde `IsInvulnerable` yetkisini vermek mümkündür. Bu, tasarımcılara oyun kurallarını kod yazmadan esnek bir şekilde yönetme imkanı sunar.

## 5. QA Engineer Perspektifi: Test ve Validasyon

**Sorumluluk Alanı:** Sistem kararlılığı, hata tespiti, test senaryoları ve kullanıcı deneyimi doğrulaması.

Sistem, sağlam bir test altyapısı ve hata önleme mekanizmaları ile geliştirilmiştir.

### 5.1. `MovementConfig.cs` Validasyonu
`MovementConfig.cs` içerisindeki `[Export]` değerlerine C# tarafında `Mathf.Clamp` ve `Mathf.Max` ile validasyon eklenmiştir. Bu, Godot editöründen veya koddan girilen geçersiz (negatif hız, sıfır ivme vb.) değerlerin sistemin kararlılığını bozmasını engeller. QA ekibi, bu validasyonların doğru çalıştığını ve beklenmedik değerlerin sisteme sızmadığını doğrulamalıdır.

### 5.2. Debug Paneli ve Sudo Yetkileri
`debug_panel.gd` aracılığıyla ekranda canlı olarak izlenebilen hareket durumu, aktif modifiye ediciler ve zıplama bilgileri, test süreçlerini büyük ölçüde kolaylaştırır. `SudoMovementDecorator` sayesinde `GodMode`, `InfiniteJumps`, `NoClip` gibi yetkiler, test senaryolarını hızlandırmak ve belirli durumları kolayca yeniden üretmek için kullanılabilir. **GÜNCELLEME:** Bu yetkiler artık `MovementGates` üzerinden yönetildiği için, QA ekibi `MovementGates.SetGate()` metodunu kullanarak test senaryolarını daha dinamik hale getirebilir.

### 5.3. Kamera Akıcılığı Testleri
Kamera mikserinin (blending system) ve `SpringArm3D` optimizasyonunun, farklı kamera modları ve dar alanlarda akıcı geçişler sağladığı doğrulanmalıdır. Özellikle hızlı mod geçişleri ve çarpışma durumlarında kamera titremeleri veya ani sıçramalar test edilmelidir.

### 5.4. "The Pulse" ve "Kinetic Chain" Testleri
`MovementPulse` sisteminin zamanlayıcıları doğru yönettiği ve `MovementEvents` aracılığıyla yayınlanan olayların beklenen mekanikleri tetiklediği doğrulanmalıdır. Özellikle zincirleme mekaniklerin (örneğin, dash sonrası ivme transferi) doğru çalışıp çalışmadığı test edilmelidir.

## 6. UX Designer Perspektifi: Kullanıcı Deneyimi ve Geri Bildirim

**Sorumluluk Alanı:** Oyuncu hissiyatı, geri bildirim mekanizmaları, arayüz ve etkileşim tasarımı.

Sistem, oyuncuya zengin ve tatmin edici bir deneyim sunmak üzere tasarlanmıştır.

### 6.1. Dinamik FOV ve Kamera Sarsıntısı
*   **Dinamik FOV:** Karakterin hızına ve durumuna göre otomatik olarak ayarlanan FOV, oyuncuya hız ve hareket hissini daha iyi aktarır. Nişan alma sırasında daralan, sprint sırasında genişleyen FOV, oyuncunun odaklanmasına ve çevreyi algılamasına yardımcı olur.
*   **Kamera Sarsıntısı:** `FastNoiseLite` tabanlı sarsıntı sistemi, inişler, patlamalar veya darbeler gibi olaylara fiziksel bir geri bildirim ekler. Bu, oyuncunun oyun dünyasıyla daha derin bir bağlantı kurmasını sağlar ve olayların etkisini artırır.

### 6.2. Pürüzsüz Kamera Geçişleri
Kamera mikseri (blending system) sayesinde modlar arası geçişler (FPS/TPS, araçtan inme vb.) ani sıçramalar yerine yumuşak ve doğal animasyonlarla gerçekleşir. Bu, oyuncunun göz yorgunluğunu azaltır ve kesintisiz bir deneyim sunar.

### 6.3. Grapple Fiziği
Geliştirilmiş grapple fiziği, oyuncuya daha kontrol edilebilir ve tatmin edici bir salınım mekaniği sunar. Damping kuvvetinin hedefe yönelimi koruması, oyuncunun grapple'ı daha stratejik kullanmasına olanak tanır.

### 6.4. "Kinetic Chain" ile Akış Durumu Geri Bildirimi
`Kinetic Chain` eventleri, oyuncunun başarılı kombolar yaptıkça (örneğin, dash'ten sonra zıplama) görsel ve işitsel geri bildirimleri tetikleyebilir. Bu, oyuncunun "akış durumunu" (flow state) destekler ve oyun deneyimini daha ödüllendirici hale getirir.

## 7. Performance Engineer / Optimizer Perspektifi: Kaynak Kullanımı ve Optimizasyon

**Sorumluluk Alanı:** Oyunun performansını artırmak, bellek ve CPU kullanımını optimize etmek.

Sistem, performans göz önünde bulundurularak tasarlanmıştır.

### 7.1. C# Core Performansı
C# Core katmanı, Godot'un GDScript'ine kıyasla daha yüksek hesaplama performansı sunar. Özellikle fizik hesaplamaları ve modifiye edici pipeline gibi yoğun işlemler C# tarafında gerçekleştirilerek CPU yükü optimize edilmiştir.

### 7.2. `FastNoiseLite` ve Sarsıntı Optimizasyonu
`FastNoiseLite` tabanlı kamera sarsıntısı, rastgele sayı üretimine kıyasla daha deterministik ve optimize edilmiş bir gürültü üretimi sağlar. Her profil için ayrı `FastNoiseLite` örnekleri kullanılması, hesaplama maliyetini dağıtır. Ancak, çok sayıda sarsıntı profilinin aynı anda aktif olması durumunda performans etkileri izlenmelidir.

### 7.3. Modifiye Edici Yönetimi
Modifiye edici pipeline, `List<IMovementModifier>` üzerinde çalışır ve `RemoveAll` gibi işlemlerle süresi dolan modifiye edicileri temizler. Bu işlemlerin her fizik karesinde yapılması, listenin boyutunu kontrol altında tutar ve gereksiz hesaplamaları önler.

### 7.4. "The Pulse" ile Minimalist Zamanlayıcı Yönetimi
`MovementPulse` sistemi, `_coyoteTimer` ve `_jumpBufferTimer` gibi zamanlayıcıları sadece aktif olduklarında güncelleyerek CPU döngülerinden tasarruf sağlar. Bu "lazy evaluation" yaklaşımı, özellikle çok sayıda karakterin veya karmaşık zamanlayıcıların olduğu senaryolarda performans avantajı sunar.

### 7.5. "Ghost Logic" ile Dinamik Yetki Kontrolü
`MovementGates` sistemi, yetki kontrollerini `Func<bool>` delegeleri aracılığıyla yönettiği için, sadece ihtiyaç duyulduğunda ilgili koşulları değerlendirir. Bu, `if (is_admin)` gibi sabit kontrollerin her karede çalışmasının önüne geçerek mikro optimizasyonlar sağlar.

## 8. Sound Designer Perspektifi: Ses Entegrasyonu

**Sorumluluk Alanı:** Oyun içi ses efektleri, müzik ve genel ses deneyimi.

Sistem, ses tasarımcılarının hareket olaylarına kolayca tepki verebilmesi için sinyaller ve eventler sağlar.

### 8.1. `StateChanged` Sinyali
`MovementNode`'dan yayılan `StateChanged` sinyali, karakterin hareket durumundaki değişiklikleri (Idle, Moving, Sprinting, Airborne, Flying) bildirir. Ses tasarımcıları, bu sinyali dinleyerek farklı hareket durumları için uygun ayak sesi, rüzgar sesi veya diğer çevresel ses efektlerini tetikleyebilir.

### 8.2. "Kinetic Chain" Eventleri ile Adaptif Sesler
`MovementEvents` aracılığıyla yayınlanan `DashStarted`, `GrappleReleased` gibi eventler, ses tasarımcılarına mekaniklerin başlangıç ve bitiş anlarına özel ses efektleri ekleme imkanı sunar. Bu, oyunun dinamiklerine göre ses ortamını zenginleştirir ve oyuncuya daha fazla geri bildirim sağlar.

### 8.3. "The Pulse" ile Zamanlama
`MovementPulse` sistemi, zamanlayıcıları hassas bir şekilde yönettiği için, ses efektlerinin (örneğin, coyote time veya jump buffer ile ilişkili sesler) doğru zamanda tetiklenmesini sağlar. Bu, ses ve oynanış arasındaki senkronizasyonu artırır.

## 9. Animator Perspektifi: Animasyon Entegrasyonu

**Sorumluluk Alanı:** Karakter animasyonları, geçişler ve görsel akıcılık.

Sistem, animatörlerin karakter hareketlerini görsel olarak zenginleştirmesi için gerekli bilgileri sağlar.

### 9.1. `StateChanged` Sinyali
`MovementNode`'dan yayılan `StateChanged` sinyali, karakterin hareket durumundaki değişiklikleri animasyon durum makinelerine (Animation State Machines) doğrudan beslemek için kullanılabilir. Bu, `Idle`'dan `Walk`'a, `Walk`'tan `Sprint`'e veya `Airborne`'a geçiş gibi temel animasyon geçişlerini kolaylaştırır.

### 9.2. Dinamik Hız Bilgisi
`MovementSystem`'den alınan hız bilgisi, animasyon hızını dinamik olarak ayarlamak için kullanılabilir. Örneğin, karakterin hızı arttıkça yürüme/koşma animasyonunun oynatma hızını artırmak, daha gerçekçi bir hissiyat sağlar.

### 9.3. Kamera Sarsıntısı ve FOV Animasyonları
Kamera sarsıntısı ve dinamik FOV, animasyonlarla birlikte çalışarak görsel etkiyi artırır. Örneğin, bir patlama animasyonu sırasında kamera sarsıntısı ve hafif bir FOV değişimi, olayın şiddetini vurgular.

### 9.4. "Kinetic Chain" Eventleri ile Animasyon Tetikleme
`MovementEvents` aracılığıyla yayınlanan `DashStarted`, `GrappleReleased` gibi eventler, animatörlere özel animasyonları (örneğin, dash başlangıcı, grapple atışı) tetikleme imkanı sunar. Bu, mekaniklerin görsel olarak daha etkileyici olmasını sağlar ve oyuncuya daha fazla geri bildirim verir.

## 10. Technical Writer Perspektifi: Dokümantasyon ve Bilgi Paylaşımı

**Sorumluluk Alanı:** Teknik dokümantasyonun oluşturulması, güncellenmesi ve sürdürülmesi.

Bu dokümantasyon, projenin tüm paydaşları için tek ve güvenilir bir bilgi kaynağı olmayı hedefler.

### 10.1. Kapsamlı ve Modüler Yapı
Dokümantasyon, her uzmanlık alanına özel bölümlerle modüler bir yapıya sahiptir. Bu, ilgili kişilerin sadece kendi alanlarıyla ilgili bilgilere hızlıca ulaşmasını sağlar.

### 10.2. "Tak-Çalıştır" Yaklaşımı
Her özellik ve bileşen, nasıl entegre edileceği, nasıl kullanılacağı ve nasıl yapılandırılacağı konusunda net talimatlarla açıklanmıştır. Bu, yeni ekip üyelerinin veya farklı projelerin sistemi kolayca benimsemesine olanak tanır.

### 10.3. Sürekli Güncelleme
Dokümantasyon, projenin evrimiyle birlikte sürekli güncellenmelidir. Yeni özellikler, bug fix'ler veya mimari değişiklikler anında dokümantasyona yansıtılmalıdır.

### 10.4. "Living Documentation" Prensibi
Kod yorumları ve dokümantasyonun otomatik olarak senkronize edildiği bir sistem hedeflenmektedir. C# kodundaki `/// <summary>` etiketleri veya GDScript'teki `#region` açıklamaları gibi yapıların dokümantasyon çıktısına otomatik olarak dahil edilmesi, dokümantasyonun her zaman güncel kalmasını sağlar ve manuel güncelleme yükünü ortadan kaldırır.

---

## Kurulum ve Entegrasyon (Tüm Uzmanlar İçin)

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
