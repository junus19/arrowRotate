# Gamebrain — Project Reference

Unity casual puzzle game using a state machine core, event bus communication, and ScriptableObject-driven data.

---

## Team

Bu proje 2 developer tarafından geliştirilmektedir. Her iki developer da ayrı Claude Code hesapları kullanmaktadır ancak aynı ekipte ve aynı codebase üzerinde çalışmaktadır.

- Tüm kararlar bu dosyada kayıt altına alınır; her iki Claude instance'ı da bu kuralları takip eder.
- Bir convention değiştirildiğinde önce bu dosya güncellenir, sonra koda yansıtılır.
- Çakışmaları önlemek için sistemler arasında sahiplik ayrımı yapılır — bir sistem bir developer tarafından geliştirilirken diğeri o sisteme dokunmaz.
- Kod stili, mimari kararlar ve isimlendirme her iki taraf için bağlayıcıdır; kişisel tercih geçerli değildir.
- **Vibe (2026-07-22):** İş güzel gittiğinde, görünüm/hissiyat oturduğunda ekip "Maşallah" der 😄 — küçük bir kutlama kültürü. Claude da uygun düştüğünde katılabilir.

---

## Oyun: Hexa Arrows (Arrow Rotate)

Hexagon döndürme bulmacası. **Sürükleme yok** — tek etkileşim taşa dokunmak (60° saat yönü dönüş). Renkli ok segmentlerini (tail/mid/head) kuyruktan uca bağla; bağlanan ok otomatik uçar, önü tıkalıysa çarpıp bekler. Buz mekaniği: 3 ok buzlu başlar, toplam çıkış sayısı eşiğe (1/2/3) ulaşınca kırılır. Tüm oklar çıkınca level biter. **v1'de fail koşulu yok** (süre/hamle yalnız istatistik).

