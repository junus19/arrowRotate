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

### Buz görseli — XZ 3D (`IceView3D`, 2026-07-28)

XZ'de ertelenmiş olan buz görseli tamamlandı; **kullanıcının `Ice_Mat`** materyaliyle (Assets/_Arrow Rotate/Material/Ice_Mat.mat — URP Lit, **Transparent + additive** `_Blend:2`, `_ZWrite:0`, Smoothness 0.85, BaseColor siyah/alpha 0 → yalnız specular parlama = buzlu cam). `BoardView.IceMaterial3D` alanına Game.unity'de bağlı.
- ⚠⚠ **KİLİT BULGU (2026-07-28): `hexagon_tile_mesh` (EP puck) UV ve TANGENT TAŞIMIYOR** — runtime doğrulaması: `verts=950, uv=0, normals=950, tangents=0`. Bu yüzden texture/normal-map/smoothness haritası kullanan materyaller (Ice_Mat) o mesh'te düzgün görünmez, "sadece saydam" kalır (Unity'nin Cube/Plane'inde düzgün görünmesinin sebebi: onlarda UV+tangent var). **Aynı tuzak ileride texture'lı her materyalde geçerli** — puck mesh'ine texture'lı materyal verilecekse önce UV üretilmeli.
- **Çözüm — `MeshFactory.HexPlaneXZ()`:** XZ'de yatık, yarıçap 1, flat-top hexagon plane; **UV (0..1) + normal (+Y) + tangent** içerir, statik cache'li. Buz katmanı artık bu plane ile çiziliyor → Ice_Mat'in buz dokusu (çatlaklar/parlamalar) düzgün görünüyor (play-mode'da doğrulandı).
- **PREFAB KAPLAMA (2026-08-06, güncel yol):** `BoardView.IceCapPrefab` = **`IceHex_Prefab`** (Modules/Board Object/Mechanics/Ice/Prefabs). Verilirse aşağıdaki prosedürel gövde/plane YERİNE kullanılır. Yapı: `Hexa-RockTile` → `RockTile-Lvl01` (aktif, sağlam buz) + `Lvl01/02/03-Broken` (inaktif; kırık parçalar + `RockBreakParticles`). ⚠ **Buzda ÖLÇEK 1** (2026-08-06 kullanıcı kararı): `LockKeyFx.MakeCapPrefabUnscaled` — otomatik bounds-fit YOK; kaplama hücre merkezine, taşın ÜST YÜZEYİNE (`TileTopY`) oturur ve rastgele 60° döner. Boyut/yükseklik ince ayarı **prefabın kendi içinden** yapılır. (Ahşap kapak hâlâ bounds-fit'li `MakeCapPrefab` kullanır.) Kırılma (`BreakPrefabRoutine`): aktif mesh'ler (=sağlam buz) `enabled=false`, **üç `Lvl0X-Broken` seti aktifleştirilip `Play()`** edilir (taştan taşa 0.05s), 1.8s sonra yok olur.
  - ⚠⚠ **Rozet görünürlüğü:** buz materyali `IceTile` **renderQueue 3001**; rozet varsayılan 3000'de kalınca kaplama rozetin ÜSTÜNE çiziliyordu (ikisi de transparent, ZWrite yok → derinlik değil QUEUE belirliyor). Fix: rozet disk materyali queue **3200**, yazı materyali **3201** (runtime klonlar) + rozet `top + 0.85·s` yüksekliğe alındı + `Billboard.SetPull` ile kameraya doğru 1.0·s çekiliyor.
- **Prosedürel yol (IceCapPrefab boşsa) — hücre başına İKİ parça (2026-07-28):**
  1. **GÖVDE (`IceBody`)** — `Ice_Body` materyali (guid a834eba8…; URP Lit transparent, turkuaz 0.44/0.83/0.89, Smoothness 0.5, **texture YOK → UV gerekmez**, bu yüzden EP puck `HexMesh3D` kullanılabilir). Taştan %5 geniş, üstü `TileTopY + 0.05·CellSize`'de biter → **ok segmenti gövdenin üstünde görünür kalır**. `BoardView.IceBodyMaterial3D`.
  2. **ÜST KATMAN (`IceBlock`)** — `Ice_Mat`'li UV'li plane, taştan %2 geniş, `y = SurfaceY + 0.42·CellSize`.
  ⚠ Renk MPB ile BASILMAZ — materyallerin kendi görünümü korunur (`MeshFactory.SetColor` çağrılmaz). Gölge kapalı. Her iki parça da shake/break listesine girer.
- **Rozet:** orta hücrenin üstünde billboard disk + `TextMesh` kalan eşik sayısı (2D `IceView` stili). 3D okunabilirliği için (2026-07-28, kullanıcı değerleri): disk yarıçapı **0.52·s**, `characterSize` **0.11** (CellSize'dan bağımsız MUTLAK), fon **maviye yakın beyaz** (0.90/0.95/1.0, alpha 1), sayı **siyaha yakın** (0.07/0.08/0.11).
- **Davranış:** `Shake()` 0.3s titreme (buzlu taşa tap — hamle sayılmaz) · `SetRemaining(n)` rozet · `Break()` bloklar taştan taşa 0.05s arayla `TileView.Explode(IceTint)` parçacıklarıyla patlar + 0.18s küçülüp söner, rozet fade, sonra yok olur.
- `BoardView.ShakeIce/BreakIce/UpdateIceBadges` hem 2D (`_ices`) hem XZ (`_ices3D`) sözlüğünü işler → çağıran kod (HexaGameplayManager) DEĞİŞMEDİ. Play-mode'da doğrulandı (blok görünümü, rozet "1", shake, kırılma + 54 parçacık).

### Anahtar (Key/Lock) mekaniği — 2026-07-24 (v2: anahtar HEXAGONU)

Bir kısım ok KİLİTLİ (üstü kapalı, pasif). Board'da **bağımsız ANAHTAR HEXAGONLARI** vardır (ok DEĞİL). Bir uçan ok anahtar hexagonunun üstünden geçince (çarpınca) anahtar tetiklenir: hexagon bounce animasyonu oynar, anahtar ikonu kilide uçar, aynı gruptaki kilitli oklar açılır. **Kilit & anahtar aynı grupta AYNI RENK** (açık ton) → hangi anahtar hangi kilit belli.

**Veri:** `Arrow.LockGroup` (>=0 → kilitli) · `Arrow.Unlocked` (runtime) · `IsLocked`. Anahtarlar `HexaLevel.Keys` (`KeyCell {Q,R,Group,Triggered}`) — ok değil, obstacle değil (ok üstünden uçar). `KeyAt(q,r)` = tetiklenmemiş anahtar. Kayıt: `HexaArrowSave.LockGroup` + `HexaLevelData.Keys` (`HexaKeySave[]`). ⚠ Eski `KeyGroup` alanı KALDIRILDI (anahtar artık cell).
- **Gate (`OnTap`):** `if (arrow.IsLocked) { Board.ShakeLock(LockGroup); return; }` — hamle sayılmaz.
- **Tetikleme (`StartFlight` → `TriggerKeysOnPath`):** ok uçuşa geçince head'den exitDir ışınında anahtar hexagonu ararsa, ok o hücreye ulaşınca (delay=mesafe·perCell) `TriggerKeyDelayed`: grubu `Unlocked` yapar + `Board.TriggerKey(q,r,group)`. Anahtar obstacle DEĞİL → ok üstünden uçar (bounce yok).
- **Görsel (yalnız XZ; `LockKeyView.cs`):** grup renkleri `LockKeyFx.GroupColor(group)` (açık mavi/yeşil/pembe/sarı/mor/şeftali; kilit+anahtar aynı). `LockGroupView` = kilitli taşlarda **AHŞAP KAPLAMA PREFAB'I** (2026-08-06): `BoardView.LockCapPrefab` = `WoodHex_Prefab` (Modules/Board Object/Mechanics/wood/Prefabs; WoodHex.prefab varyantı — yalnız **`WoodLevel1`** aktif, `WoodBreakParticleL3` inaktif bekler). `LockKeyFx.MakeCapPrefab` instantiate eder, AKTİF mesh bounds'undan ölçüp taş genişliğine ölçekler ve **alt yüzeyini taşın üstüne** oturtur (hafif gömülü + `LockKeyFx.CapYOffset = -0.4` birim aşağı, kullanıcı değeri). Her kapak **rastgele 60° katı** döndürülür (hexagon 6-kat simetrik → yine tam oturur, ama tahta deseni her taşta farklı); ⚠ dönüş bounds ölçümünden ÖNCE uygulanır ki hizalama şaşmasın. Kilit açılınca `BreakPrefabCapsRoutine`: her kapağın mesh'leri anında `enabled=false`, içindeki **`WoodBreakParticleL3` aktifleştirilip `Play()`** edilir (taştan taşa 0.05s yayılım), ikon pop+söner, 1.6s sonra grup yok edilir. ⚠ Ahşap düz lid'den YÜKSEK → Lock ikonu kapak bounds'unun üstüne alınır (yoksa tahtanın içinde kalır). Prefab yoksa eski **koyu gri lid**'e düşer (0.36/0.36/0.38; ⚠ `MakeCap` rengi **HAM** basar — `MeshFactory.SetColor`'ın sRGB→linear çevrimi bu unlit yolda İKİNCİ kez uygulanıp koyu tonları siyaha düşürüyordu; play-mode'da ham vs çevrilmiş yan yana doğrulandı) + segment gizli + centroid'e en yakın hexta **Lock ikonu** (arkasında `LockKeyFx.MakeIconBackdrop` ile **grup renginin KOYU tonu**nda daire — `DarkTone`: hue korunur, doygunluk ×1.35, parlaklık ×0.42; ikonun ÇOCUĞU olduğu için billboard/animasyonu takip eder, rengi HAM basılır) (grup renginde, scale 0.9). `KeyCellView` = **gerçek KOYU 3D hexagon puck taş** (board taşları gibi `TileView.Create3DXZ`, flat lid DEĞİL — 2026-07-28) + üstünde grup renginde **Key ikonu**; `TriggerToLock` (2026-07-28, 2 faz): (1) **PATLAMA (ANINDA)** — ok çarptığı an `TileView.Explode(pos, tint, s)` parçacıkları patlar ve taş `Destroy` ile HEMEN yok olur; ⚠ taşta scale/squash animasyonu YOK (kullanıcı kararı: "hexagon hemen yok olsun, particle hemen patlasın"). Parçacıklar taş disintegration efektinin aynısı: 18'lik burst, ömür 0.4-0.75s, hız 3.75-9, gravity 2.2. (2) **ANAHTAR POP** — ikon 0.30s `OutBack` ile havaya fırlar + **belirgin scale taşması** (`PopScale=1.7×`, OutBack 1'i aştığı için yaylanır) → 0.10s nefes (pop okunsun) → (3) **UÇUŞ** — kilide yay çizerek gider, 1.7×'ten 0.55×'e küçülerek → `OpenLock(group)` → lid'ler kalkar, segmentler görünür (oklar aktif). ⚠ Animasyon ayrımı (kullanıcı kararı): **taşta hiç scale yok** (anında patlar), **anahtarda belirgin scale pop var**. ⚠ Parçacık rengi **grup tint'i** (`_explodeColor`) — taşın kendi koyu rengi arka planda okunmazdı. `TileView.Explode` = `SpawnDisintegration`'ın public sarmalayıcısı (taşa bağlı olmayan patlamalar için). `ShakeLock` = kilit titrer. İkonlar `Billboard`+`Sprites/Default` quad. Sprite'lar **Lock_1 / Key_1** (tek renk → grup rengiyle tint'lenir), `BoardView.LockSprite/KeySprite`, Game.unity'de bağlı.
- ⚠ 2D/Shapes modda görsel YOK; mantık her modda çalışır. Play-mode'da uçtan uca doğrulandı (koyu anahtar hexagonu + mavi ikon, ok çarpınca → tetiklenme → kilit açılır, kilit+anahtar aynı mavi).
- **Editör authoring:** Edit denetçisinde ok başına **Kilit Grubu** slider'ı (-1..3). Canvas'ta kilitli hücreler koyu kapak + grup renginde Lock ikonu; anahtar hexagonları (`_selected.Keys`) koyu hex + grup renginde Key ikonu (grup id etiketi). Doğrulama: her kilit grubunun anahtar hexagonu olmalı. **Random Fill "Kilitli Ok" + "Kilit Grubu" (ÇOKLU GRUP, 2026-07-28):** `AssignLockKey(level, count, groups, radius)` çıkış sırasını `BlockedBy`'dan Kahn-peel ile bulur; **anahtar okları = sıranın BAŞI** (grup sayısı kadar), **kilitliler = sıranın SONU** → anahtarlar önce çıkıp kilitleri açar, kalan sıra aynen işler ⇒ çözülebilirlik korunur (7/7 seed, kilit+buz+ışın farkındalıklı simülasyonla 9/9 ok doğrulandı).
- ⚠ **BUZLU oklar ne kilitli ne anahtar olur** (kullanıcı isteği: aynı hexagonda buz+kilit olmasın; ayrıca buzlu anahtar eşiği dolmadan tıklanamayacağı için kilidi asla açamaz = deadlock).
- ⚠ Kilitliler TÜM anahtar oklarından SONRA çıkmalı (`order` index > maxKeyIdx) — yoksa anahtar okunu kilitli bir ok bloklarsa deadlock.
- Anahtar hexagonu okun çıkış yönünde, bölge DIŞINDA: **önce 3 adım boşluk, olmazsa 4, olmazsa 5** (`KeyGaps`); dolu/başka anahtarın olduğu yer atlanır. Yerleşemeyen grup atlanır, kilitliler yalnızca YERLEŞEN gruplara dağıtılır (anahtarsız kilit = çözülemez). keyStep ≤ MaxSteps-1.
- ⚠ Uzak anahtar için **`FitCamera` anahtar hücrelerini de kadraja dahil eder** (yoksa ekran dışında kalırdı); editör canvas `ComputeLayout` de keys'i bbox'a katar.
- **Random Fill "Kilitli Ok" (2026-07-24, düz level):** üretilen levelda `BlockedBy`'dan Kahn-peel ile çıkış sırası bulunur; **anahtar = order[0]** (ilk çıkabilen), **kilitliler = en geç çıkan k ok** (hepsi grup 0). Kilit-farkındalıklı simülasyonla çözülebilirlik korunur (anahtar önce çıkar → hepsi açılır → kalan sıra aynen işler; 5/5 seed doğrulandı). Katman>1'de devre dışı (katmanlı çıkış sırası dinamik). `AssignLockKey` (Tools.cs).

### Katman (Layer) mekaniği — 2026-07-22

Yüzeyin altında 2 katmana kadar gömülü hücre olabilir; üstteki taş temizlenince altındaki yüzeye çıkar. Oyuncu gömülü oka ulaşmak için önce üstünü kapatan okları çıkarmak zorundadır (derinlik).

**Veri modeli (kilit tasarım):** `HexaLevel.Cells` SADECE yüzeyi (Layer 0) tutar; gömülüler `Buried[(q,r)]` yığınında (Layer artan, index 0 = sıradaki). Bu sayede `ConnectionTracer`/`RayScanner`/tap DEĞİŞMEDEN doğru davranır: kısmen gömülü ok kendiliğinden bağlanamaz, gömülü hücre ışın engeli değildir.
- `Cell.Layer` (0=yüzey, 1..`HexaLevel.MaxBuriedLayers`=2 gömülü) · `AddCell` Layer'a göre yönlendirir · `PromoteAt(pos)` yüzey BOŞKEN en üsttekini terfi ettirir, kalanların Layer'ını azaltır · `GetArrowCell(id,pos)` yüzey+gömülü arar (oklar katmanlara YAYILABİLİR — kullanıcı kararı) · `IsFullySurfaced(arrow)`.
- ⚠ `ConnectionTracer.Trace` tail SAHİPLİK kontrolü yapar (`tail.ArrowId != arrowId → NotConnected`) — gömülü okun tail pozisyonundaki yüzey hücresi BAŞKA okunsa sahte bağlantı üretebilirdi (yaşanmadan yakalanan bug).
- **Terfi akışı (HexaGameplayManager.StartFlight):** hücre `Cells`'ten silinir → görseller `DetachTile/DetachSegment` ile sözlükten AYRILIR (aynı (q,r) anahtarına terfi eden bağlanacak; eskiler uçuş bitince yok edilir) → `PromoteAt` VERİDE anında (yeni yüzey hemen engel/tap olur) → `Board.PromoteCellVisual(pos, delay)` GÖRSELDE taş kaybolurken yükseltir.
- **Görsel (yalnızca XZ; 2D modlar gömülü ÇİZMEZ):** iki stil, `BoardView.BuriedStyle` ile seçilir:
  - **`Nested` (VARSAYILAN, 2026-07-23):** gömülü hücre üstteki taşın İÇİNDE küçük hexagon olarak çizilir (`localScale ×= NestScale^Layer`, NestScale=0.5). ⚠ İç taş dış taşa **GÖMÜLÜ/kakma** oturur: `tp.y = TileTopY - innerHalfUp + NestRaise·CellSize` → üstü dış yüzeyle ~hizalı (NestRaise=0.02 hafif taşma), gövdesi dış taşın İÇİNDE → **dış okun segmenti (beyaz ok) üstte görünür kalır** (kullanıcı isteği 2026-07-23). **İç segment GİZLİ** (`sv.SetVisible(false)` — ok pasif). Üstteki temizlenip terfi olunca `NestGrowRoutine`: küçük→tam boy `OutBack` büyür, y yüzeye iner, segment `SetVisible(true)` ile açılır → ok AKTİF/etkileşimli olur. Kullanıcının "yeşil hexagon içinde küçük kırmızı hexagon, tamamlanınca büyüyüp aktifleşir" mekaniği bu; layer veri modelini AYNEN kullanır, yalnız görsel farklı (play-mode'da uçtan uca doğrulandı).
  - **`StackedBelow` (eski):** gömülü taş+segment gerçek derinlikte altta (`y=-Layer·StackStepY`, StackStepY=taş kalınlığı), tam boy; terfi = `RiseRoutine` (0.4s OutCubic yükselme).
  - ⚠ **Koyulaştırma KAPALI (2026-07-22, kullanıcı kararı):** `LayerDimFactors={1,1,1}` → alt katmanlar GERÇEK palet renginde, siyah/koyu DEĞİL. (`SetDim` altyapısı duruyor; koyulaştırma istenirse buradan.) Gömülüler gölge atmaz. `_buriedViews` = `Dictionary<(q,r), List<(TileView, SegmentView, int layer)>>` (layer artan sıralı, index 0 = sıradaki terfi).
- **Kayıt:** `HexaCellSave.Layer` (varsayılan 0 → ESKİ ASSET'LER OTOMATİK UYUMLU).
- **Doğrulama/simülasyon:** `ExitSimulator.CanExitAllLayered(level)` — dinamik sim (statik blockedBy grafiği yetmez: terfi eden hücre YENİ engel olabilir). Kural: ok çıkabilir ⇔ eşik dolu VE tam yüzeyde VE ışın temiz; çıkınca hücreler silinir + terfiler işlenir.
- **Editörde:** Katman seçici (0·Yüzey / 1·Alt / 2·Dip) — TÜM araçlar aktif katmanda çalışır; canvas aktifi tam renk + beyaz segment, aktiften DERİN katmanları **içte küçük hexagon (Nested önizleme, gerçek renk, segment gizli — `NestPreviewScale=0.5^derinlik`, runtime `BoardView.NestScale` ile eşleşir)**, aktifi ÖRTEN üst katmanları kontur çizer. Yani Random Fill (Katman≥2) çıktısı editörde de nested olarak görünür (2026-07-23). Edit denetçisinde hücre başına Katman slider'ı (dolu katmana taşıma reddedilir). Doğrulama ekleri: katman başına çakışma, "katman L'nin üstünde L-1 şart" (üstü boş gömülü asla çıkamaz), palet komşuluğu KATMANLAR-ARASI (terfi sonrası yan yana gelebilirler — AutoAssignPalettes de böyle), katmanlıysa deadlock kontrolü `CanExitAllLayered`.
- Buz görselleri XZ'de artık VAR (aşağıdaki `IceView3D`); buz MANTIĞI katmanlarla çalışır (eşik kuralı aynı). Gömülü (layer>0) hücreler buzla kaplanmaz — üstü zaten kapalı.
- Testler: `LayerMechanicTests` (7 test: yönlendirme, terfi+kayma, kısmen gömülü bağlanamaz, terfi sonrası bağlanır, katmanlı sim çözülebilir/deadlock/terfi-engeli).

**Katmanlı Random Fill üretimi (2026-07-22):** `LevelGenerator.GenerateLayered` (cfg.LayerCount>1'de otomatik seçilir; düz yol dokunulmadı). Editör Random Fill'de "Özel" modu: **Ok Sayısı · Katman (1-3) · Nested Sayısı · Buzlu Ok** slider'ları.
- **Gömülü Stil (2026-07-24):** Random Fill'de Katman≥2'de **"Gömülü Stil"** popup'ı — **İç içe (Nested)** [buried=1/ok] veya **Alt alta (Stacked)** [buried=1..Katman-1/ok → sütun yığını]. Seçim `HexaLevelData.StackedLayers`'a yazılır; `HexaGameState_Gameplay` yüklemede `Board.BuriedStyle`'ı buna göre ayarlar (Begin'den ÖNCE) → aynı proje iki stili de destekler. İkisi de play-mode'da doğrulandı.
- **Nested Sayısı (2026-07-23):** editörde "kaç gömülü ok" olacağını doğrudan seçer (Stacked'de "Yayılan Ok" etiketi). İçte generator'a `ForCustom(..., spanningArrows=nested, buriedMin=1, buriedMax=1)` verilir → her yayılan ok TAM 1 gömülü hücre → tam `nested` adet nested. Eski "Yayılan Ok + Gömülü Parça min/max" ikilisi bu tek net slider ile DEĞİŞTİRİLDİ (editör sadeleşti). Max = ok−1 (en az 1 kapatıcı ok yüzeyde kalmalı), Katman≥2 gerekir. `SpanningArrows`/`BuriedMin`/`BuriedMax` config alanları + `ForCustom` overload'u DURUYOR (generator/preset yolları kullanıyor).
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
- **Çıkış renk efekti (2026-07-22) — palet renginden 2-3 tonlu geçiş:** ok TEMİZ ÇIKARKEN (bounce'ta DEĞİL; tile segmentleri hep beyaz) strip+ok başı, OKUN KENDİ PALET renginden türeyen bir gradient olur (ör. kırmızı ok → kırmızı↔turuncu). Tam gökkuşağı DEĞİL (kullanıcı kararı). `Shaders/ArrowRainbow.shader` (`ArrowRotate/RainbowVertex`, URP unlit, Cull Off, ×_Glow) `_GradientTex`'i DÜZ olarak UV'den örnekler (shader'da zaman YOK).
  - **`FlightRenderer3D.BuildPaletteGradient(baseCol)`** → uçuş başına 256×1 DİKİŞSİZ PALİNDROM texture: base ↔ (hue+`HueShift 0.08`, biraz parlak). Palindrom (0→0.5 base→shifted, 0.5→1 geri) sayesinde wrap Repeat'te uçlar eşit → kusursuz döner; dünya-pozisyonu kaydırmasıyla renk ailesi içinde salınır. HueShift renk ailesi genişliği (0.08 = komşu ton; artır = daha geniş geçiş).
  - Palet rengi `HexaGameplayManager` → `FlightRenderer3D.Create(..., HexaPalette.ForPalette(arrow.Palette))` ile geçer. Materyal+texture UÇUŞ BAŞINA (`_fxMat`/`_fxTex`, `EnableRainbow`'da kurulur, `OnDestroy`'da yok edilir → leak yok). Static paylaşımlı gökkuşağı texture'ı kaldırıldı.
  - ⚠ **`FlightRenderer3D.UseColorGradient` (varsayılan FALSE):** gradient efekti açık/kapalı. Particle-izi denemesi için KAPATILDI (ok beyaz). Palet gradient'ini geri getirmek için `true` yap (ikisi birlikte de çalışır).
  - **COMBO gökkuşağı (2026-08-06):** oklar ARDA ARDA çıkarsa **1.'den sonrakiler TAM GÖKKUŞAĞI** modunda uçar. `HexaGameplayManager.StartFlight`: `_comboCount = (Time.time - _lastExitTime) <= ComboWindow(1.2s) ? +1 : 1` → `_comboCount >= 2` ise `fr.SetComboRainbow(true)`. `_lastExitTime` `OnFlightDone`'da yazılır, `Begin`'de sıfırlanır. `FlightRenderer3D.BuildFullRainbow()` = 256×1 TAM hue taraması (0→1; hue döngüsel olduğu için wrap dikişsiz) — `EnableRainbow`'da palet gradient'i yerine bu kullanılır. Zincir çıkışlar (ChainFirstDelay 0.18 + ChainStep 0.26) pencerenin içinde kalır → doğal combo. Doğrulandı: `comboCount=2`, texture `ComboRainbow` (kırmızı→yeşil→mavi).
- **Bekleme idle'ı (2026-08-06):** tamamlanmış ama önü KAPALI ok, **KENDİ YOLU boyunca** ileri-geri süzülür ("zorlanma" hissi). ⚠ İlk deneme segment transform'larını tek bir dünya vektörüyle kaydırıyordu → ok BLOK halinde kayıyor, büklümlü okun şekli bozuluyordu. Doğrusu: uçuşun **yol-takipli** `SetOffset` mantığını kullanmak. `HexaGameplayManager.StartWaitIdle`: hücre segmentleri gizlenir, `FlightPathBuilder` yoluyla bir `FlightRenderer3D` kurulur ve `IdleLoop(amp, freq)` çağrılır (`amp = 0.22·CellSize`, `freq = 2.6`); `IdleRoutine` `SetOffset(amp·0.5·(1−cos t))` ile 0..amp arası yumuşak gidiş-geliş yapar → ok şekli/dönüşleri korunur. `StopWaitIdle` renderer'ı yok eder + segmentleri geri açar. Başlatma: bounce bitince ok hâlâ `Waiting` ise, ya da aynı engele tekrar denk gelince. Durdurma: `TryLaunch` başında → yol açılınca uçuşa geçer. `_waitIdle` sözlüğü `Begin`'de temizlenir. Doğrulandı: büklümlü ok şeklini koruyarak salındı, yol açılınca `Flying`'e geçip idle düştü.
- **Particle izi — PREFAB yolu (2026-08-06, GÜNCEL):** `BoardView.TailParticleWhite` / `TailParticleRainbow` = `Assets/_Arrow Rotate/Prefabs/Particles/Tail_Particle_White|Rainbow.prefab` (aynı ayarlar, yalnız `startColor` farklı: beyaz vs renkli). Prefab özellikleri: **World sim, loop, playOnAwake, `rateOverTime=60` → OTOMATİK emit**, lifetime 2–3s, speed 3–4.5, size 0.1–0.2, Cone r=0.01, Mesh render (`Confetti2x2`, URP Particles Unlit), colorOverLifetime + sizeOverLifetime + rotationOverLifetime + limitVelocity açık.
  - Kullanım: `HexaGameplayManager.StartFlight` → `fr.SetTailPrefabs(white, rainbow)`; `Fly()` içinde **combo ise Rainbow, değilse White** prefabı Flight3D'ye child olarak instantiate edilir (`TailEmitter_<prefabAdı>`). ⚠ Prefab kendi emit ettiği için **elle `Emit` YAPILMAZ** (`_trail=false`); `SetOffset` her frame emitter'ı okun **KUYRUĞUNA** taşır → iz arkada oluşur. `DetachTrail`: emitter unparent + `Stop(StopEmitting)` + `lifetime+0.3s` sonra Destroy (parçacıklar sönene dek yaşar).
  - Prefab atanmamışsa eski sprite'lı manuel-emit yoluna düşer (aşağıda). Doğrulandı: normal uçuşta beyaz konfeti, combo uçuşunda renkli konfeti izi.
- **Particle izi — eski sprite yolu (2026-07-22, fallback):** ok TEMİZ ÇIKARKEN (bounce'ta DEĞİL) arkasından random aralıkla RANDOM ŞEKİLLİ parçacıklar bırakır, sönerek kaybolur. `FlightRenderer3D.BuildTrail`: **sprite başına bir `ParticleSystem`** (Flight3D child) — farklı texture'lı sprite'lar tek materyalde karışamadığı için ayrı PS; emit'te `_psList`'ten RANDOM biri seçilir → şekiller karışık (Circle/Star/Square). `simulationSpace=World` (bırakılan parçacık yerinde kalır → iz), emisyon OTOMATİK DEĞİL (`rateOverTime=0`) — `SetOffset`'te `_emitTimer`/random `[0.012,0.035]sn` ile **okun KUYRUĞUNDA** (`PointAt(tailL)`→dünya) `Emit(1)`. ⚠ Baş (ön uç) konumunda emit edilirse parçacıklar gövdenin ALTINDA kalıyordu; kuyrukta emit → gövdenin ARKASINDA gerçek iz. Emit konumuna uçuş yönüne DİK yanal savrulma (`TrailLateralSpread=0.3·S`, perp=(-exitDir.y,exitDir.x)). Her parçacık: `startRotation` random 0..2π, `startColor` MinMaxGradient (iki renk arası → random hafif tint+alfa), colorOverLifetime alfa 1→1(0.35)→0 (söner), sizeOverLifetime ×1→0.4, startSize 0.22–0.4·S.
  - **Sprite'lar:** `BoardView.TrailSprites` (serialize; Game.unity'de Circle_1/Star_1/Square_1 wire'lı, guid'ler 27fda25c/361a56f7/7b1edecd). Manager `Board.TrailSprites`'ı `Create(...)`'a geçer. Boşsa fallback kod-içi daire (`UiSprites.Circle`). Materyaller sprite-texture başına önbelleklenir (`TrailMatFor`, static Dictionary — leak yok). ⚠ **BoardView'a yeni serialize alan ekleyince Game.unity'yi `AssetDatabase.ImportAsset(ForceUpdate)` ile reimport et** — yoksa Play cache eski sahneyi yükleyip alanı boş gösterir (yaşandı; reimport sonrası 3 sprite geldi). Materyal `TrailMat` = `Shaders/ArrowTrail.shader` (`ArrowRotate/Trail`, URP unlit transparent, `_MainTex × COLOR`, alpha blend, ZWrite Off) + daire texture = `UI.UiSprites.Circle.texture` (runtime üretilen yumuşak daire; kullanıcının Circle_1'i yerine build-safe/wire'sız). Shader Always Included'a eklendi (guid 82c1d2fa…).
  - ⚠ **Uçuş bitince `DetachTrail`:** PS Flight3D'den AYRILIR, emisyon durur, kalan parçacıklar sönene dek yaşar sonra kendini yok eder (yoksa Flight3D silinince iz aniden kaybolurdu).
  - Güncel ayarlar (kullanıcı tweak'i 2026-07-23): `TrailGapMin/Max`=0.012/0.035 (sık), startLifetime=0.6–1.3 (uzun ömür), startSize=0.22–0.4·S (küçük), `TrailLateralSpread`=0.3·S (hafif serpilme), colorOverLifetime tepe alfa=1.0. Kullanıcının Circle_1'i (guid 27fda25c…) istenirse: TrailMat `_MainTex`'ini ona bağla (runtime asset load için Resources/serialized alan gerekir).
  - ⚠ **UV = DÜNYA POZİSYONU İZDÜŞÜMÜ, ZAMAN YOK (2026-07-22 nihai yöntem):** `WriteRainbowUV`: `uv.x = (worldX·_exitDir.x + worldZ·_exitDir.y)·RainbowWorldFreq(0.09)`, uv.y=0.5. `_exitDir` = uçuş yönü. Renk değişimi TAMAMEN yılanın hareketinden gelir → TEK hareket kaynağı. Neden: zaman-kaydırması + yılan-hareketi = İKİ hareket, hissiyatı bozuyordu (kullanıcı geri bildirimi). Şimdi bantlar dünyada sabit + harekete dik; yılan içlerinden geçtikçe renkler gövde boyunca akar.
  - ⚠ **freq DÜŞÜK olmalı:** renk hızı = flightSpeed(≈25.9 u/s) × freq. 0.09 → ~2.3 tur/sn (iyi). Yüksek freq → hızlı uçuşta STROBE (0.33'te yaşandı). Bu yüzden dünya-pozisyonu yöntemi ancak DÜŞÜK freq ile pürüzsüz.
  - Eski denemeler (kaldırıldı): shader `_Time` kaydırma (KAYMADI) → C# `Time.time` kaydırma (çalıştı ama hareketle çakıştı). Nihai: dünya-pozisyonu, zamansız.
  - ⚠ **Bounce'ta efekt YOK:** `Create` materyali BEYAZ kurar; yalnız `Fly()` → `EnableRainbow()` gökkuşağına çevirir. `Bounce()` çağırmaz → engelli ok beyaz çarpıp döner (doğrulandı: bounce Body shader = URP/Lit).
  - Shader `Always Included Shaders`'a eklendi (GraphicsSettings, guid 883a472c…) → build'de dahil, cihazda `Shader.Find` çalışır.
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
- **Kamera:** `FitCamera` XZ dalı — yukarıdan eğik (varsayılan 55°; CameraPrefab X-tilt'i >20° ise ondan), kadraj/zoom otomatik. Üstüne **elle pinch-zoom + pan** (`CameraPanZoom`, aşağıda).
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

**Kamera pinch-zoom + pan (`Scripts/Input/CameraPanZoom.cs`, 2026-07-23):** büyük level'larda tıklamayı kolaylaştırır. Manager GO'suna runtime iliştirilir (`HexaGameState_Gameplay.StartHexaLevel` + `HexaSandboxDriver.Start`, `Init(cam, board)`). İki parmak = pinch-zoom (parmağa doğru) + iki-parmak pan; **tek parmak = pan** yalnız `DragThresholdPx=18` eşiği geçilirse; editörde fare tekeri zoom + sol-tuş sürükleme pan.
- **YUMUŞAK (2026-07-23):** girdi doğrudan kameraya değil HEDEF (`_tPos`/`_tSize`) durumuna işlenir; gerçek kamera her frame `LateUpdate`'te `SmoothDamp` ile hedefe süzülür (`PanSmoothTime=0.09`, `ZoomSmoothTime=0.10` — inspector'dan ayarlanır; sert sıçrama/clamp-snap yok). ⚠ Pan/zoom/clamp matematiği HEDEF üzerinde çalışır: ekran→düzlem eşlemesi `ScreenToPlaneTarget` ile (kamerayı geçici olarak hedefe alıp geri koyar — frame içinde render yok, güvenli) → yumuşatma gecikmesinden bağımsız doğru grab. Play-mode'da reflection ile LateUpdate pompalanarak easing eğrisi doğrulandı (23→10.35 zoom ~20 frame'de yumuşak).
- **Tap ile ÇAKIŞMAZ (kilit tasarım):** `CameraPanZoom` ve `TapController` AYNI eşiği (`CameraPanZoom.DragThresholdPx`) kullanır → koordinasyon gerekmez: parmak eşikten çok kayarsa TapController tap saymaz (pan olur), az kayarsa CameraPanZoom pan etmez (tap olur). ⚠ Bu yüzden **TapController artık SERBEST BIRAKIŞTA tap sayar** (Began değil), tek-parmak + kısa (<0.4s) + eşikten az kayış + gesture boyunca 2+ parmak görülmemiş şartıyla — pinch başında yanlış dönüş engellenir.
- **Sınırlar:** zoom `[fit·MinZoomFactor(0.4), fit]` (fit = maks uzaklaşma, `BoardView.CameraFitSize`). Pan `ClampPan` ile board bbox içinde; **yakınken** board sınırlarına clamp, **uzakken (fit)** `_home` (fit dinlenme noktası — Init'te fit sonrası yakalanır) etrafında `OverscrollFactor(0.25)·extent` kadar SİMETRİK overscroll → fit'te bile her iki eksende (X ve Y/derinlik) pan çalışır (2026-07-23 kullanıcı isteği). ⚠ Fit'te ekran-merkez board merkezinde DEĞİL (XZ tilt kayması) → overscroll board merkezi değil `_home` etrafında olmalı, yoksa Y tek yöne çalışır. Kadraj bilgisi `BoardView.CameraFocusCenter/Extents/FitSize` (FitCamera doldurur). Play-mode'da reflection ile 4-yön pan + zoom + clamp doğrulandı.
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
- ⚠ **Tap çift-tetikleme (2026-07-22, BUILD'de yaşandı, editörde görünmez):** cihazda dokunuş `Input.simulateMouseWithTouches` (varsayılan açık) yüzünden HEM `Input.GetMouseButtonDown(0)` HEM `Input.GetTouch(Began)` tetikler → tek dokunuş 2× Tap → 120° dönüş. Editörde touch olmadığından yalnız mouse → 60°, bu yüzden editörde YAKALANMAZ. Fix: `TapController.Update` touch VARSA mouse dalını ATLAR (`if (touchCount>0) {touch} else if (mouse)`) — bu ayrım release-tabanlı tap'e geçince de KORUNDU (2026-07-23). ⚠ Input'u mouse+touch birlikte okuyan her yerde bu tuzağa dikkat.
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

### Bottom Nav Bar — `Modules/Bottom Nav Bar/` (2026-07-29 yeniden yazıldı)

Ana menü alt navigasyon çubuğu. hocus3'ten kopyalanan hali 3 tab'ı hard-code ediyor ve **bu projede olmayan** `DailyChallengeManager`/`DailyChallengePanel`'e baktığı için derlemeyi bozuyordu → sıfırdan yazıldı, Daily Challenge bağı tamamen çıktı (eski `BottomNavBar`/`BottomNavTab`/`BottomNavBarEvents` **SİLİNDİ**).

**Yapı — İKİ COMPONENT, hepsi bu (2026-07-29, kullanıcı kararı: "navbarbutton için tek component, navbar için tek component, visibility'yi de navbar içinde topla"):**

| Dosya | Nerede | Sorumluluk |
|---|---|---|
| `Runtime/NavBar.cs` | bar kökü | hangi buton seçili (görünürlük DEĞİL) |
| `Runtime/NavBarButton.cs` | buton başına | görsel **+ tap ne yapar** + seviye kilidi + **tap feedback'i** |

- **Yeni tab = kod DEĞİL:** buton GameObject'ini kopyala, `Mode`'unu seç. `NavBarButton.Mode`: `SelectOnly` (Home — sadece seçilir, öncekinin panelini kapatır) · `OpenPanel` (`Panel`'i açar, `Close Panel On Deselect` ile kapatır) · `Placeholder` (daima kilitli görünür, tap yalnız mesaj gösterir, seçim değişmez).
- **Adresleme INDEX bazlı (2026-07-29, kullanıcı kararı):** buton id'si YOK — `NavBarButton.Index` = `transform.GetSiblingIndex()`. API `NavBar.Select(int index)` / `GetButton(index)`; **default buton `NavBar._defaultButton` = doğrudan `NavBarButton` referansı** (2026-07-29 kullanıcı isteği: index yerine buton; `_isDefault` buton-içi flag'i yok). Bar'a ait olmayan bir referans başlangıçta uyarıyla düşürülür → ilk kullanılabilir butona fallback. Referans olduğu için satırı yeniden sıralamak onu bozmaz. ⚠ Child index olduğu için satırı yeniden sıralamak index'leri kaydırır — `_defaultIndex` ve `Select(n)` çağrılarını buna göre güncelle. `Buttons` düz `List<NavBarButton>` (IReadOnlyList değil). UnityEvent kancaları (`On Selected`/`On Deselected`) ve `_startHidden` da KALDIRILDI.
- **Arka plan rengi DİNAMİK (2026-07-29, kullanıcı isteği "seçilenin rengi farklı, background dinamik değişsin"):** `_background` (Graphic) + `_selectedColor` (0.753 gri) / `_unselectedColor` (beyaz) → `SetEmphasis` içinde **aynı weight** ile `Color.Lerp` (clamped, `OutBack` taşması rengi aralık dışına çıkarmaz). Prefab'da `Background` Image'ına bağlı; Home instance'ındaki elle konmuş 3 `m_Color` override'ı SİLİNDİ (artık component sürüyor, override sadece editör görünümünü yanıltırdı). ⚠ İkon/etiket tint'i hâlâ YOK — component'in renklendirdiği tek şey bu background.
- **RENK DEĞİŞTİRME ve SELECTION PILL YOK (2026-07-29, kullanıcı kararı "renk değiştirmeyi istemiyorum, ikonlarla halledeceğim"):** `_contentColor`/`_lockedContentColor`/`_selectionPill` alanları ve `Refresh()`'teki tint + pill toggle **SİLİNDİ** (pill GameObject'i de buton prefab'ından kalktı, bar prefab'ındaki 12 geçersiz renk override'ı temizlendi). Kilitli/açık ayrımı **yalnız ikon sprite'ı** ile: `_lockedIconSprite`/`_unlockedIconSprite`. ⚠ Sonuç: component artık SEÇİLİ durumu hiçbir şekilde göstermiyor — seçili görünüm gerekiyorsa prefab'da kurulup `NavBar.SelectionChanged`'dan sürülmeli ya da butona selected-sprite alanı eklenmeli.
- ⚠ **Buton ayrı prefab'a çıktı (kullanıcı):** `Prefab/Navbar Button.prefab` (`Background`/`Inline`/`Image`/`Image (1)` kullanıcının art'ı); `BottomNavBar.prefab` bunun 3 nested instance'ını tutar. Bar prefab'ında butonların RectTransform'ları `stripped` görünür — normaldir.
- **Seçili vurgusu (2026-07-29, kullanıcı: "komple navbar button büyüsün, seçili biraz yukarı uzasın, diğerleri küçük olsun"):** seçili buton **bir bütün olarak** büyür; üç parça TEK 0..1 tween'inden sürülür (taşan ease'te bile ayrışmazlar):
  - **Genişlik** = `_layoutElement.preferredWidth = _resolvedBaseWidth × factor` → layout satırı yeniden böler, komşular **yer açar**; butonun rect'ine stretch olan art (`Background`/`Inline`/`Image`ler) bedavaya takip eder.
  - **Yükseklik** = kendi `sizeDelta.y = _baseHeight + _selectedRise × weight` (satır height'ı kontrol etmiyor, bu eksen bizim) → **yukarı** uzar. Değişimden sonra `LayoutRebuilder.MarkLayoutForRebuild(row)` çağrılır: çocuğun kendi boyut değişimi grubu kirletmez, rebuild olmazsa yukarı değil aşağı uzardı.
  - **İkon = RECT'ten sürülür** (scale değil): `sizeDelta` authored (dinlenme, prefab'da 128×128) → `_iconSelectedSize` (160), `anchoredPosition.y` += `_iconRise × weight` (16). Mutlak hedef + bulanık upscale yok; aynı weight ikonu kaldırdığı için label ile arasındaki mesafe açılır. Seçim değişince ikon küçülüp aşağı iner, label kapanır. ⚠ `_visual` alanı BOŞ bırakılmalı (prefab'da 0) — dolu olursa ikon iki kez büyür.
  - ⚠ İkonun anchor'ı tüm butonlarda aynı olmalı (prefab'da orta, `anchorY 0.5`, y=+32): üst-hizalı ikon buton uzarken 40, orta-hizalı 20 yükselir → karışık anchor'lar butonları farklı davrandırır. 2026-07-29'da 3. instance'ta override yoktu (160×160 üst-hizalı kalmış, elle yapılmış "seçili" görünüm) → diğer ikisiyle aynı hale getirildi.
  - **Label görünürlüğü = `_selected || IsLevelLocked`** (2026-07-29): seçili buton etiketli, diğerleri ikon-only, AMA **seviye-kilitli** buton etiketini gizlemez — `Level 20` şartı okunur kalmalı. `Placeholder` modundaki kilitli buton (seviye kilidi yok) ikon-only kalır.
  - **Kilitli buton ASLA büyümez:** hedef `_selected && !IsLocked ? 1 : 0` (tıklama gate'i `OnClicked`'ta zaten var, bu programatik seçime karşı ikinci kilit).
  - `factor` = `Lerp(_restScale 0.9, _selectedScale 1.1, weight)` → seçili olmayanlar gözle görülür şekilde küçük. `_baseWidth: 0` → layout'un başta verdiği genişlik taban. `_selectedRise: 40` (0 = kapalı). `ResetEmphasis` deaktifleşmede yarı büyümüş buton bırakmaz.
- ⚠ **Satırın ayarları bu davranışın parçası:** `Child Control Width` AÇIK (yoksa `preferredWidth` yok sayılır) · `Child Control Height` KAPALI (yoksa `sizeDelta.y`'yi grup ezer) · **`Child Alignment` = `LowerCenter`** — 2026-07-29'da `UpperCenter`'dan çevrildi, çünkü Upper hizalamada fazla yükseklik AŞAĞI taşıyordu. `Child Force Expand Width` açıkken artan boşluk eşit bölünmeye devam eder → genişlik farkının ~2/3'ü seçiliye yansır; 1:1 akordeon isteniyorsa force-expand kapatılmalı. Emphasis tween'i çalıştığı her frame satırı rebuild eder (3 buton için önemsiz).
- ⚠ **Buton köküne elle `localScale` verilmemeli:** layout scale'i saymadığı için o buton kalıcı olarak komşularının üstüne biner ve satır bozuk görünür (2026-07-29'da bar prefab'ındaki bir instance'ta 1.1 override'ı bulunup silindi; `Outline (1)`'in 1.1'i kullanıcı art'ı, ona dokunulmadı). Boyutu emphasis sürer.
- ⚠ Büyüyen buton bar'ın rect'inin DIŞINA taşar (bilinçli: "bar'ın üstüne çıkma" görünümü). Bar'a `Mask`/`RectMask2D` eklenirse kırpılır.
- ⚠ Buton prefab'ının köküne **`LayoutElement` eklendi** (varsayılanların hepsi -1; genişliği runtime'da `NavBarButton` sürer).
- **Seviye kilidi butonun içinde:** `Unlock At Level` (0 = kilit yok) + `GameData`. Kilitliyken **locked state** açılır, tap'te `Unlocks at Level {0}!` süzülür. Kilitli butonun Button'ı **bilerek aktif** kalır (mesaj çıkabilsin).
- **Locked/unlocked İKİ AYRI OBJE (2026-07-29, kullanıcı isteği):** buton içinde iki karşılıklı-dışlayan container var — `Unlocked` (Icon+Bullet, Label) ve `Locked` (Icon, Label). Component yalnız hangisinin aktif olacağına karar verir (`_unlockedState`/`_lockedState`); **sprite takası ve tint YOK** (`_lockedIconSprite`/`_unlockedIconSprite` alanları SİLİNDİ) → iki görünüm bağımsız art-direct edilir. `_icon`/`_label` = unlocked'ın ikonu/etiketi (emphasis o ikonu büyütür, o etiket yalnız seçilide görünür), `_lockedLabel` = kilit şartı metnini (`Level {0}`) alan etiket. Locked state bağlanmamışsa unlocked etiketi şartı taşır (geriye dönük davranış). Paylaşılan chrome (`Background`/`Inline`/`Image`ler/`Feedback`) kökte kalır.
- ⚠ Icon/Label'ı `Unlocked` altına taşımak bar prefab'ındaki per-instance override'ları BOZMAZ: override'lar path değil **fileID** hedefler (128×128 ikon override'ları taşıma sonrası doğrulandı).
- **Butonu tamamen gizlemek** = GameObject'i deaktif et (layout group'tan da çıkar, kalanlar ortalanır). `Badge` property'si bildirim noktasını açar/kapar.
- **Görünürlük mekanizması YOK (2026-07-29, kullanıcı kararı "navbar zaten main menu panelin içerisinde"):** bar ana menü panelinin ALTINDA yaşar, parent kapanınca kendiliğinden kapanır. `_followMainMenu`/`_visualRoot`/`Show()`/`Hide()`/`IsVisible` ve `MainMenuOpenedEvent`/`MainMenuClosedEvent` **SİLİNDİ** — host'un raise edeceği bir şey kalmadı. Her yeniden aktifleşmede `OnEnable` → `Refresh()` + (`_selectDefaultOnEnable` ise) default butona dönüş; "her menü ziyareti Home'da başlar" davranışı bundan gelir. Panel dışarıdan kapanırsa (kendi X'i) seçim default butona döner.
- **Feedback LOCAL (2026-07-29, kullanıcı kararı "event'lerden komple kopart"):** kilitli/placeholder butona tıklanınca mesaj **butonun kendi `_feedbackText`'inde** (TMP child, buton üstünde, başlangıçta inaktif) DOTween ile fade-in + yukarı süzülme + fade-out yapar (`_feedbackRise` 70, `_feedbackDuration` 1s; `DOTween.Sequence`, `OnComplete`'te SetActive(false)). Tekrar tıklama animasyonu baştan başlatır (`Kill` + reset), buton deaktif olursa `ResetFeedback` yarı yolda kalmış mesajı toplar. `_feedbackText` atanmamışsa `LogWarning`.
- ⚠ **EventBus / `IEvent` kullanımı SIFIR:** `NavBarEvents.cs` (`NavFeedbackRequestedEvent`) ve `NavBar.ShowFeedback`/`RaiseFeedback` **SİLİNDİ**; modülde `GameBrain.Utils` referansı yok. Modül artık host'tan hiçbir abonelik beklemiyor (öncesinde abone olmadığı için mesajlar görünmüyordu — "feedback göremiyorum" sorunu bu kararla kökten çözüldü). Bağımlılık: DOTween (`DG.Tweening`).
- ⚠ **Feedback'i GÖSTEREN kimse YOK (2026-07-29):** kilitli/placeholder butona tıklayınca mesaj üretilir ama ekranda hiçbir şey görünmez — projede bu event'e abone tek satır yok, genel toast sistemi de yok (`StoreMessagePopup` store'a özel: başlık "Purchase Failed", `PurchaseFailureReason` alır). "Feedback göremiyorum" olarak yaşandı. Teşhis için `NavBar.RaiseFeedback` **editörde** her isteği log'lar (`[NavBar] Feedback requested: "…"`); `EventBus<T>` abone sayısı vermediği için modül mesajın çizilip çizilmediğini anlayamaz. Kullanıcı kararı: **şimdilik sadece log** — görsel gösterim istendiğinde bir abone (toast) yazılacak.
- ⚠ Butonlar kendi `Awake`'lerinde değil `NavBar.Awake` → `Bind()` ile bağlanır (gizli butonun `Awake`'i çalışmaz ama açıldığı an tam bağlı olmalı).
- **Banner/reklam etkileşimi YOK** (kullanıcı kararı): `GameBrain.SDK.*` referansı sıfır, bar banner'a göre kıpırdamaz. Gerekirse modül dışında ayrı bir layout component'i olarak yapılır.
- Namespace `GameBrain.Casual` (runtime) / `GameBrain.Navigation.EditorTools` (editör) — base konvansiyonu. Klasör: `Runtime/` + `Editor/` + `Prefab/` + `README.md` (3 dosya için alt klasör kırmak gereksizdi).
- `Prefab/BottomNavBar.prefab` iki component'e bağlandı, art wiring korundu (Challenge ikonları/renkleri, unlock 20 + GameData, 288 yükseklik). Editör: **GameBrain → Navigation → Build Bottom Nav Bar** + **Add Button To Selected Bar**.
- **Kalan entegrasyon:** prefab henüz hiçbir sahnede DEĞİL — GUI sahnesindeki ana menü panelinin altına yerleştirilmeli (görünürlük parent'tan gelir, kod gerekmez).
- ⚠ **2026-07-29 olayı:** oturum sırasında dışarıdan bir işlem (untracked klasörleri süpüren git/başka oturum) bu modül klasörünü diskten sildi. Modül **untracked** olduğu için git koruması yok — commit'lenene kadar kaybolabilir. Birebir kopyaları `Gamebrain/Assets/...`, `hocus 3d/...`, `gamebrainBaseProject/...` altında duruyor.

### Level Progression — `Modules/Level Progression/` (2026-07-29 yeni)

Ana menüdeki level yolu: oyuncunun bulunduğu level + ilerideki level'lar, dikey bir çizgi üzerinde; **mevcut en ALTTA**, gelecek yukarı doğru. Mockup referansı: daire içinde level numarası, mevcut olanda halka, zor level'da "Hard" etiketi.

**Yapı — İKİ COMPONENT** (navbar ile aynı desen, namespace `GameBrain.Casual` / editör `GameBrain.LevelProgression.EditorTools`):

| Dosya | Nerede | Sorumluluk |
|---|---|---|
| `Runtime/LevelPath.cs` | yol kökü | node'ların hangi numarayı göstereceği |
| `Runtime/LevelPathNode.cs` | node başına | kendi görünümü: current/upcoming state, numara, hard etiketi |

- **Sabit pencere (kullanıcı kararı):** `_nodeCount` (3) kadar level, mevcut seviyeden başlayıp yukarı sayar (level 10 → 10, 11, 12). Kaydırma/ScrollRect YOK.
- ⚠ **Child sırası AŞAĞI doğru okunur:** ilk child en uzak level, **son child mevcut level**. Böylece düz bir `VerticalLayoutGroup` + `Child Alignment: Lower Center` mockup'taki yerleşimi verir, `reverseArrangement` bayrağı gerekmez.
- Node'lar `Awake`'de bir kez kurulur, sonra yalnız yeniden etiketlenir → menü açılışı allocation yapmaz. `Refresh()` **`OnEnable`'da** çalışır; panel her menü ziyaretinde yeniden aktifleştiği için ilerleme kendiliğinden güncel kalır.
- **Elle kurulmuş node'lar adopte edilir:** `Nodes Root` altındaki mevcut `LevelPathNode`'lar klonlanmaz, olduğu gibi kullanılır (yol tamamen elle tasarlanabilir). `_nodePrefab` yalnız eksiği tamamlar; `_nodeCount`'tan fazlası yok edilmez, kapatılır.
- **Zorluk `GameConfig`'ten:** `GameConfig.Levels[level-1].Difficulty is Hard` → node'un `Hard Tag`'i. Dizinin sonunu aşan level'lar (random level loop) `false` döner, tahmin yapılmaz; `GameConfig` boşsa etiket hiç çıkmaz. (`LevelManager.IsHardLevel` yalnız MEVCUT level'ı yanıtladığı için kullanılmadı.)
- **Node = iki state objesi** (navbar butonundaki desen): `Current State` / `Upcoming State` bağımsız art-direct edilir, component yalnız hangisinin aktif olduğuna ve numaralara karar verir. Çizgi/daire/halka/pill tamamen art — kod hiçbirine dokunmaz, tint yapmaz.
- **Mevcut node'un ring'i PULSE atar (2026-07-29):** `LevelPathNode._ring` DOTween ile `_pulseScale` (1.08) hedefine `_pulseDuration` (0.8s) `InOutSine` + `SetLoops(-1, LoopType.Yoyo)`. Çarpan **authored scale** üzerine (ring 1 değilse de çalışır); `Setup` her refresh'te çağrıldığı için **çalışan pulse yeniden başlatılmaz** (`IsActive()` kontrolü); node current olmaktan çıkınca/deaktifleşince tween kill + scale eski haline döner. ⚠ `Application.isPlaying` değilse hiç tween kurulmaz — editörde DOTween'i süren player loop yok ve yarı uygulanmış scale sahneye/prefab'a kaydolurdu (`LevelPath`'in "Preview In Editor" menüsü bu yüzden güvenli).
- **Node'lar dekoratif** (kullanıcı kararı): tıklanmaz, oynamak için `HomePanel`'in Play butonu var. İlerleme animasyonu da YOK (kullanıcı: "şimdilik statik").
- Editör: **GameBrain → Level Path → Build Level Path** — hiyerarşiyi placeholder art'la üretir, sahnedeki `HomePanel`'in (ya da seçili RectTransform'un) altına koyar, `GameData` + `GameConfig`'i bağlar. Prefab dosyası YOK; builder üretir, sonra istersen prefab'a çıkarırsın.
- `LevelPath` inspector'ında sağ tık → **Preview In Editor** (play mode'a girmeden kayıtlı seviyeye göre etiketler).
- **Çizgi kod-sürümlü (2026-07-29 düzeltme):** `LevelPath._line` alt node'un merkezinden üst node'un merkezine gerilir (+`_lineOvershoot`), ölçüm `LayoutRebuilder.ForceRebuildLayoutImmediate` sonrası dünya koordinatından yapılır. ⚠ Sebep: node kolonu `count×nodeH + (count-1)×spacing` ile büyür (8 node → 2600px) ama path rect'i sabit (~1152px) — çizgiyi rect'e stretch etmek `_nodeCount` artınca kolonun sadece %44'ünü kaplıyordu ("ilk 2 node arasındaki çizgi denk gelmiyor" olarak yaşandı). Genişlik/renk hâlâ art'tan.
- ⚠ `Nodes` satırının layout group'unda `Child Control Width/Height` **kapalı** olmalı — node'lar kendi boyutlarını taşır.
- İlgili mevcut yapı: `Main Panel.prefab` → `Content` → `Home Panel` [`HomePanel`] (Play, Settings, level text) ve `MainMenuPanel._hardLevelTag`. Yeni yol bunlardan bağımsız çalışır; `HomePanel._levelText`/`_hardLevelTag` isteğe göre kaldırılabilir.

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