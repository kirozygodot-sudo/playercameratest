# Virabis Movement & Camera System: Zekice Mimari (Smart Architecture)

![Godot 4.6](https://img.shields.io/badge/Godot-4.6-478cbf?style=for-the-badge&logo=godotengine&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET-512bd4?style=for-the-badge&logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)

Bu proje, **Godot 4.6 (.NET/C#)** tabanlı, hibrit (C# Core + GDScript Bridge) bir karakter hareket ve kamera sistemidir. 10 farklı hibrit uzmanın (Muhasebeciden Sanatçıya, Mühendisten Minimaliste) ortak vizyonuyla, **"minimal eklenti, maksimum etki"** prensibine göre inşa edilmiştir.

## 🚀 Öne Çıkan Özellikler (Tak-Çalıştır)

### 1. Mimari Desenler (Zekice Mimari)
*   **The Pulse:** Tüm zamanlayıcılar (Coyote Time, Jump Buffer) merkezi ve event-driven olarak yönetilir. Sadece aktif olanlar CPU tüketir.
*   **Kinetic Chain:** Mekanikler arası hafif event bus sistemi. Bir mekaniğin bitişi (Dash), diğerinin başlangıcını (Jump Boost) organik olarak besler.
*   **Ghost Logic:** Logic-Gate tabanlı yetki sistemi. Gereksiz `if` yığınları yerine, yetkiler dinamik kapılar üzerinden sorgulanır.
*   **Sudo Decorator:** Ana hareket kodunu bozmadan admin yeteneklerini (GodMode, NoClip) sarmalayan modüler yapı.

### 2. Kamera Sistemi (AAA Kalitesi)
*   **Camera Mixer:** FPS ve TPS modları arasında pürüzsüz `lerp` geçişleri. Ani sıçramalara son!
*   **Dynamic FOV & Shake:** `FastNoiseLite` tabanlı, olaylara duyarlı (Landing, Explosion) sarsıntı profilleri ve hıza duyarlı FOV.
*   **Target Lock-on:** Düşmanlara kilitlenme mekaniği entegre edilmiştir.

### 3. Data-Driven Presetler
*   **Preset Kütüphanesi:** Ninja, Tank ve God gibi hazır `.tres` dosyaları. Tek bir dosya sürükleyerek tüm oyun hissini (Hız, İvme, Yerçekimi) anında değiştirin.

## 🛠️ Kurulum ve Kullanım

1.  **Repoyu Klonlayın:** `gh repo clone orikirozytito-ops/playercameratest`
2.  **Eklentiyi Aktif Edin:** `Project Settings > Plugins` sekmesinden `Virabis Movement` eklentisini aktif edin.
3.  **C# Çözümünü Derleyin:** Godot Editörünün sağ üst köşesindeki **Build (Çekiç simgesi)** butonuna tıklayın.
4.  **Karakterinizi Hazırlayın:** Karakter sahnenize `MovementNode` ekleyin ve `CharacterBody3D` referansını bağlayın.
5.  **Preset Atayın:** `MovementNode` üzerindeki `Config` slotuna `addons/virabis_movement/presets/` altındaki bir `.tres` dosyasını sürükleyin.
6.  **Girdi Yönlendirmesi:** `player_controller.gd` üzerinden girdi yönlendirmelerini yapın (Örnek projede mevcuttur).

## 📄 Dokümantasyon (10 Uzman Bakış Açısıyla)

Detaylı teknik rehberlere ve uzman notlarına `addons/virabis_movement/docs/` klasöründen ulaşabilirsiniz:
*   [Teknik Dokümantasyon](./addons/virabis_movement/docs/TECHNICAL_DOCUMENTATION.md) - Uzmanların teknik detayları.
*   [Mekanik Kullanım Rehberi](./addons/virabis_movement/docs/MECHANICS_USAGE_GUIDE.md) - 10 uzman mekanik.
*   [Preset Kütüphanesi Rehberi](./addons/virabis_movement/docs/PRESET_LIBRARY_GUIDE.md) - Veri odaklı tasarım.
*   [Uzman Toplantı Notları](./addons/virabis_movement/docs/MEETING_NOTES.md) - Stratejik kararlar.

## 📂 Klasör Yapısı

```
playercameratest/
├── addons/
│   └── virabis_movement/
│       ├── bridge/         # Godot API Köprüsü (MovementNode.cs)
│       ├── core/           # Saf C# Çekirdek (The Pulse, Ghost Logic)
│       ├── docs/           # Uzman Dokümantasyonu
│       ├── presets/        # Hazır Oyun Hissi Paketleri (.tres)
│       ├── scenes/         # Örnek Sahneler
│       └── scripts/        # GDScript Kontrolcüleri (Camera Mixer)
└── README.md
```

---
*Bu proje, **Manus AI** tarafından "Zekice Mimari" prensipleriyle optimize edilmiştir.*
