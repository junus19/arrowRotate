# Arrow Rotate (Hexa Arrows) — Proje Planı

> Durum: Taslak — onay bekliyor · Tarih: 2026-07-11
> Kaynaklar:
> - **Spesifikasyon:** `.claude/skills/hexa-arrows-unity/SKILL.md` (Unity port spec'i — davranış konusunda bağlayıcı)
> - **Prototip (source of truth):** `.claude/skills/hexa-arrows-unity/reference/hexa-arrows-prototype.html`
> - **Referans testler:** `reference/*_test.js` (palette, ice, struct, contig, genel)
> - **Önceki oyun:** `GitHub/arrowJam` (Gamebrain entegrasyon deseni için referans)

---

## 0. Oyun Özeti

**Hexa Arrows** — hexagon döndürme bulmacası. Sürükleme YOK; tek etkileşim dokunmak.

- Flat-top hexagon taşlardan oluşan tahta (axial q,r koordinat). Her taşın üzerinde bir okun bir segmenti var (tail / mid / head).
- Taşa dokun → taş 60° saat yönünde döner, segment de onunla döner.
- Bir okun tüm segmentleri kuyruktan uca kesintisiz bağlanınca ok kilitlenir ve otomatik fırlar: kendi yolu boyunca süzülüp head yönünde tahtadan çıkar; geçtiği taşlar sırayla yok olur.
- Çıkış ışınında başka okun segmenti varsa çarpıp geri döner, `waiting`'e geçer (taşları görünmez olur); engel kalkınca zincirleme otomatik fırlar.
- **Buz mekaniği:** 3 ok buzlu başlar (eşik 1/2/3); toplam çıkan ok sayısı eşiğe ulaşınca buz kırılır. Buzlu taş döndürülemez.
- Tüm oklar çıkınca level biter. HUD: süre (ilk hamlede başlar), hamle sayacı, ok başına renk çipi (✓ / ❄ + kalan).
- Level'lar deterministik seed'li prosedürel üretim: self-avoiding walk + palet çizge boyaması + engelleme DAG doğrulaması + buz simülasyonu + karıştırma.

**Kritik ayrım (spec §3):** `arrowId` (mantık kimliği) ≠ `palette` (görsel renk). Büyük levellarda birden fazla ok aynı paleti paylaşır; tüm mantık `arrowId` üzerinden yürür.

---

## 1. Teknik Kararlar

| Konu | Karar | Gerekçe |
|---|---|---|
| Unity | 6000.3.11f1 + URP 17.3 (kurulu) | Mevcut zemin |
| Mimari | Gamebrain template; framework'e dokunulmaz, subclass ile bağlanılır | arrowJam'de kanıtlanmış desen |
| Mantık katmanı | **MonoBehaviour'suz saf C#** (grid, üretim, traceConnected, rayBlockers, simülasyon) — spec §11 zorunlu kılıyor | EditMode testleri, görünümden bağımsızlık |
| Hücre erişimi | `Dictionary<(int q,int r), Cell>` (string key değil) | Spec §11 |
| Sunum (2D/3D) | Mantık bağımsız; ilk dikeyde 2D (SpriteShape/mesh hex + LineRenderer/mesh segment). 3D taş görünümü art direction kararıyla View katmanında değişebilir | Prototiple birebir his öncelik |
| Input | Fizik raycast YOK — dokunma noktası → axial ters dönüşüm + hex rounding | Spec §11 |
| Y ekseni | SVG (y aşağı) → Unity (y yukarı) aynalama kararı ilk gün birim testle sabitlenir: "dokun → EKRANDA saat yönünde 60°" korunur | Spec §2 uyarısı, bilinen tuzak |
| Level formatı | `HexaLevelData : LevelData` (SO) = **seed + config** (R, lengths, minEdges, buz); grid runtime'da deterministik üretilir | Spec seed'i level numarasına bağlamayı öneriyor; arrowJam'in "flat cell array bake" yaklaşımına gerek yok |
| RNG | mulberry32 birebir port (aynı seed → prototiple aynı level, debug/repro için) | Spec §7 |
| Tween | DOTween (kurulu) — dönüş overshoot, vanish, bounce | Az kod |
| Test | Unity Test Framework EditMode — spec §10'daki 6 test sınıfı, test başına ≥100 seed | Spec zorunlu kılıyor; `reference/*_test.js` birebir çevrilir |

---

## 2. Mimari

```
Assets/_Arrow Rotate/
├── Scripts/
│   ├── Core/          Saf C#: HexCoord (axial + DIRS + opp + rounding), Cell, Arrow,
│   │                  HexaLevel (Dictionary<(q,r),Cell> + arrows), Mulberry32
│   ├── Logic/         Saf C#: ConnectionTracer (traceConnected), RayBlockers,
│   │                  FlightPath (polyline kurucu), ExitSimulator (buz dahil çözülebilirlik)
│   ├── Generation/    Saf C#: LevelGenerator (walk + segment kurulum + palet boyama +
│   │                  DAG/Kahn + buz atama + scramble), LevelConfig (zorluk tablosu)
│   ├── Data/          HexaLevelData : LevelData (seed+config), PaletteData (10 renk + assert),
│   │                  ThemeData
│   ├── Board/         BoardView (hex taş spawn/yaşam döngüsü, koordinat↔dünya dönüşümü),
│   │                  TileView (taş + vanish/clear/shake), SegmentView (beyaz segment çizimi,
│   │                  merkezden kırık çizgi kuralı), IceOverlayView (buz + çatlak + rozet)
│   ├── Input/         TapController (ekran→axial dönüşüm, debounce'suz birikimli +60°,
│   │                  input lock entegrasyonu)
│   ├── Animation/     RotationAnimator (160ms overshoot), FlightAnimator (polyline pencere,
│   │                  0.55px/ms eşleniği), BounceAnimator (560ms sin), IceBreakFX (6 üçgen),
│   │                  TileVanishSequencer (kuyruk geçtikçe sırayla)
│   ├── GUI/           HexaGameplayPanel (süre, hamle, ok çipleri ✓/❄), TutorialHand (tap)
│   ├── Gameplay/      HexaGameplayManager (koordinatör: BeginLevel, ok durum makinesi
│   │                  idle→connected→flying|waiting→done, zincirleme fırlatma kuyruğu,
│   │                  buz eşiği takibi, hamle/süre sayaçları, win tespiti)
│   └── Integration/   HexaGameManager : GameManager,
│                      HexaGameState_Gameplay : GameState_Gameplay,
│                      Events/ (aşağıda)
├── Editor/            Level önizleme/tarama penceresi (seed gezgini, zorluk istatistikleri,
│                      toplu doğrulama), test yardımcıları
├── Data/Levels/       HexaLevelData asset'leri (seed+config)
├── Prefabs/           Tile, UI parçaları
├── Scene/             Game içeriği (Boot/GUI Gamebrain'de kalır)
└── Sprites / FX / Settings
```

### Katman kuralları

- **Core + Logic + Generation:** Unity API yok. Tüm oyun kuralları burada; `reference/` testleri buraya birebir çevrilir. Görsel katman yalnızca bu katmanın olaylarını oynatır.
- **Gameplay (koordinatör):** Ok durum makinesinin tek sahibi. Görsel animasyonların zamanlamalarını (240ms fırlatma gecikmesi, 180/260ms zincirleme kademe) yönetir; mantık sorgularını Logic'e delege eder.
- **Integration:** Gamebrain'e tek temas. `HexaGameManager.OnInitializationCompleted()` içinde `_inGameState` değiştirilir, booster/board-object enjeksiyonu atlanır (Example + arrowJam deseni).

### EventBus event'leri (`Integration/Events/`)

| Event | Ne zaman |
|---|---|
| `HexaLevelStartedEvent` | Level kuruldu (ok sayısı, buzlu oklar) |
| `HexaRotateEvent` | Geçerli döndürme (hamle sayacı, analytics; buzlu/kilitli dokunuş SAYILMAZ) |
| `HexaArrowConnectedEvent` | Ok bağlandı (kilitleme, FX) |
| `HexaArrowExitedEvent` / `HexaArrowBlockedEvent` | Uçuş tamam / çarpıp bekleme |
| `HexaIceBrokenEvent` | Buz kırıldı (FX + çip güncelleme) |
| `HexaLevelWonEvent` (+ gerekirse `HexaLevelFailedEvent`) | Tüm oklar çıktı → state geçişi |
| `HexaTutorialEvent` | Tutorial adımı |

Ses/haptik: yalnızca `FxRequestEvent` (Rotate / Connect / Launch / Bounce / IceBreak / Win anahtarları).

---

## 3. Fazlar

> **Yürütme kararları (2026-07-11, geliştirme başladı):**
> - v1'de fail koşulu YOK — prototip gibi süre/hamle yalnızca istatistik (Faz 7'de yeniden değerlendirilir).
> - Sunum başlangıçta 2D (SpriteRenderer/mesh + segment çizimi); mantık katmanı bağımsız olduğundan 3D'ye geçiş View değişikliğidir.
> - Unity MCP (CoplayDev, v10.0.0'a sabitlendi) üzerinden editörde doğrudan çalışılıyor: sahne kurulumu, derleme/konsol kontrolü, EditMode test koşumu, screenshot doğrulaması.

### Faz 0 — Hazırlık (yarım gün)
- [x] `hexa-arrows-unity` skill'i repoya kopyalandı (`.claude/skills/`).
- [x] Unity MCP güvenlik kontrolü + paket `#v10.0.0`'a sabitlendi.
- [ ] EditorBuildSettings düzelt: Boot → Game → GUI (şu an Example ilk sırada, Boot devre dışı); stale `SampleScene` girdisini sil.
- [ ] `CLAUDE.md`'ye oyun bölümü ekle: mekanik özeti, skill'e işaretçi, klasör sözleşmesi, event listesi, arrowId≠palette kuralı, iki geliştirici sahiplik ayrımı.

### Faz 1 — Saf mantık çekirdeği ✅ (2026-07-11)
- [x] `HexCoord`: DIRS tablosu, opp, DirBetween, InRegion. `HexMetrics`: center/edgeMid, ekran→axial + hex rounding.
- [x] Y-ekseni aynalama sözleşmesi sabitlendi: Unity açısı = -(30+60d)°, tap = z'de -60° (`HexMetrics` + `HexMathTests`).
- [x] `Cell` / `Arrow` / `HexaLevel` modeli, `Mulberry32` birebir port.
- [x] `ConnectionTracer` + `RayScanner` + `ExitSimulator` — spec §4 birebir.
- [x] EditMode testleri (düz mid çift-rot, kendi gövdesi/exited ok engel değil, buz deadlock senaryoları).
- Asmdef zinciri: `ArrowRotate.Core` ← `Logic` ← `Generation` (hepsi `noEngineReferences: true`, MonoBehaviour'suz) + `ArrowRotate.Tests.EditMode`.

### Faz 2 — Level üretimi + doğrulama ✅ (2026-07-11)
- [x] `LevelGenerator`: self-avoiding walk, segment kurulumu (rot=0 çözüm), palet çizge boyaması, engelleme grafiği + Kahn DAG, minEdges hedefi, buz ataması + tam simülasyon + garantili fallback, scramble + "hiçbir ok baştan bağlı değil", bitişiklik öz-denetimi. Regen zinciri prototiple aynı (+7919 / +31337).
- [x] `LevelConfig.ForLevel(n)` zorluk tablosu (spec §7 birebir).
- [x] Spec §10'un 6 test sınıfı + determinizm testi: **27/27 geçti** (4 config × 100 seed = 400 üretim, ~6 sn).
- **Milestone L: mantık tamam ✅** — görsel olmadan testte kanıtlı.

### Faz 3 — Board görselleştirme + input ✅ (2026-07-11)
- [x] `BoardView`/`TileView`: runtime hex mesh taşlar (palet renginde, MPB + linear color space düzeltmesi), kamera fit.
- [x] `SegmentView`: beyaz segment — tail (nokta+çizgi), mid (merkezden kırık çizgi), head (çizgi + üçgen uç); ölçüler spec §9 (0.153·S vb.). Rot görseli RotRoot z-dönüşü (-60°/tap), lokal a/b ile çizim.
- [x] `TapController`: raycast'siz ekran→axial (HexMetrics.WorldToAxial); birikimli dönüş (sürekli açı takibi, debounce yok).
- [x] Dönüş animasyonu: 160ms OutBack, animasyon sonrası yalnız o okun kontrolü (`CheckDelay` 170ms).

### Faz 4 — Uçuş ve durum makinesi ✅ (2026-07-11) — **Milestone A**
- [x] `FlightPathBuilder` (Logic, saf): kuyruk merkezi → kenar ortaları → hücre merkezleri → head ucu.
- [x] `FlightRenderer`: polyline üzerinde kayan pencere (dash tekniği eşleniği), hız 16.18·S/sn; uçuş başında hücreler veri katmanından silinir (uçan ok engel değil).
- [x] Taş yok olma: kuyruk geçtikçe √3·S/hız kademeli; 450ms scale+dönme+fade.
- [x] Bounce (560ms sin, engel sınırı formülü) + waiting görseli (taşlar şeffaf, segment kalır).
- [x] Zincirleme fırlatma (180ms/+260ms) + win tespiti (500ms; şimdilik C# event — Faz 6'da `HexaLevelWonEvent`).
- [x] `HexaSandbox.unity` test sahnesi + `HexaSandboxDriver` (MCP'den sürülebilir: TapCell/SolveArrow/SolveAll).
- Editörde play modunda uçtan uca doğrulandı: bağlan→fırlat, engel→bounce→waiting, zincirleme çıkış, WIN. Ekran görüntüleriyle prototip görsel şeması teyit edildi.
- **Milestone A: prototiple birebir oynanabilir çekirdek ✅** (buz görselleri hariç — Faz 5)

### Faz 5 — Buz mekaniği ✅ (2026-07-13)
- [x] `IceView`: yarı saydam mavi hex katman + kenar çizgisi + deterministik çatlaklar + orta hücrede kalan-eşik rozeti (TextMesh).
- [x] Buzlu taşa dokunuş: shake ~300ms, hamle sayılmaz, timer başlamaz (manager'da doğrulandı: MoveCount=0).
- [x] Kırılma FX: taş başına 6 üçgen parça (dışa savrulma + dönme + küçülme + fade, ~650ms), taştan taşa 50ms yayılım; rozet fade.
- [x] Eşik akışı canlı testte doğrulandı: 1. çıkış → eşik-1 kırıldı, 3. çıkış → tümü kırıldı; buzlu segmentler ışın engeli olarak çalışıyor (bekleyen oklar buz kalkınca zincirle çıktı).
- HUD çipinde ❄ + kalan sayı → Faz 7 (HUD ile birlikte).

### Faz 6 — Gamebrain entegrasyonu ✅ (2026-07-13) — **Milestone B**
- [x] `HexaGameManager : GameManager` (Boot'ta component swap yapıldı, tüm data referansları korundu), `HexaGameState_Gameplay : GameState_Gameplay`, `HexaLevelData : LevelData` (seed + zorluk; sahne alanı boş → Level.Load sahne yüklemez).
- [x] EventBus event'leri: `HexaLevelStartedEvent/RotateEvent/ArrowConnectedEvent/ArrowExitedEvent/ArrowBlockedEvent/IceBrokenEvent/LevelWonEvent/TutorialEvent`; ses `FxRequestEvent` üzerinden.
- [x] 10 level asset'i (`Data/Levels/HexaLevel_001..010`, test edilmiş seed'ler) + `Hexa Game Config.asset` → GameConfig._levels.
- [x] Game sahnesi: "Hexa Arrows" GO (BoardView+Manager+TapController); Example kalıntıları (trigger/kamera/ışık) temizlendi; kamera CameraManager.GameplayCamera + FitCamera.
- [x] Uçtan uca canlı test: Boot → Gameplay (HexaLevel_002) → win → LevelCompletePanel → Next → HexaLevel_003 (buzlu) → win → state geçişleri, level index ilerlemesi, `Fx: LevelComplete` hepsi çalıştı.
- **Milestone B: oyun döngüsü tamam ✅**
- Not: Main menü anında gameplay'e geçiyor (template davranışı) ve Win panel içeriği placeholder — ikisi de Faz 7 HUD/panel işine girer. Kalan: `HexaGameplayPanel` (süre/hamle/ok çipleri).

### Faz 7 — İçerik + meta ✅ (2026-07-13)
- [x] `HexaHudPanel` (GUI sahnesi, salt EventBus abonesi): ⏱ süre (ilk hamlede başlar, m:ss) · ok başına palet-renkli çip (buzlu: kalan eşik sayısı, çıkan: soluk) · hamle sayacı. `HexaLevelStartedEvent` çip verisi (`HexaArrowChipInfo[]`) taşıyacak şekilde genişletildi. Runtime üretilen sprite'lar (asset bağımlılığı yok).
- [x] Tutorial: level 1'de yalnız ilk ok döndürülebilir + head hücresinde pulse halkası; ok bağlanınca kısıt kalkar. Canlı testte doğrulandı (kısıtlı tap sayılmadı, bağlanınca serbest kaldı).
- [x] Fail koşulu: v1'de YOK (yürütme kararı — prototiple aynı; revive/süre limiti gerekirse Faz 8+).
- [x] Ekonomi: level sonu +25 coin (`HexaGameManager` → `CurrencyManager.AddCoin`, canlı testte coin=25 doğrulandı). Ses anahtarları: dönüş=Drag, çıkış=RocketLaunch, engel=InvalidDrop, buz=Ice_1, win=LevelComplete (klip atamaları Feedbacks_SO içerik işi).
- [x] **Seed Browser** editor penceresi (`Arrow Rotate ▸ Seed Browser`): zorluk+seed aralığı tarama, istatistik (ok/hücre/engel/buz) + geçerlilik, mini önizleme, seçileni bake, klasörü Config'e atama.
- [x] İlk içerik paketi: 50 level (zorluk eğrisi 1→2→3×4→5×44, test edilmiş seed uzayından) Config'e atandı. Küratörlük/ayıklama Seed Browser ile yapılabilir.
- Faz 8'e devredilenler: tema sistemi (art direction ile birlikte), HUD ikonları (❄/✓ glifleri TMP fontunda yok — art pass), tutorial el sprite'ı (şimdilik pulse halkası).

### Ek iş — Hexa Level Editor ✅ (2026-07-13)
- [x] **Level formatı değişti:** `HexaLevelData` artık tam hücre dizisi saklar (seed alanı kalktı; `HexaCellSave[]` + `HexaArrowSave[]` + Radius, To/FromHexaLevel dönüşümleri). Rot'lar asset'te — editördeki diziliş oyundakiyle birebir. 50 mevcut level aynı seed formülüyle migrate edildi, runtime doğrulandı.
- [x] `HexaLevelEditorWindow` (**Arrow Rotate ▸ Level Editor**, arrowJam editör paritesi): level listesi (GameConfig checkbox, rename/duplicate/sil) · hex canvas (prototip koordinatları, rot uygulanmış segment çizimi, buz katmanı+rozet, move hayaleti, draw önizleme) · araçlar: Draw (kuyruk→head), Erase (tüm ok), Move (tüm ok), Rotate, Recolor, Ice, Edit (denetçi + uçuş yönü döndürme) · "Paletleri Ata" çizge boyaması · Scramble (bağlı-kalmaz garantili) / Çöz · Random Fill (zorluk+seed → üretici) · canlı doğrulama (yapı, bitişiklik, çözülebilirlik, karışıklık, palet komşuluğu, buz DAG). Undo destekli; doğrulama ve Undo editörde test edildi.
- Seed Browser durur (toplu tarama/ayıklama); bake artık hücrelere yazar.

### Ek iş — 3D görünüm modu ✅ (2026-07-17)
- [x] `BoardView.ViewMode` (Flat2D/Depth3D) — 2D mod korunarak 3D eklendi; Game + HexaSandbox sahneleri Depth3D'ye alındı.
- [x] Taşlar `Models/hexagon.fbx` (kullanıcının modeli): otomatik ölçek/yönelim (`TileView.Create3D`), URP Lit Transparent paylaşılan malzeme (fade'ler 3D'de çalışır), MPB `_BaseColor`.
- [x] Runtime ışık + Flat ambient (pozlama dengesi ~1.0), kamera 24° üstten eğim, `TapController` düzlem-raycast (31/31 hücre doğrulandı).
- [x] Oklar karar gereği düz sprite katmanı kaldı (kabartma yok — referans görsellerle uyumlu).
- Referans stil: Hexa Sort / Combo Blast benzeri puck taşlar; ekran görüntüleriyle karşılaştırıldı. Gölge/arka plan cilası Faz 8'e.

### Ek iş — XZ 3D render yolu ✅ (2026-07-21)
- [x] Board gerçek XZ düzlemine taşındı (kullanıcı kararı: klasik 3D düzlemi; partikül vb. için doğal). Taş = kullanıcının EP hexagon'u (bake edilmiş mesh), kamera yukarıdan eğik, tap Y=0 düzlemine ışın, dönüş Y ekseni.
- [x] **Segmentler tek parça prosedürel mesh** (`SegmentMesh3D`): EP arm oranlarıyla süpürülmüş yuvarlak kesit, açılı birleşimlerde kavisli dirsek, head'de stub+tabanı oturan ok başı (CombineMeshes). Kullanıcının şikayet ettiği iki bug çözüldü: ok başı/segment bindirmesi ve açılı head bozukluğu.
- [x] **Uçuş/bounce 3D** (`FlightRenderer3D`): LineRenderer kaldırıldı; her frame pencere şeridi mesh üretimi + uçta aynı ok başı → tile segmentleriyle seamless. Canlı testte uçuş + zincirleme çıkış doğrulandı.
- Kalan (XZ): buz görselleri (IceView XY'de; XZ'de Build atlıyor — mantık çalışıyor, görsel işaret yok) + ok başı boyu/kamera ince ayarı kullanıcı geri bildirimiyle.

### Faz 8 — SDK, build, cila (2 gün + süreklilik)
- [ ] Native SDK'lar import edilmemiş: MAX (AppLovin), Adjust, GameAnalytics, Firebase + scripting define'ları (`MAX_ENABLED`, `ADJUST_ENABLED`…).
- [ ] Analytics şeması: level_start/win (seed loglanır — spec §11 repro önerisi), rotate sayısı, süre, buz kırılmaları, blocked sayısı.
- [ ] iOS/Android build, low-end 60fps kontrolü (taş/segment havuzu, tek geçişli çizim).
- [ ] Görsel cila: prototipin stil sabitleri (spec §9) temel; art direction 3D derse TileView/SegmentView değişir.

**Kaba tahmin:** Milestone A ~6–8 iş günü, Milestone B ~8–10 gün; içerik+meta+SDK ile ~3 hafta (tek kişi eşdeğeri; iki geliştiriciyle kısalır).

---

## 4. arrowJam'den Yeniden Kullanım (güncellenmiş — sınırlı)

Mekanik tamamen farklı (kare grid+drag → hex+rotate); kod portu değil **desen** portu yapılır:

| Ne | Strateji |
|---|---|
| `Integration/` deseni (GameManager + GameState_Gameplay + LevelData subclass, transition yeniden kurma) | Birebir aynı desen |
| Event isimlendirme/akış sözleşmesi (oyun-öneki, UI yalnızca subscribe) | Aynı desen |
| GameplayPanel HUD kalıpları (timer, çip/nokta göstergeleri) | Uyarlanır |
| TutorialHand + hamle kısıtlama | Tap'e uyarlanır |
| ThemeData/ColorDatabase SO deseni | Uyarlanır (PaletteData assert'li) |
| Editor window kalıbı (level listesi, doğrulama paneli) | İlham; içerik seed-tabanlı olduğundan daha hafif |
| GridManager, DragController, SnakeRibbon/ExitAnimator, LevelGenerator (Warnsdorff) | **Taşınmaz** — kare grid/drag'e özgü |

---

## 5. İki Geliştirici İçin Sahiplik Önerisi

- **Dev A — Mantık:** Core + Logic + Generation + EditMode testleri + Editor seed gezgini + level içeriği. (Sahneye dokunmaz; saf C#.)
- **Dev B — Sunum:** Board/Input/Animation/GUI/Gameplay koordinatörü + Integration + sahne/prefab sahipliği.
- Ortak sözleşme: `Cell`/`Arrow` modeli, Logic'in public API'si ve event imzaları — değişiklik önce CLAUDE.md'ye.
- Faz 1–2 (Dev A) ile Faz 3 iskeleti (Dev B, sahte level verisiyle) paralel yürüyebilir.

---

## 6. Riskler / Açık Sorular

1. **Fail koşulu:** Prototipte fail yok (süre/hamle yalnız istatistik). Mobil sürümde süre limiti + revive eklenecek mi? Faz 7'yi etkiler.
2. **2D/3D art direction:** Mantık bağımsız; karar Faz 3 sonuna kadar verilmeli (TileView/SegmentView'i etkiler).
3. **Level içerik stratejisi:** Salt seed+config mi (sonsuz üretim), yoksa elle seçilmiş/ayıklanmış seed listesi mi? Öneri: ilk 50 level elle seçilmiş seed, sonrası prosedürel.
4. **Runtime üretim maliyeti:** 400 attempt'li üretim mobilde level yüklemesini uzatabilir → yükleme ekranında async üretim veya seed'lerin önceden doğrulanması (Editor toplu doğrulama bunu çözer).
5. **Booster'lar:** Bu mekanikte anlamlı booster var mı (ör. "bir oku otomatik çöz", "buzu kır")? v1 varsayılanı: yok.
