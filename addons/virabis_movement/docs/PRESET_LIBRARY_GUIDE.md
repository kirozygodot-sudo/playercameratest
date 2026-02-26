# Virabis Movement: Data-Driven Preset Kütüphanesi Rehberi

Virabis Movement System, oyun hissiyatını (game feel) kod satırlarına gömmek yerine, Godot'un **Resource** sistemini kullanarak tamamen veri odaklı (data-driven) bir yapı sunar. Bu rehber, preset kütüphanesinin mimarisini ve nasıl kullanılacağını açıklar.

## 1. Mimari Mantık: Resource Tabanlı Yapı

Sistemin kalbinde `MovementConfig.cs` sınıfı yer alır. Bu sınıf bir `[GlobalClass]` ve `Resource` olarak tanımlanmıştır. Bu sayede, editör içerisinde sağ tıklayıp "New Resource" diyerek yeni bir konfigürasyon dosyası (`.tres`) oluşturabilirsiniz.

### Neden Data-Driven?
*   **Hızlı İterasyon:** Kod derlemeye gerek kalmadan hız, ivme ve sürtünme değerlerini değiştirip anında test edebilirsiniz.
*   **Karakter Çeşitliliği:** Aynı `MovementNode` kodunu kullanan farklı karakterlere (Ninja, Tank, Boss) sadece farklı bir `.tres` dosyası atayarak tamamen farklı karakter hisleri verebilirsiniz.
*   **Modülerlik:** Tasarımcılar, çekirdek koda dokunmadan oyun dengesini (balancing) ayarlayabilirler.

---

## 2. Mevcut Preset Örnekleri

Kütüphanede şu an 3 temel preset bulunmaktadır:

| Preset | Karakter Hissi | Temel Özellikler | Kullanım Alanı |
| :--- | :--- | :--- | :--- |
| **Ninja** | Çevik, Hızlı, Keskin | Yüksek ivme, düşük sürtünme, Double Jump (MaxJumps=2), yüksek hız sınırı. | Hızlı aksiyon karakterleri. |
| **Tank** | Ağır, Hantal, Güçlü | Düşük hız, yüksek sürtünme, zıplama yok (MaxJumps=0), dar dönüş açısı. | Ağır zırhlı düşmanlar veya karakterler. |
| **God** | Sınırsız, Deneysel | Çok yüksek hız, sınırsız zıplama (MaxJumps=999), sıfır hava sürtünmesi. | Debug, test ve özel yetenek durumları. |

---

## 3. Uygulama ve Kullanım Örnekleri

### 3.1. Editör Üzerinden Kullanım (Tak-Çalıştır)
1.  Sahnede karakterinizin altındaki `MovementNode` düğümünü seçin.
2.  Inspector panelindeki **Config** slotuna gidin.
3.  `addons/virabis_movement/presets/` klasöründeki istediğiniz `.tres` dosyasını sürükleyip bu slota bırakın.
4.  Oyunu başlattığınızda karakter o presetin fizik kurallarıyla hareket edecektir.

### 3.2. Çalışma Anında (Runtime) Preset Değiştirme
Oyun içinde karakterin formu değiştiğinde (örneğin bir güçlendirme aldığında) preseti kodla değiştirebilirsiniz:

```gdscript
# GDScript: Karakteri anında 'Ninja' moduna geçir
func power_up_ninja():
    var ninja_config = load("res://addons/virabis_movement/presets/ninja_preset.tres")
    movement_node.SetConfig(ninja_config)
    print("Ninja modu aktif!")
```

### 3.3. Yeni Bir Preset Oluşturma
1.  FileSystem panelinde sağ tıklayın: **Create -> Resource**.
2.  Arama çubuğuna **MovementConfig** yazın ve seçin.
3.  Dosyayı `my_custom_preset.tres` olarak kaydedin.
4.  Dosyaya çift tıklayarak Inspector panelinde `WalkSpeed`, `SprintSpeed`, `MaxJumps` gibi değerleri kendi isteğinize göre ayarlayın.

---

## 4. Güvenlik ve Validasyon

Tasarımcıların hatalı değerler girmesini önlemek için `MovementConfig.cs` içerisinde otomatik koruma mekanizmaları bulunur:

```csharp
// C# Core Validasyon Örneği
[Export] public float WalkSpeed { 
    get => _walkSpeed; 
    init => _walkSpeed = Mathf.Max(0.1f, value); // Negatif hız girilse bile min 0.1 olur
}
```

Bu yapı sayesinde, editörden "0" veya "-50" gibi geçersiz bir değer girilse dahi sistem çökmez, en yakın mantıklı değere kendini sabitler.

---

## 5. Özet: Tasarımcı İçin İş Akışı
1.  **Tanımla:** Karakterin tipini belirle.
2.  **Seç:** Hazır bir preseti kopyala veya yenisini oluştur.
3.  **Ayarla:** Inspector panelinden değerleri "ince ayar" (fine-tune) yap.
4.  **Test Et:** Oyunu başlat ve hissi kontrol et.

> **İpucu:** `MovementConfig` dosyaları metin tabanlı (`.tres`) olduğu için Git üzerinde yapılan değişiklikleri kolayca izleyebilir ve farklı versiyonları karşılaştırabilirsiniz.
