# Virabis Movement System for Godot 4.6 (.NET)

![Godot 4.6](https://img.shields.io/badge/Godot-4.6-478cbf?style=for-the-badge&logo=godotengine&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET-512bd4?style=for-the-badge&logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)

## 🚀 Genel Bakış

Virabis Movement System, Godot 4.6 (.NET) için geliştirilmiş, yüksek performanslı ve modüler bir karakter hareket eklentisidir. C# tabanlı optimize edilmiş bir çekirdek (Core) ile GDScript arayüzünü (Bridge & Controller) birleştirerek, 10 farklı uzman hareket mekaniğini sorunsuz bir şekilde projenize entegre etmenizi sağlar. Bu sistem, sadece "çalışan" değil, aynı zamanda "sürdürülebilir, optimize edilmiş ve geleceğe dönük" bir hareket deneyimi sunar.

## ✨ Özellikler

*   **Hibrit Mimari:** C# Core ile GDScript Controller arasında güçlü ve esnek bir köprü.
*   **10 Uzman Mekanik:** Grapple Hook, Slide, Wall Jump, Crouch Jump, Vaulting, Time Dilation, Recoil Propulsion, Explosion Boost, ADS Glide ve Air Momentum Transfer.
*   **Resource Tabanlı Konfigürasyon:** Tüm hareket parametreleri Godot editöründen kolayca ayarlanabilir `MovementConfig` Resource'ları ile yönetilir.
*   **Performans Odaklı:** Event-driven sistem ve optimize edilmiş `_physics_process` döngüsü.
*   **Kapsamlı Debug Araçları:** Görsel debug paneli ve detaylı loglama.
*   **Modüler ve Genişletilebilir:** Yeni mekanikler `IMovementModifier` arayüzü ile kolayca eklenebilir.

## 📦 Kurulum

### Tek Satırda Kurulum (Önerilen)

Terminalinizi açın ve Godot projenizin ana dizininde aşağıdaki komutu çalıştırın:

```bash
git clone https://github.com/orikirozytito-ops/playercameratest.git addons/virabis_movement
```

Bu komut, eklentiyi doğrudan Godot projenizin `addons/` klasörüne klonlayacaktır.

### Manuel Kurulum

1.  Bu depoyu bilgisayarınıza indirin veya klonlayın.
2.  `playercameratest/addons/virabis_movement` klasörünü Godot projenizin `res://addons/` dizinine kopyalayın.
3.  Godot Editörünü açın.
4.  Üst menüden **Proje > Proje Ayarları > Eklentiler** sekmesine gidin.
5.  "Virabis Movement System" eklentisini bulun ve durumunu **"Etkin"** olarak ayarlayın.

### C# Çözümünü Derleme

Eklenti C# tabanlı olduğu için, Godot Editörünün sağ üst köşesindeki **Build (Çekiç simgesi)** butonuna tıklayarak projenizi bir kez derlemeniz gerekmektedir.

## 🎮 Kullanım

1.  Karakterinizin (örneğin `CharacterBody3D`) altına bir `MovementNode` düğümü ekleyin.
2.  `MovementNode` düğümünü seçin ve Inspector panelinde `Character` slotuna kendi `CharacterBody3D` düğümünüzü sürükleyip bırakın.
3.  Aynı `MovementNode` üzerindeki `Config` slotuna yeni bir `MovementConfig` Resource oluşturup atayın. Bu Resource üzerinden karakterinizin yürüme hızı, zıplama gücü gibi temel hareket parametrelerini ayarlayabilirsiniz.
4.  `PlayerController.gd` scriptinizi karakterinize ekleyin ve `movement` ile `camera` referanslarını doğru düğümlere bağlayın.
5.  Girdi (Input Map) ayarlarınızı `Project Settings > Input Map` üzerinden yapın (örneğin `jump`, `sprint`, `grapple` gibi aksiyonlar).

Detaylı kullanım örnekleri ve her bir mekaniğin kod snippet'leri için [MECHANICS_USAGE_GUIDE.md](docs/MECHANICS_USAGE_GUIDE.md) belgesine göz atın.

## 📂 Klasör Yapısı

```
playercameratest/
├── addons/
│   └── virabis_movement/         # Eklentinin ana klasörü
│       ├── bridge/               # Godot API ile C# Core arasındaki köprü (MovementNode.cs)
│       ├── core/                 # Saf C# hareket çekirdeği (MovementSystem.cs, Modifiers/)
│       ├── docs/                 # Detaylı dokümantasyonlar
│       │   ├── ANALIZ_VE_YOL_HARITASI.md
│       │   ├── MECHANICS_USAGE_GUIDE.md
│       │   ├── TECHNICAL_DOCUMENTATION.md
│       │   └── USAGE_AND_DEBUG_NOTES.md
│       ├── plugin.cfg            # Godot eklenti konfigürasyonu
│       ├── scenes/               # Örnek sahneler veya prefablar (main.tscn)
│       └── scripts/              # GDScript kontrolcüleri (player_controller.gd, debug_panel.gd)
├── MovementSystemTests.cs        # C# birim testleri
└── README.md                     # Bu dosya
```

## 🛠️ Teknik Detaylar

Bu sistem, **Arda (Godot Core Uzmanı & Muhasebeci)** ve **Elif (Gameplay Programcısı & Bahçıvan)** gibi uzmanlarımızın önerileriyle, `MovementConfig` Resource'ları ve `IMovementModifier` arayüzü etrafında inşa edilmiştir. Daha fazla teknik bilgi için [TECHNICAL_DOCUMENTATION.md](docs/TECHNICAL_DOCUMENTATION.md) belgesini inceleyebilirsiniz.

## 🐛 Debugging

Sistem, geliştirme sürecini kolaylaştırmak için kapsamlı bir debug altyapısı sunar. Görsel debug paneli entegrasyonu ve C#/.NET debugger kullanımı hakkında bilgi için [USAGE_AND_DEBUG_NOTES.md](docs/USAGE_AND_DEBUG_NOTES.md) belgesine başvurun.

## 🤝 Katkıda Bulunma

Her türlü katkı, hata raporu veya özellik isteği memnuniyetle karşılanır. Lütfen bir `Issue` açmaktan veya `Pull Request` göndermekten çekinmeyin.

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır. Daha fazla bilgi için `LICENSE` dosyasına bakın.

---

**Manus AI** tarafından **Virabis** projesi için özel olarak geliştirilmiştir.