**Durum (2026-07-13):** Faz 0–7 tamamlandı (bkz. `PLAN.md`). Oyun uçtan uca oynanabilir: Boot'tan Play → tutorial'lı level 1 → 50 level, buz, HUD, coin ödülü. Kalan: Faz 8 (tema/art pass, HUD ikonları, native SDK'lar, build).

**Bağlayıcı kaynaklar (bu sırayla):**
1. `.claude/skills/hexa-arrows-unity/SKILL.md` — port spesifikasyonu (veri modeli, algoritmalar, zorluk tablosu, görsel oranlar, zorunlu testler)
2. `.claude/skills/hexa-arrows-unity/reference/hexa-arrows-prototype.html` — davranışta nihai kaynak (source of truth)
3. `PLAN.md` — faz planı, yürütme kararları, faz kapanış notları

### Kod yerleşimi ve assembly yapısı

Her şey `Assets/_Arrow Rotate/Scripts/` altında. Gamebrain asmdef kullanmadığı için iki katman var:

| Klasör | Assembly | İçerik |
|---|---|---|
| `Core/` | `ArrowRotate.Core` (noEngineReferences) | HexCoord, HexMetrics, Mulberry32, Cell/Arrow/HexaLevel, LevelConfig |
| `Logic/` | `ArrowRotate.Logic` (noEngineReferences) | ConnectionTracer, RayScanner, ExitSimulator, FlightPathBuilder |
| `Generation/` | `ArrowRotate.Generation` (noEngineReferences) | LevelGenerator (prototip birebir portu) |
| `Tests/EditMode/` | `ArrowRotate.Tests.EditMode` | 27 test: SKILL.md §10'un 6 zorunlu sınıfı (4 config × 100 seed) + HexMath/Mulberry/Logic birim testleri |
| `Board/ Input/ Animation/ Gameplay/ GUI/ Integration/` | **Assembly-CSharp** (asmdef YOK — Gamebrain'e erişim için şart) | Görünüm: BoardView/TileView/SegmentView/IceView/TutorialPulse, TapController, FlightRenderer, HexaGameplayManager (durum makinesi), HexaHudPanel, Integration (aşağıda) |
| `Editor/` | Assembly-CSharp-Editor | `HexaLevelEditorWindow` (**Arrow Rotate ▸ Level Editor** — görsel editör) + `HexaSeedBrowserWindow` (**Arrow Rotate ▸ Seed Browser** — toplu seed tarama/bake) |

Saf C# katmanına (Core/Logic/Generation) MonoBehaviour/Unity API EKLENMEZ; testler her zaman yeşil kalmalı (Test Runner → EditMode → ArrowRotate.Tests.EditMode, ~6 sn).

### Renk & Tema (ScriptableObject — arrowJam deseni)

İki SO tüm renklerin kaynağı (`Board/` altında, Assembly-CSharp). Her ikisi de `Active` static + Resources fallback deseni kullanır — sahne referansı gerekmez.
- **`HexaColorDatabase`** (`Resources/HexaColorDatabase.asset`): ok PALET renkleri (INT-index'li dizi, 0..N-1 — arrowJam'in SnakeColor enum'unun aksine, çünkü palet level'larda/generator'da/spec §6'da int) + `SegmentColor`. `Active.ForPalette(i)`, `AllColors()`. Varsayılanlar SKILL §6 birebir.
- **`HexaThemeData`** (`Resources/Themes/Theme_Default.asset`, `Theme_Light.asset`): `CameraBackground` (2D), boş hücre/grid renkleri, `SegmentShadow` (Shapes2D ok gölgesi), buz renkleri (Fill/Edge/Crack/Badge). `Active` set edilince `ApplyCamera()` çalışır. Not: Hexa runtime board'u grid arka planı çizmez → tema arrowJam'den küçük.
- **Runtime tema güncelleme:** `BoardView.RefreshTheme()` — aktif temayı yeniden uygular (gölge renkleri + kamera bg canlı; taş renkleri paletten, buz renkleri oluşturulurken sabit → onlar için rebuild). BoardView inspector'ında **"Temayı Güncelle (runtime)"** butonu (Play modunda; `BoardViewEditor`). ⚠ ScriptableObject alan değişiklikleri play modunda KALICIDIR (scene gibi geri alınmaz) — asset renklerini test için değiştirdiysen play sonrası geri al.
- **`HexaPalette`** artık bu SO'ları okuyan ince bir **facade** (Background/Segment/Palettes/ForPalette). Tüm mevcut çağrı yerleri (BoardView, SegmentView, FlightRenderer, HUD, editör, IceView) değişmeden çalışır. Renk değiştirmek için kod değil ASSET düzenlenir.
- Aktif tema değiştirme: `BoardView.Theme` inspector alanı (boşsa Theme_Default) — Build'de `HexaThemeData.Active` olur. Runtime programatik değişim: `HexaThemeData.Active = <asset>`. Palet index'i eklemek/çıkarmak güvenli (editör palet seçici + IceView dinamik).
- Sahne durumu: **Game = Theme_Light**, HexaSandbox = Theme_Default (karşılaştırma için). Taş (hexagon) renkleri her zaman HexaColorDatabase'ten (palet), segmentler beyaz.

### Gamebrain bağlantısı (dokunulan yerler)

- **Boot.unity**: "Game Manager" objesindeki component `HexaGameManager : GameManager` ile değiştirildi (inspector'daki data referansları korunarak). `_gameConfig` → `Assets/_Arrow Rotate/Data/Hexa Game Config.asset`. Level sonu +25 coin burada verilir (`_coinRewardPerLevel` alanı).
- **Game.unity**: yalnız "Hexa Arrows" GO (BoardView + HexaGameplayManager + TapController). Example kalıntıları (trigger'lar, kamera, ışık) silindi — kamera Boot'taki `CameraManager.GameplayCamera`.
- **HUD (2026-07-22 değişti): `HexaHudPanel` artık UI'ı runtime KURMAZ — sahnedeki hazır hiyerarşiye bağlanır.** Yer: **Gameplay Panel.prefab** (GUI sahnesi) içindeki `Gameplay Items Container > Hexa HUD Panel > Bar > Timer/Moves/Chips`. Serialize alanlar prefab'da bağlı: `_bar` (göster/gizle kökü), `_timerText`, `_movesText` (TMP), `_chipsContainer` (GridLayoutGroup'lu Chips), `_chipTemplate` (opsiyonel — verilirse ok başına klonlanır ve şablon gizlenir; boşsa kod-içi daire çip fallback), `_checkSprite` (`Icon_WhiteIcon_check_m`, guid cc04d8c6…). Sadece EventBus dinler (gameplay'e referans yok). BuildChips container'ı temizleyip ok başına çip üretir → **editördeki örnek çipler runtime'da otomatik silinir**.
  - **Onay işareti (2026-07-22):** her çip oluşturulurken üstüne gizli bir "Check" Image (fill, `_checkSprite`, preserveAspect) eklenir; ok ÇIKINCA (`OnArrowExited`) `Check.enabled=true` → renk tam kalır, üstünde beyaz tik belirir (çip 0.9× küçülür). Eski alpha-fade sinyali yerini tike bıraktı.
  - ⚠ **Artık 2 ESKİ HexaHudPanel instance'ı kaldı** (GUI sahnesinde `Hexa HUD` standalone + `Gameplay Items Container/Hexa HUD Sample Prefab`). Üçü de EventBus'a abone → çakışma. Yalnızca `Hexa HUD Panel` kalmalı; diğer ikisi silinmeli/devre dışı bırakılmalı (kullanıcı temizleyecek).
- `HexaGameState_Gameplay : GameState_Gameplay` gameplay state'ini değiştirir (Example deseni); `HexaLevelData : LevelData` hücre dizisini taşır (bkz. "Level içeriği ve formatı"), Scene alanı boş (Level.Load sahne yüklemez).
- Build sırası: Boot → Game → GUI. `Assets/_Arrow Rotate/Scene/HexaSandbox.unity` = Gamebrain'siz izole test sahnesi (`HexaSandboxDriver`: TapCell/SolveArrow/SolveAll).
- **Android build fix (2026-07-22):** Gradle `:launcher:checkReleaseDuplicateClasses` → "Duplicate class kotlin.*.jdk7/jdk8" hatası veriyordu. Sebep: farklı plugin'ler `kotlin-stdlib:1.8.22` ile eski `kotlin-stdlib-jdk7/jdk8:1.6.21` getiriyor; Kotlin 1.8'den beri jdk7/jdk8 sınıfları ana stdlib'e taşındığı için duplicate. Fix: **Custom Launcher Gradle Template** (`Assets/Plugins/Android/launcherTemplate.gradle`, PlayerSettings `useCustomLauncherGradleManifest=1`) dependencies'ine `implementation platform('org.jetbrains.kotlin:kotlin-bom:1.8.22')` — tüm kotlin-stdlib-* modüllerini 1.8.22'ye hizalar (üst-küme, yukarı zorlamak güvenli). ⚠ Template Unity'nin varsayılanının BİREBİR kopyası + tek satır (tüm `**TOKEN**`'lar korunmalı; yoksa build büsbütün bozulur). Unity sürümü değişirse varsayılan template değişebilir — güncelle.

### Level içeriği ve formatı

**Format (2026-07-13'te değişti): leveller asset'te TAM HÜCRE DİZİSİ olarak saklanır** (arrowJam deseni) — seed alanı kalktı, elle düzenlenebilir:
- `HexaLevelData`: `Radius` + `HexaCellSave[]` (q, r, arrowId, type, a, b, **rot**, **layer**) + `HexaArrowSave[]` (palette, freezeAt). `ToHexaLevel()` / `FromHexaLevel()` dönüşümleri.
- **Cells dizisi invariant'ı:** ok sırasında ve her ok içinde kuyruk→head sıralı. Ardışık hücreler axial komşu.
- **Rot'lar asset'te saklanır** — editördeki başlangıç dizilimi oyundakiyle birebir aynı; runtime scramble YOK.
- `Data/Levels/HexaLevel_001..050.asset` → `Hexa Game Config._levels`. **Seed Browser** toplu tarama/ayıklama için durur; bake artık hücrelere yazar.

### Katman (Layer) mekaniği — 2026-07-22

Yüzeyin altında 2 katmana kadar gömülü hücre olabilir; üstteki taş temizlenince altındaki yüzeye çıkar. Oyuncu gömülü oka ulaşmak için önce üstünü kapatan okları çıkarmak zorundadır (derinlik).

**Veri modeli (kilit tasarım):** `HexaLevel.Cells` SADECE yüzeyi (Layer 0) tutar; gömülüler `Buried[(q,r)]` yığınında (Layer artan, index 0 = sıradaki). Bu sayede `ConnectionTracer`/`RayScanner`/tap DEĞİŞMEDEN doğru davranır: kısmen gömülü ok kendiliğinden bağlanamaz, gömülü hücre ışın engeli değildir.
- `Cell.Layer` (0=yüzey, 1..`HexaLevel.MaxBuriedLayers`=2 gömülü) · `AddCell` Layer'a göre yönlendirir · `PromoteAt(pos)` yüzey BOŞKEN en üsttekini terfi ettirir, kalanların Layer'ını azaltır · `GetArrowCell(id,pos)` yüzey+gömülü arar (oklar katmanlara YAYILABİLİR — kullanıcı kararı) · `IsFullySurfaced(arrow)`.
- ⚠ `ConnectionTracer.Trace` tail SAHİPLİK kontrolü yapar (`tail.ArrowId != arrowId → NotConnected`) — gömülü okun tail pozisyonundaki yüzey hücresi BAŞKA okunsa sahte bağlantı üretebilirdi (yaşanmadan yakalanan bug).
- **Terfi akışı (HexaGameplayManager.StartFlight):** hücre `Cells`'ten silinir → görseller `DetachTile/DetachSegment` ile sözlükten AYRILIR (aynı (q,r) anahtarına terfi eden bağlanacak; eskiler uçuş bitince yok edilir) → `PromoteAt` VERİDE anında (yeni yüzey hemen engel/tap olur) → `Board.PromoteCellVisual(pos, delay)` GÖRSELDE taş kaybolurken yükseltir.
- **Görsel (yalnızca XZ; 2D modlar gömülü ÇİZMEZ):** gömülü taş+segment gerçek derinlikte (`y=-Layer·StackStepY`, StackStepY=taş kalınlığı) ve koyu (`LayerDimFactors` 1/0.55/0.35, `TileView/SegmentView.SetDim`); terfi = 0.4s OutCubic yükselme + renk açılma (`RiseRoutine`). Gömülüler gölge atmaz, terfi edince açılır.
- **Kayıt:** `HexaCellSave.Layer` (varsayılan 0 → ESKİ ASSET'LER OTOMATİK UYUMLU).
- **Doğrulama/simülasyon:** `ExitSimulator.CanExitAllLayered(level)` — dinamik sim (statik blockedBy grafiği yetmez: terfi eden hücre YENİ engel olabilir). Kural: ok çıkabilir ⇔ eşik dolu VE tam yüzeyde VE ışın temiz; çıkınca hücreler silinir + terfiler işlenir.
- **Editörde:** Katman seçici (0·Yüzey / 1·Alt / 2·Dip) — TÜM araçlar aktif katmanda çalışır; canvas aktifi tam renk, derindekileri koyu, aktifi ÖRTEN üst katmanları kontur çizer. Edit denetçisinde hücre başına Katman slider'ı (dolu katmana taşıma reddedilir). Doğrulama ekleri: katman başına çakışma, "katman L'nin üstünde L-1 şart" (üstü boş gömülü asla çıkamaz), palet komşuluğu KATMANLAR-ARASI (terfi sonrası yan yana gelebilirler — AutoAssignPalettes de böyle), katmanlıysa deadlock kontrolü `CanExitAllLayered`.
- Buz görselleri XZ'de zaten ertelenmişti; buz MANTIĞI katmanlarla çalışır (eşik kuralı aynı).
- Testler: `LayerMechanicTests` (7 test: yönlendirme, terfi+kayma, kısmen gömülü bağlanamaz, terfi sonrası bağlanır, katmanlı sim çözülebilir/deadlock/terfi-engeli).

**Katmanlı Random Fill üretimi (2026-07-22):** `LevelGenerator.GenerateLayered` (cfg.LayerCount>1'de otomatik seçilir; düz yol dokunulmadı). Editör Random Fill'de "Özel" modu: **Ok Sayısı · Katman (1-3) · Yayılan Ok · Buzlu Ok** slider'ları (`LevelConfig.ForCustom`).
- **Kilit fikir — kolon-yığma:** hücreler (q,r) kolonuna yığılır; yerleştirme anında katman = kolonun mevcut yüksekliği (0=yüzey). Böylece KAPSAMA (alt katmanın üstü daima dolu) YAPI GEREĞİ garanti. "Flat" oklar yalnız boş kolonda yürür (hep katman 0); "spanning" oklar dolu kolona basıp gömülü hücre üretir → parçaları farklı katmanda.
- Yayılan ok sayısı HEDEF (best-effort): son S ok flatlerden sonra yerleştirilip dolu kolona yönlendirilir; gerçekleşmezse retry, olmazsa daha az (konsola `ReportSpanning` gerçek sayıyı + katman dağılımını yazar). Testte ortalama hedefin ≥%100'ü tutuyor.
- **Gömülü parça min/max (`BuriedMin`/`BuriedMax`):** yayılan ok başına yüzey ALTINDAKI (layer≥1) parça sayısı [min,max]'tan rastgele, `[1, len-1]`'e kırpılır (yüzeydeki = uzunluk − gömülü). `WalkLayered` buriedTarget'a göre yönlendirir: gömülü açığı varsa dolu kolona, doldu ise boş kolona. bmin=bmax=k → uzunluk yeten her yayılan okta TAM k gömülü (test edildi 60/60).
- Çözülebilirlik `ExitSimulator.CanExitAllLayered` ile PAZARLIKSIZ doğrulanır (statik DAG değil — dinamik, terfi eden hücre yeni engel olabilir); tutmazsa seed atılır. Palet `AssignPalettesLayered` katmanlar-arası komşuluğa bakar. Scramble/ice `GetArrowCell` ile gömülü hücreye erişir (Cells'te değiller).
- ⚠ Katman>1'de ScrambleLayered/BuildCellsLayered `level.GetCell` YERİNE `GetArrowCell` kullanmalı — gömülü hücre `Cells`'te yok, GetCell yüzeydeki BAŞKA okun hücresini döndürür.
- Testler: `LayeredGeneratorTests` (5 test × 4 kombinasyon × 40 seed: çözülebilir, kapsama+katman-boşluğu yok, derinlik≤katman, yayılma hedefi, katmanlar-arası palet ayrımı). Toplam 39/39 yeşil.

### Level Editor (`Arrow Rotate ▸ Level Editor`)

Asıl level düzenleme aracı — arrowJam editörünün hex uyarlaması. Tüm düzenlemeler Undo destekli, doğrudan `HexaLevelData` asset'ine yazılır (`Record` → değişiklik → `Dirty` deseni).

**Dosya yapısı** (partial class, `Scripts/Editor/`):
| Dosya | Sorumluluk |
|---|---|
| `HexaLevelEditorWindow.cs` | Pencere/yerleşim, sol panel (level listesi, rename/duplicate/sil, GameConfig checkbox), veri yardımcıları (`CellAt`, `CellIndicesOfArrow`, `RemoveArrow`, `Record/Dirty`) |
| `HexaLevelEditorWindow.Canvas.cs` | Hex canvas çizimi (taş/segment/buz/hayalet/önizleme) + input (hotControl deseni) + araç davranışları (ToolDown/Drag/Up) |
| `HexaLevelEditorWindow.Tools.cs` | Sağ panel (araç seçimi, palet, denetçi, level ayarları), Paletleri Ata / Scramble / Çöz, Random Fill, doğrulama |

**Koordinat sistemi:** Canvas GUI uzayında çalışır (y AŞAĞI) → prototipin SVG formülleri BİREBİR: merkez `x=1.5·S·q, y=√3·S·(r+q/2)`, açı `30+60d`. Runtime'daki y-aynalama editörde YOKTUR. Hit-test: fractional axial + `HexMetrics.AxialRound` (yönelim-bağımsız). Layout her frame bölge bbox'ından hesaplanır (`ComputeLayout`).

**Araçlar (SelectionGrid, sağ tık her araçta okun tamamını siler):**
- **Draw** — boş hücrede başlar; hex-komşu adımlarla yürür (hızlı sürüklemede hücre atlamaz, geri adım son hücreyi siler). Bırakınca: başlangıç=Tail, son=Head, aralar=Mid; a/b otomatik, head uçuş yönü `Opp(a)` (düz devam), rot=0, palet=seçili. **Min 2 hücre** — tek tık hiçbir şey üretmez (hexa'da tek hücrelik ok geçersiz).
- **Erase** — tıklanan hücrenin OKUNU tümüyle siler. `RemoveArrow` arrowId'leri yoğun tutmak için büyük id'leri bir azaltır (runtime `level.Arrows[arrowId]` indekslemesi buna dayanır).
- **Move** — tüm ok taşınır (anchor'a göre axial offset); hedef bölge içi + boş (kendi hücreleri hariç) değilse kırmızı hayalet, commit edilmez.
- **Rotate** — hücre rot+1 (oyundaki tap'in editör karşılığı; başlangıç karışıklığını elle ayarlamak için).
- **Recolor** — okun paletini seçili swatch'a boyar (drag ile çoklu).
- **Ice** — okun FreezeAt'ini döndürür: 0→1→2→3→0.
- **Edit** — hücre denetçisi: A/B/Rot slider'ları + ok'un Palet/Buz Eşiği; seçili HEAD'e ikinci tık uçuş yönünü döndürür (girişle aynı yöne gelirse bir daha atlar).

**Butonlar:** *Paletleri Ata* = deterministik çizge boyaması (komşu oklara farklı palet, en az kullanılan tercih; katmanlar-arası; çözemezse uyarı loglar). *Scramble* = tüm rot'lar rastgele + ok başına "bağlı kalmaz" garantisi (head rot bump, üreticiyle aynı algoritma). *Çöz* = tüm rot=0 (çözümü görsel kontrol için; kaydetmeden önce Scramble'la). *Random Fill* iki mod: **preset** (zorluk 1/2/3-4/5+) veya **Özel** (Ok Sayısı · Katman 1-3 · Yayılan Ok · Buzlu Ok slider'ları → `LevelConfig.ForCustom`) + seed → `LevelGenerator.Generate` çıktısını level'a yazar. Katman>1'de kolon-yığma üretimi + `CanExitAllLayered` doğrulaması; gerçek yayılan ok sayısı konsola yazılır.

**Katman seçici (editör):** Sağ panelde **0·Yüzey / 1·Alt / 2·Dip** — TÜM araçlar aktif katmanda çalışır. Canvas: aktif katman tam renk + beyaz segment, daha derin katmanlar koyu + soluk segment, aktifi ÖRTEN üst katmanlar renkli KONTUR (altındaki aktif hücre okunur). Draw/Erase/Move/Rotate/vb. `CellAt(q,r)` → aktif katmandaki hücre. Edit denetçisinde hücre başına **Katman** slider'ı (dolu katmana taşıma reddedilir).

**Doğrulama (panel altında canlı, tüm kontroller):** ① çakışma (aynı hücre+KATMAN iki kez) ② bölge dışı hücre ③ geçersiz arrowId/katman ④ **katman kapsaması** (L>0 hücrenin üstünde L-1 şart — üstü boş gömülü asla çıkamaz) ⑤ ok yapısı (≥2 hücre, ilk Tail/son Head/ara Mid) ⑥ bitişiklik ⑦ çözülebilirlik (ok başına mini-level rot=0 Trace) ⑧ başlangıç karışıklığı ⑨ palet komşuluğu (KATMANLAR-ARASI) ⑩ deadlock — katmanlıysa `CanExitAllLayered` (dinamik), düzse `CanExitAll` (statik grafik). Yapı bozuksa mantık kontrolleri atlanır.

**GameConfig üyeliği:** listedeki checkbox `Hexa Game Config._levels`'a ekler/çıkarır (SerializedObject ile; sıra = listedeki mevcut sıra + sona ekleme). Level silinirken config'ten de düşülür.

**Bilinen sınırlamalar:** yarıçap küçültülünce dışarıda kalan hücreler otomatik silinmez (doğrulama ② işaretler); tip (Tail/Mid/Head) denetçiden değiştirilemez (zincir invariant'ını korumak için — gerekiyorsa oku silip yeniden çiz); tek hücre taşıma/silme bilinçli olarak yok.

### Görünüm modları (2D / 3D)

`BoardView.ViewMode` (inspector): **Flat2D** = prosedürel düz hex (prototip görünümü) · **Depth3D** = `hexagon.fbx` puck taşlar · **Shapes2D** = Shapes kütüphanesi ile vektör kalitesinde çizim (yuvarlatılmış `RegularPolygon` taşlar + `Polyline`/`Triangle`/`Disc` oklar). **Game sahnesi şu an Shapes2D** (en iyi 2D görünüm), HexaSandbox Flat2D. Dönmek için ViewMode değiştir. Oklar/uçuş düz katmandır (z=-0.1..-0.25, taş üstünde).

**Shapes2D notları** (`Assets/Shapes`, Freya Holmér — namespace `Shapes`, asmdef `ShapesRuntime` autoReferenced):
- Taş: `TileView.CreateShapes` → `RegularPolygon` (Sides=6, Angle=0 flat-top, Radius=0.985·S, Roundness=0.14). Renk/alfa `_poly.Color` (fade/vanish `SetAlpha` branch'i).
- Segment: `SegmentView.BuildShapeShapes` → beyaz `Polyline` (Round joins, Closed=false) + tail `Disc` + head `Triangle` (Roundness=0.5). **Head = prototip yapısı (reference `segLocalDrawing` 'head' birebir):** şaft `EdgePoint(A) → merkez → dirB·stubDist` (İKİ kollu: giriş kolu + uçuş kolu), ok ucu uçuş kolunun ucunda B'ye bakar. stubDist=0.30·apo, headLen=0.42·s (prototip 0.62·apo+uzun uç kullanır; köşeye taşmasın diye kısaltıldı). ⚠ Şaftın B-kolunu SİLME — sadece giriş kolu bırakılırsa dönüşlü head'lerde şaft "60° kaçık/eksik" görünür (yaşanmış bug). "Main" kabı altına çizilir; sonra `BuildShadow` main'i klonlayıp **yerinde büyütülmüş** (çizgi kalınlığı ×1.6, nokta/üçgen ×1.28 kendi merkezinden — transform scale DEĞİL, yoksa uçlar kenardan taşar) gölge kopyası ekler. SortingOrder: gölge=8, main=10, taş=0.
- **Gölge:** rengi **temadan** (`HexaThemeData.SegmentShadow`), runtime'da `SetShadowColor`/`BoardView.RefreshTheme` ile güncellenir. `SegmentView.DrawShadow` static bayrağı aç/kapa (şu an `true`; izolasyon/debug için `false` yapılabilir).
- Henüz Shapes'e ÇEVRİLMEDİ: `IceView` (mesh+LineRenderer) ve `FlightRenderer` (LineRenderer) — çalışıyor ama görsel olarak Shapes taşlarla tam uyumlu değil; istenirse çevrilir.

**Depth3D XZ (güncel yol — 2026-07-21):** Board GERÇEK XZ düzleminde (dünya Y=0, planarY→dünya Z; kamera yukarıdan eğik bakar). Gate: `BoardView.Is3DXZ` (= Depth3D + HexMesh3D atanmış). 2D modlar XY'de dokunulmadan durur.
- **Taş:** kullanıcının EP hexagon'undan bake edilmiş `hexagon_tile_mesh.asset` (`Prefabs/Sample Model Prefabs/`), XZ/Y-up, köşe yarıçapı ~1 → S ile ölçek. `TileView.Create3DXZ`; vanish dönüşü Y ekseni (`_spinAxis`).
- **Segmentler TEK PARÇA prosedürel mesh** (`SegmentMesh3D` üreticisi, EP arm oranları: w=0.28·S, h=0.30·S, fillet=0.10·S): yuvarlatılmış-dikdörtgen kesit yol boyunca süpürülür; **açılı birleşimler kavisli dirsek** (JoinRadius=0.2·S); hücre kenarında uçlar DÜZ kesim → komşu segmentle yüz yüze, bağlı ok kesintisiz okunur, z-fight yok. **Head:** giriş→merkez→B stub kolu + tabanı stub'a oturan aynı yükseklikte ok başı, `CombineMeshes` ile tek mesh (⚠ B stub'u ve taban-oturması SİLİNMEZ — bindirme/60°-kaçık görünüm yaşanmış bug'lar). Mesh'ler (Tip,A,B,s) anahtarıyla önbelleklenir; tap dönüşü Y ekseni +60°/tap.
- **Head geometrisi (2026-07-21 ikinci tur, yaşanmış üç bug):**
  - `HeadTipDist=0.75` (eski 0.6235): taban merkezden 0.27·S'e taşındı → **açılı girişte ok başı dirseğe/komşu segmente değmiyor**. ⚠ `FlightPathBuilder.Build(..., tipDist)` 3D'de `SegmentMesh3D.HeadTipDist·s` alır (HexaGameplayManager geçer) — tile ucu ile uçuş overlay ucu AYNI nokta, kalkışta sıçrama yok. 2D `tipDist=-1` → prototip 0.62·apothem korunur.
  - `BuildArrowhead` üst fillet'i GERÇEK poligon offset (köşe yay merkezi sabit, yarıçap cornerR−inset, min 0). ⚠ Eski normal-inset yöntemi inset>cornerR olunca outline'ı kendine kestiriyordu → görünür mesh defekti. Üst yüz halkası fan için KOPYALANIR (hard edge, düz gölgeleme).
  - Stub/şerit ok başının **0.04·S içine uzar** (SegmentView head'i + FlightRenderer3D `stripEnd`). ⚠ Düz kapak ok başı arka duvarıyla eş düzlemli kalırsa z-fight titremesi (defekt görünümü).
- **Açılı head'de ok başı öne itme (2026-07-21 üçüncü tur):** `SegmentView.HeadForwardBump(a,b,s)` — A/B kolları arasındaki açıya göre ok başını dirB boyunca öne iter (düzlükten sapma: 180°→0, 120°→0.5, 60°→1 × `HeadBendForwardMax=0.20·S`). Amaç: açılı head'de ok başının tabanı elbow'a/A koluna yapışmasın. Açı iki kol arasında olduğundan ROTASYONDAN BAĞIMSIZ → local (A,B) ile world (WorldA,WorldB) aynı bump'ı verir. ⚠ Uçuş seamless'ı için HexaGameplayManager `FlightPathBuilder.Build`'e `tipDist = HeadTipDist·s + HeadForwardBump(headCell.A,B,s)` geçer — tile ok başı ile uçuş yolu ucu birebir aynı nokta.
- **Editör testi tuzağı:** oyun penceresi odaksızken player loop DURUR (`Time.time` ilerlemez → coroutine'ler/uçuş çalışmaz, ok Connected'da asılı kalır). MCP'den test ederken `Application.runInBackground=true` yap (PlayerSettings.runInBackground açıldı — mobilde etkisiz, dev kolaylığı).
- **Uçuş/bounce = `FlightRenderer3D`** (LineRenderer YOK): her frame [kuyruk..uç] penceresi için SegmentMesh3D şeridi üretilir + uçta aynı ok başı yol tanjantında ilerler; kesit tile segmentleriyle birebir → seamless kalkış. Manager `Board.Is3DXZ` ile seçer; oturma yüksekliği `Board.SurfaceY`.
- **Gökkuşağı juice (2026-07-22) — kaydırılan gradient texture:** ok TEMİZ ÇIKARKEN (bounce'ta DEĞİL; tile segmentleri hep beyaz) strip+ok başı akan gradient olur. `Shaders/ArrowRainbow.shader` (`ArrowRotate/RainbowVertex`, URP unlit): `_GradientTex`'i UV.x'ten örnekler, `uv.x − _Time.y·_ScrollSpeed(0.5)` ile KAYDIRIR (Cull Off, ×_Glow). `FlightRenderer3D.RainbowGradient` = runtime üretilen 256×1 HSV gökkuşağı (wrap Repeat → kaydırma kusursuz döner) — **farklı gradient denemek için bu texture'ı değiştir**.
  - ⚠ **UV.x OK'A GÖRELİ** yazılır (her frame `WriteRainbowUV`): `uv.x = dist(vertexWorld, tailWorld)·RainbowUVScale(0.33)`, uv.y=0.5. `tailWorld` uçan kuyruğun dünya konumu. Dünya pozisyonuna bağlarsan ok hızlı uçarken sabit bantların içinden geçip STROBE/FLASH olur; göreli olunca bantlar okla taşınır, kaydırmayı yalnız shader _Time yapar → PÜRÜZSÜZ. Strip+ok başı AYNI tailWorld → kesintisiz.
  - ⚠ **Bounce'ta efekt YOK:** `Create` materyali BEYAZ kurar; yalnız `Fly()` → `EnableRainbow()` gökkuşağı materyaline çevirir + `_rainbow=true`. `Bounce()` çağırmaz → engelli ok beyaz çarpıp döner (doğrulandı: bounce Body shader = URP/Lit).
  - Shader bulunamazsa (`Shader.Find` null) beyaz kalır. ⚠ Build'de shader'ın dahil olması için materyal asset'i veya Always Included gerekebilir (runtime `Shader.Find`).
- **Taş boyutu/kalınlığı + segment gömme (2026-07-21):** `BoardView` ayar alanları. ⚠ Game.unity sahnesinde SERIALIZE edilmiş değerler script varsayılanlarını EZER — güncel kaynak sahnedir (inspector'dan kullanıcı ayarlar). Sahnedeki güncel değerler: `TileGap=0`, `TileThicknessY=1.5`, `SegmentSink=0.12`, `SegmentDropY=0.05`, `CastShadows3D=1`.
  - `TileGap`: taşlar arası boşluk (S oranı), footprint = 1−TileGap. Hücre aralığı SABİT (segment bağlantıları apothem·s midpoint'te buluşmaya devam eder). ⚠ Footprint 1'in ÜSTÜNE çıkarılırsa (negatif gap) taşlar örtüşüp transparent+ZWrite z-fighting yapar (1.1× footprint'te bile örtüşme, 1.2×'te belirgin tırtıklı dikişler — yaşandı).
  - `TileThicknessY`: taş kalınlık (Y) çarpanı. Mesh merkez pivotlu (bounds Y −0.25..+0.25) → hem üste hem alta büyür. `TileView.Create3DXZ(..., xzScale, thickness)` localScale=(s·xz, s·thick, s·xz).
  - `SegmentSink` + `SegmentDropY`: `SurfaceY = (bounds.max.y·TileThicknessY − 0.005 − SegmentSink − SegmentDropY)·CellSize` — kalınlaşan taş üst yüzeyini takip eder, segment/ok/uçuş hepsi buraya oturur (FlightRenderer3D de SurfaceY kullanır).
- **Materyaller `HexaColorDatabase`'ten (Resources/HexaColorDatabase.asset)** — burada denenir/güncellenir, BoardView alanları yalnızca YEDEK:
  - `MasterHexMaterial` + `HexMaterial` modu: **Master** = tüm hexagonlar tek materyal; **PerColor** = her `Entry.Material` (boşsa master). Hangi materyal olursa olsun renk MPB `_BaseColor` ile palete göre verilir → materyalde sadece yüzey özelliği (metallic/smoothness/shader) ayarlanır.
  - `SegmentMaterial`: ok+segment için tek materyal (renk `SegmentColor`/beyaz MPB ile). Uçuş da `BoardView` segMat'ini kullanır.
  - Çözüm sırası: DB > BoardView `TileMaterial3D`/`ArrowMaterial3D` yedeği > kod-içi `Lit3DTransparent`.
  - **Canlı deneme:** materyal asset'inin kendi özelliklerini değiştirmek anında yansır (sharedMaterial). Referans/mod değiştirince `BoardView.RefreshTheme()` (Güncelle) XZ'de materyalleri level'ı yeniden yüklemeden yeniden atar (`TileView.SetMaterial` / `SegmentView.SetMaterial`).
- **Tap:** XZ'de Y=0 düzlemine ışın (`TapController`), `WorldToAxial` world.z kullanır.
- **Kamera:** `FitCamera` XZ dalı — yukarıdan eğik (varsayılan 55°; CameraPrefab X-tilt'i >20° ise ondan), kadraj/zoom otomatik.
- **Gölge (2026-07-21):** `BoardView.CastShadows3D` (varsayılan açık). Üç şey birden gerekir, üçü de bununla hallolur: (1) ANA ışık gölge atmalı — `Setup3DLighting` `LightPrefab` (`GetComponentInChildren<Light>`) `LightShadows.None`'sa `Soft`'a zorlar; (2) taş+segment caster olmalı — `TileView/SegmentView.SetCastShadows(true)` (`ShadowCastingMode.Off` idi); (3) URP asset'te main light shadows açık (zaten açık, mesafe 150). Altına konan zemin plane'i `receiveShadows=true` + OPAK Lit materyal olmalı. ⚠ Transparent taş materyali `ShadowCastingMode.On` ile gölge ATAR (URP alpha'yı yok sayıp katı yazar) — test edildi, sorun değil.
- **Işık kurulumu (2026-07-21):** iki prefab alanı, ikisi de `Setup3DLighting`'de `transform`'un ALTINA instantiate edilir (her Build'de `Clear()` temizler → birikmez):
  - `LightPrefab`: ana yönlü ışık, gölge kaynağı (Soft). Sahnede şu an **"Board Light Prefab"** atanmış (Directional, i=1, Soft — kullanıcı "Board Light Prefab 2"den değiştirdi).
  - `HelperLightPrefab` (helperLight): sıcak dolgu ışığı (Directional, i=0.6, gölge YOK). `CastShadows3D` buna DOKUNMAZ (dolgu kasıtlı gölgesiz). Game.unity'de atandı (guid f790968...).
- **Zemin (2026-07-21):** Game.unity'de kalıcı **"Plane"** objesi var (y=−3, scale 4.31, MeshCollider, materyal `Assets/_Arrow Rotate/Material/ground.mat`) — taşların gölgesi buraya düşer. ⚠ Tap raycast'i etkilemez (TapController collider değil matematiksel Y=0 düzlemi kullanır) ama sahneyi kodla temizlerken bu objeye dokunma; kullanıcının zemini.
- **Environment lighting (2026-07-21, kullanıcı kararı):** `Setup3DLighting` ambient'i **Skybox kaynak + intensity 1.1** yapar (`RenderSettings.ambientMode=Skybox; ambientIntensity=1.1f`). Eski Flat/koyu ambient (0.25) kaldırıldı → tahta daha aydınlık, renkler canlı. ⚠ Skybox ambient için sahnede `RenderSettings.skybox` atanmış olmalı (yoksa ambient siyaha düşer).
- **XZ'de bilinçli ERTELENDİ (2026-07-21, kullanıcı kararı "buz kısmı kalsın şimdilik"):** buz görselleri. `IceView` XY'de; XZ Build dalında `if (arrow.FreezeAt > 0 && !xz)` ile atlanır → buz MANTIĞI çalışır (buzlu ok dönmez, eşik dolunca çözülür) ama buzlu okların XZ'de görsel işareti YOK. Buz görseli XZ'e taşınana dek Depth3D'de buzlu leveller görsel olarak eksik görünür.

**Depth3D (eski XY-tilted yol — fallback) sözleşmeleri:**
- **Model kontratı** (`Models/hexagon.fbx`): XZ düzleminde yatar, köşeler local Z ekseninde, pivot üst yüzey merkezinde, kalınlık local −Y. `TileView.Create3D` bunu X'te −90° + ekran düzleminde −30° döndürür (flat-top hizası), ölçeği mesh bounds'tan otomatik hesaplar (hedef köşe yarıçapı 0.91·S; kalınlık ×1.25). Model değişirse bu kontrat korunmalı.
- **Malzeme**: paylaşılan `MeshFactory.Lit3DTransparent` (URP Lit, Transparent surface, ZWrite açık) — alpha fade'ler (bağlanınca saydamlaşma) 3D'de de çalışır. Renkler MPB ile; `MeshFactory.SetColor` hem `_Color` (2D) hem `_BaseColor` (URP Lit) yazar, linear dönüşümlü.
- **Işık + kamera prefab'lardan** (`Assets/_Arrow Rotate/Prefabs/`, kullanıcı düzenler, oyuna yansır): `BoardView.LightPrefab` = **Board Light Prefab 2** (Build'de olduğu gibi instantiate; açı/şiddet/renk prefab'dan) · `BoardView.CameraPrefab` = **Camera Prefab** (FitCamera tilt=rotation ve arka plan rengini buradan okur). Prefab boşsa inline alanlar (`CameraTiltXDeg=-27.4`, `CameraBackground=#9BA9D1`) devreye girer. Mevcut değerler: kamera X eğim −27.4°, arka plan #9BA9D1.
- **Kadraj/zoom OTOMATİK** kalır (`FitCamera` board bbox'ından ortho size hesaplar) — level yarıçapı 3-7 değiştiği için prefab'ın sabit pozisyon/ortho size'ı kullanılmaz; yalnız tilt+bg okunur. Ambient Flat 0.25 (renkler doygun kalsın).
- `TapController` ekran→z=0 düzlemi ışın kesişimi kullanır — her modda ve her eğimde doğru (31/31 hücre round-trip doğrulandı).
- **Screenshot değerlendirme tuzağı**: MCP inline önizlemesi 288px'e küçülür ve renkleri soluk gösterebilir — görsel yargıya varmadan tam çözünürlük dosyasını kırpıp bak (`Assets/Screenshots/*.png`, 1080×1920).

### Değişmez kurallar

- `arrowId` (mantık kimliği) ≠ `palette` (görsel renk). TÜM mantık arrowId üzerinden. Aynı paletteki iki ok hiçbir hücrede komşu olamaz (test 6 bekçidir).
- **Y-ekseni sözleşmesi** (`HexMetrics` + `HexMathTests` ile sabit — DEĞİŞTİRME): Unity açısı = −(30+60d)°, hücre y'si negatiflenir, tap = z'de −60° (ekranda saat yönü).
- Mid segment görseli DAİMA merkezden kırık çizgi; uçuş polyline'ı da merkezlerden geçer.
- Uçuşa başlayan okun hücreleri `level.Cells`'ten ANINDA silinir (uçan ok engel sayılmaz).
- **Çarpma tek seferlik / engel değişince (2026-07-22, kullanıcı kararı):** bekleyen ok AYNI engele TEKRAR çarpmaz — yalnızca önündeki en yakın engel (blockers[0]) DEĞİŞİRSE yeniden çarpar. `Arrow.LastBouncedBlocker` (arrowId, -1=hiç) tutar; `TryLaunch` gate'i `if (LastBouncedBlocker == nearest) { State=Waiting; return; }` yoksa çarp+güncelle. ⚠ Gate state'e GÜVENMEZ — `OnFlightDone` zincir-fırlatmada state'i geçici `Connected`'a çevirdiği için yalnız `LastBouncedBlocker`'a bakar; skip'te tekrar `Waiting` yapılır ki sonraki zincirde yeniden denensin. Aksi halde sürekli çarpma olurdu (yaşandı: ok 2 kez çarpıyordu).
- **Uçuş hızı çarpanı (2026-07-22):** `HexaGameplayManager.FlightSpeedMultiplier` (varsayılan 1.6) — tamamlanan ok bununla çıkar; `FlightSpeed = 16.18·S·çarpan`. Taş vanish gecikmesi de FlightSpeed'e bağlı olduğundan tutarlı hızlanır.
- Hücre erişimi `Dictionary<(int q,int r), Cell>`; string key yasak.
- Level asset'lerinde hücre dizisi elle DEĞİL Level Editor ile düzenlenir; tek hücre silmek/taşımak zincir invariant'ını bozar (editör bu yüzden ok bazında siler/taşır).
- Zamanlamalar prototip birebir (HexaGameplayManager/SegmentView sabitleri): dönüş 160ms, kontrol 170ms, fırlatma 240ms, bounce 560ms, zincir 180+260ms, uçuş 16.18·S/sn **× FlightSpeedMultiplier (vars. 1.6)**. Değiştirmeden önce bu dosyaya yaz.
- MeshRenderer renkleri MPB ile ve **linear dönüşümlü** set edilir (`MeshFactory.SetColor`) — proje Linear color space.
- Framework koduna (`Assets/Gamebrain/`) dokunulmaz; genişletme yalnız subclass ile.

**Oyun event'leri** (`Scripts/Integration/Events/HexaEvents.cs`): `HexaLevelStartedEvent` (çip verisi `HexaArrowChipInfo[]` taşır), `HexaRotateEvent`, `HexaArrowConnectedEvent`, `HexaArrowExitedEvent`, `HexaArrowBlockedEvent`, `HexaIceBrokenEvent`, `HexaLevelWonEvent`, `HexaTutorialEvent`. Ses/haptik yalnız `FxRequestEvent` (dönüş=Drag, çıkış=RocketLaunch, engel=InvalidDrop, buz=Ice_1; klipler Feedbacks_SO'da henüz atanmadı).

### Bilinen notlar / tuzaklar

- **Prototipten bilinçli sapma (2026-07-13):** taş saydamlık akışı ters çevrildi. Prototipte taşlar bounce SONRASI kalıcı görünmez olur (`pulseWaiting`/`.tile.clear`); bizde ok BAĞLANINCA taşlar saydamlaşır (`TileView.FadeOut`), çarpıp geri dönerse bounce bitiminde GERİ AÇILIR (`FadeIn`); zincirleme yeniden fırlatmada tekrar saydamlaşır. `TileView.Vanish` mevcut alfadan başlar (saydam taş uçuşta geri parlamaz).
- Tutorial yalnız level index 0'da tetiklenir (`HexaGameState_Gameplay`); kayıt sıfırlamak için `~/Library/Application Support/DefaultCompany/Arrow Rot/Game Data.json` silinir.
- İlk N level'da ana menü atlanır — template özelliği (`GameData.InstantStartLevelWithoutMainMenu`), bug değil.
- ⚠ **Tap çift-tetikleme (2026-07-22, BUILD'de yaşandı, editörde görünmez):** cihazda dokunuş `Input.simulateMouseWithTouches` (varsayılan açık) yüzünden HEM `Input.GetMouseButtonDown(0)` HEM `Input.GetTouch(Began)` tetikler → tek dokunuş 2× Tap → 120° dönüş. Editörde touch olmadığından yalnız mouse → 60°, bu yüzden editörde YAKALANMAZ. Fix: `TapController.Update` touch VARSA mouse dalını ATLAR (`if (touchCount>0) {touch} else if (mouse)`). ⚠ Input'u mouse+touch birlikte okuyan her yerde bu tuzağa dikkat.
- TMP varsayılan fontunda ❄/✓ glifleri yok — HUD şimdilik sayı/soluk çip kullanıyor; art pass'te sprite ikon.
- Win panelindeki sayılar (level/ödül) template placeholder'ı — Faz 8'de bağlanacak.
- Editör odak dışıyken play modu kendiliğinden pause olabiliyor (MCP/arka plan testinde görüldü); test otomasyonunda `EditorApplication.isPaused=false` watchdog'u kullanılıyor, runtime'ı etkilemez.
- MCP ile script yazımından sonra Unity bazen import etmiyor — `AssetDatabase.ImportAsset(..., ImportRecursive|ForceUpdate)` ile zorla.
- MCP ile sahne bileşenine referans atarken `SerializedObject.ApplyModifiedPropertiesWithoutUndo` + hemen Play (play-from-Boot sahneyi diskten yeniden yükler) atamayı KAYBEDEBİLİR. Güvenli yol: alanı doğrudan set et + `EditorUtility.SetDirty(comp)` + `EditorSceneManager.MarkSceneDirty(scene)` + `SaveScene`, sonra dosyadan grep'le doğrula.

### Sahiplik ayrımı

Faz 0–7 tek elden yazıldı; bundan sonrası için: **Dev A** = Core/Logic/Generation + testler + Seed Browser + level içeriği · **Dev B** = Board/Input/Animation/GUI/Gameplay/Integration + sahne-prefab sahipliği (Boot/Game/GUI/HexaSandbox). `Cell`/`Arrow` modeli, Logic public API'si ve event imzaları ortak sözleşmedir — değişiklik önce bu dosyaya yazılır. Sahne dosyalarına aynı anda iki kişi dokunmaz.

---

## Project Layout

```
Assets/Gamebrain/
├── Scenes/          Boot.unity · Game.unity · GUI.unity (additive)
├── Scripts/
│   ├── Runtime/     Game logic (states, GUI, boosters, events, interfaces)
│   ├── Infrastructure/  Shared framework (state machine, pool, factory, GUIService)
│   ├── Utils/       EventBus, coroutine helpers, editor utilities
│   ├── Data/        ScriptableObject data managers
│   ├── Attribute/   Custom inspector attributes
│   ├── Economy/     Currency system
│   ├── Debug/       SRDebugger integration
│   └── Editor/      Editor-only tooling (main toolbar Boot toggle, config)
├── Modules/         Board mechanics (BoardObject, Ice, FireCracker, Jelly, Sand)
├── Prefabs/         UI panels, booster prefabs, camera
├── Materials/       Shared materials (bolt, fold)
├── Settings/        EditorConfig asset
├── Example/         Minimal runnable example (ExampleGameManager)
└── Tutorial/        Onboarding flow
```

---

## Game Flow

```
GameManager (MonoBehaviour, Boot scene)
    └── StateMachine
            ├── GameState_Main       ← main menu (initial state)
            ├── GameState_Gameplay   ← active gameplay
            ├── GameState_Win        ← level cleared
            ├── GameState_Loose      ← level failed
            └── GameState_Restart    ← replay same level
```

`GameManager` creates the `StateMachine`, passes a `GameStateContext` (holds all managers) into each state, and pushes `GameState_Main` first. State transitions are triggered by EventBus events — the states themselves subscribe to the relevant events and call `StateMachine.ChangeState(...)`.

> The first-run tutorial is **not** a game state — it lives in the `Tutorial/` folder as a separate flow, not under the state machine.

---

## Core Architecture Patterns

### State Machine — `Infrastructure/Common/State/`

| File | Role |
|---|---|
| `State.cs` | Abstract base. Override `OnEnter`, `OnUpdate`, `OnExit`. |
| `StateMachine.cs` | Holds current state, calls lifecycle hooks, exposes `ChangeState<T>()`. |
| `Transition.cs` | Condition + target-state pair (optional, for data-driven transitions). |

All game states extend `GameStateBase` (which extends `State`), and receive a `GameStateContext` on construction.

### EventBus — `Utils/EventBus/`

Typed, static publish-subscribe with zero coupling between senders and receivers.

```csharp
// Publish
EventBus<PlayRequestedEvent>.Raise(new PlayRequestedEvent());

// Subscribe (store binding to unsubscribe later)
EventBinding<PlayRequestedEvent> _binding;
_binding = new EventBinding<PlayRequestedEvent>(OnPlayRequested);
EventBus<PlayRequestedEvent>.Register(_binding);

// Unsubscribe
EventBus<PlayRequestedEvent>.Deregister(_binding);
```

All event types live in `Runtime/Event/` and `Runtime/Booster System/Event/`. Each is a plain struct/class — no base class required.

**Key game-wide events:**

| Event | Trigger |
|---|---|
| `PlayRequestedEvent` | Play button pressed |
| `RestartRequestedEvent` | Restart button pressed |
| `MainMenuRequestedEvent` | Back-to-menu pressed |
| `NextLevelRequestedEvent` | Advance to next level |
| `ReviveRequestedEvent / ReviveDeclinedEvent` | Revive popup interaction |
| `InputLockEvent / InputUnlockRequestedEvent` | Disable / re-enable input |
| `ScoreUpdateEvent` | Score changed |
| `FxRequestEvent` | Trigger sound or haptic |
| `SettingChangeRequestEvent / SettingIsChangedEvent` | Settings toggle |
| `CurrencyUpdatedEvent` | Coin/gem balance changed |

### GUI — `Infrastructure/GUI/`

`GUIService` is the single panel coordinator. Panels are shown/hidden by state enter/exit, never directly from game logic.

| File | Role |
|---|---|
| `GUIService.cs` | Registers and activates `UIPanel` instances by type. |
| `UIPanel.cs` | Abstract base. Override `OnShow` / `OnHide`. |
| `UIPopup.cs` | Abstract base for transient dialogs. |

Each game state activates its own panel on `OnEnter` and hides it on `OnExit`:

```
GameState_Main      → MainMenuPanel
GameState_Gameplay  → GameplayPanel
GameState_Win       → LevelCompletePanel
GameState_Loose     → LevelFailPanel
```

### Data (ScriptableObjects) — `Scripts/Data/`

| Asset | Holds |
|---|---|
| `GameData` | Player level index, coin count, progress flags |
| `BoosterGameData` | Booster counts per type |
| `SettingsData` | Audio/haptic on-off state |
| `GameMetaData` | Analytics / meta info |
| `Feedbacks_SO` | Sound and haptic clip mappings |

`DataManager` provides a single access point. `BaseGameData` handles serialization; extend it to add new save data.

---

## Major Systems

### Level System — `Infrastructure/System/Level System/`

```
LevelManager
    ├── Loads LevelData (ScriptableObject) for current index
    ├── Instantiates Level (ILevel)
    ├── Tracks LevelObjective completion → emits Win / Loose signals
    └── Advances GameData.currentLevel on success
```

`LevelData` contains: scene reference, objectives list, target score, and booster config. `Status` enum = `Success | Fail | NotCompleted`.

### Booster System — `Runtime/Booster System/`

```
BoosterManager
    ├── Reads BoosterGameData for counts
    ├── Listens to BoosterRequestedEvent
    └── Executes BaseBooster.Execute() → emits BoosterActionStartedEvent / BoosterActionEndedEvent
```

Built-in boosters: `HammerBooster`, `SwapBooster`, `RefreshBooster`, `GroupRemoverBooster`. Add new ones by extending `BaseBooster`.

### Board Object System — `Modules/Board Object/`

```
BoardObject (abstract)
    └── DamageableBoardObject (has Health)
            ├── ClayBoardObject   (1 HP)
            ├── WoodBoardObject   (2 HP)
            ├── IceBoardObject    (frozen — needs multiple hits)
            └── FireCrackerBoardObject (chain reaction on break)
    └── RewardedCellBoardObject  (tapped → shows rewarded ad → removed when ad is completed)
    └── CellLockBoardObject      (locked cell)
```

`BoardObjectFactory` creates pieces by `BoardObjectType` enum. Destroyed pieces raise `BoardObjectBrokenEvent`.

### Shop System — `Infrastructure/System/Shop System/`

`ShopSystem` processes `ShopItemPurchaseRequestEvent`, validates currency via `CurrencyManager`, and records a `PurchaseTransaction`. Items are described by `ShopCatalogData → ShopCategoryData → ShopItemData`. Currency types: `Coin` (soft), `Gem` (hard).

### Economy — `Scripts/Economy/`

`CurrencyManager` is the single authority for adding/subtracting currency. Raises `CurrencyUpdatedEvent` after each change. Do not modify `GameData` coin values directly.

### Feedback — `Runtime/FeedbackManager.cs`

Send an `FxRequestEvent` with a feedback key; `FeedbackManager` resolves it from `Feedbacks_SO` and delegates to `AudioManager` / `HapticManager`. Never call audio or haptic systems directly.

---

## Extending the Template

### Add a new Game State

1. Create `GameState_MyState.cs` in `Runtime/Game/States/`, extend `GameStateBase`.
2. Override `OnEnter` / `OnExit` / `OnUpdate`.
3. In `OnEnter`, call `GUIService.Show<MyPanel>()` and subscribe to relevant events.
4. Register the state in `GameManager` and add a transition trigger.

### Add a new UI Panel

1. Create `MyPanel.cs`, extend `UIPanel`. Override `OnShow` / `OnHide`.
2. Place the prefab under `Prefabs/UI/` and wire it to `GUIService` in the inspector.

### Add a new Event

1. Create a plain class/struct in `Runtime/Event/`.
2. Publish with `EventBus<MyEvent>.Raise(...)`.
3. Subscribe/deregister with `EventBinding<MyEvent>`.

### Add a new Booster

1. Create `MyBooster.cs`, extend `BaseBooster`, implement `Execute()`.
2. Add the type to `BoosterData` and `BoosterGameData`.
3. Create a prefab under `Prefabs/Boosters/`.

### Add a new Board Object

1. Create `MyBoardObject.cs`, extend `BoardObject` (or `DamageableBoardObject`).
2. Add entry to `BoardObjectType` enum.
3. Register in `BoardObjectFactory`.

---

## Custom Inspector Attributes

Use these to keep the inspector clean:

```csharp
[ShowIf("myBool")]    // show field only when myBool is true
[HideIf("myBool")]    // inverse
[HideIfAny("a","b")] // hide if any of the named fields are true
[DynamicRange("minField","maxField")]  // slider with runtime bounds
[Dropdown("MyList")]  // dropdown from a list property
[Tag]                 // Unity tag selector
[SceneDropdown]       // scene selector
```

---

## Object Pooling — `Infrastructure/Common/Object Pool/`

`PoolManager` holds references to `Pool` instances and manages their lifecycle — do not use it to get or release objects directly.
Use `Pool` to acquire and return individual objects.

```csharp
// Pool manages a specific type of object
Pool<MyObject> pool = new Pool<MyObject>();

// Acquire from pool
var obj = pool.Get();

// Return to pool
pool.Release(obj);
```

Use pools for any object spawned and destroyed frequently (board pieces, effects, projectiles).

---

## Scenes

| Scene | Purpose |
|---|---|
| `Boot.unity` | Entry point. Loads `GameManager`. |
| `Game.unity` | Main gameplay scene. Board lives here. |
| `GUI.unity` | Loaded additively on top of Game. All UI panels live here. |

Never put UI in the Game scene or gameplay objects in the GUI scene.

---

## Conventions

- **Events** are the only cross-system communication channel. No direct references between unrelated systems.
- **ScriptableObjects** for all persistent data. Never use `PlayerPrefs` directly.
- **GUIService** is the only code that shows or hides panels.
- **CurrencyManager** is the only code that modifies currency.
- **DataManager** is the only entry point for save/load.
- Manager initialization order is controlled by `GameManager.Awake()` — do not add `[RuntimeInitializeOnLoadMethod]` to subsystems.

---