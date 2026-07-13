# Gamebrain — Project Reference

Unity casual puzzle game using a state machine core, event bus communication, and ScriptableObject-driven data.

---

## Team

Bu proje 2 developer tarafından geliştirilmektedir. Her iki developer da ayrı Claude Code hesapları kullanmaktadır ancak aynı ekipte ve aynı codebase üzerinde çalışmaktadır.

- Tüm kararlar bu dosyada kayıt altına alınır; her iki Claude instance'ı da bu kuralları takip eder.
- Bir convention değiştirildiğinde önce bu dosya güncellenir, sonra koda yansıtılır.
- Çakışmaları önlemek için sistemler arasında sahiplik ayrımı yapılır — bir sistem bir developer tarafından geliştirilirken diğeri o sisteme dokunmaz.
- Kod stili, mimari kararlar ve isimlendirme her iki taraf için bağlayıcıdır; kişisel tercih geçerli değildir.

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

### Gamebrain bağlantısı (dokunulan yerler)

- **Boot.unity**: "Game Manager" objesindeki component `HexaGameManager : GameManager` ile değiştirildi (inspector'daki data referansları korunarak). `_gameConfig` → `Assets/_Arrow Rotate/Data/Hexa Game Config.asset`. Level sonu +25 coin burada verilir (`_coinRewardPerLevel` alanı).
- **Game.unity**: yalnız "Hexa Arrows" GO (BoardView + HexaGameplayManager + TapController). Example kalıntıları (trigger'lar, kamera, ışık) silindi — kamera Boot'taki `CameraManager.GameplayCamera`.
- **GUI.unity**: "Hexa HUD" GO (`HexaHudPanel`) — UI'ı runtime kurar, yalnız EventBus dinler.
- `HexaGameState_Gameplay : GameState_Gameplay` gameplay state'ini değiştirir (Example deseni); `HexaLevelData : LevelData` seed+zorluk taşır, Scene alanı boş (Level.Load sahne yüklemez).
- Build sırası: Boot → Game → GUI. `Assets/_Arrow Rotate/Scene/HexaSandbox.unity` = Gamebrain'siz izole test sahnesi (`HexaSandboxDriver`: TapCell/SolveArrow/SolveAll).

### Level içeriği ve formatı

**Format (2026-07-13'te değişti): leveller asset'te TAM HÜCRE DİZİSİ olarak saklanır** (arrowJam deseni) — seed alanı kalktı, elle düzenlenebilir:
- `HexaLevelData`: `Radius` + `HexaCellSave[]` (q, r, arrowId, type, a, b, **rot**) + `HexaArrowSave[]` (palette, freezeAt). `ToHexaLevel()` / `FromHexaLevel()` dönüşümleri.
- **Cells dizisi invariant'ı:** ok sırasında ve her ok içinde kuyruk→head sıralı. Ardışık hücreler axial komşu.
- **Rot'lar asset'te saklanır** — editördeki başlangıç dizilimi oyundakiyle birebir aynı; runtime scramble YOK.
- `Data/Levels/HexaLevel_001..050.asset` → `Hexa Game Config._levels`. **Seed Browser** toplu tarama/ayıklama için durur; bake artık hücrelere yazar.

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

**Butonlar:** *Paletleri Ata* = deterministik çizge boyaması (komşu oklara farklı palet, en az kullanılan tercih; çözemezse uyarı loglar). *Scramble* = tüm rot'lar rastgele + ok başına "bağlı kalmaz" garantisi (head rot bump, üreticiyle aynı algoritma). *Çöz* = tüm rot=0 (çözümü görsel kontrol için; kaydetmeden önce Scramble'la). *Random Fill* = zorluk satırı (1/2/3-4/5+) + seed → `LevelGenerator` çıktısını level'a yazar (scramble+buz dahil; aynı seed aynı level).

**Doğrulama (panel altında canlı, tüm kontroller):** ① çakışma (aynı hücre iki kez) ② bölge dışı hücre ③ geçersiz arrowId ④ ok yapısı (≥2 hücre, ilk Tail/son Head/ara Mid) ⑤ bitişiklik (ardışık hücreler komşu) ⑥ çözülebilirlik (rot=0 kopyada her ok Trace bağlı) ⑦ başlangıç karışıklığı (mevcut rot'larla bağlı ok = ihlal) ⑧ palet komşuluğu ⑨ buz+DAG deadlock (`ExitSimulator`, engelleme grafiği çözülmüş halden `BuildBlockedBy` ile). Yapı bozuksa (①-⑤) mantık kontrolleri atlanır.

**GameConfig üyeliği:** listedeki checkbox `Hexa Game Config._levels`'a ekler/çıkarır (SerializedObject ile; sıra = listedeki mevcut sıra + sona ekleme). Level silinirken config'ten de düşülür.

**Bilinen sınırlamalar:** yarıçap küçültülünce dışarıda kalan hücreler otomatik silinmez (doğrulama ② işaretler); tip (Tail/Mid/Head) denetçiden değiştirilemez (zincir invariant'ını korumak için — gerekiyorsa oku silip yeniden çiz); tek hücre taşıma/silme bilinçli olarak yok.

### Değişmez kurallar

- `arrowId` (mantık kimliği) ≠ `palette` (görsel renk). TÜM mantık arrowId üzerinden. Aynı paletteki iki ok hiçbir hücrede komşu olamaz (test 6 bekçidir).
- **Y-ekseni sözleşmesi** (`HexMetrics` + `HexMathTests` ile sabit — DEĞİŞTİRME): Unity açısı = −(30+60d)°, hücre y'si negatiflenir, tap = z'de −60° (ekranda saat yönü).
- Mid segment görseli DAİMA merkezden kırık çizgi; uçuş polyline'ı da merkezlerden geçer.
- Uçuşa başlayan okun hücreleri `level.Cells`'ten ANINDA silinir (uçan ok engel sayılmaz).
- Hücre erişimi `Dictionary<(int q,int r), Cell>`; string key yasak.
- Level asset'lerinde hücre dizisi elle DEĞİL Level Editor ile düzenlenir; tek hücre silmek/taşımak zincir invariant'ını bozar (editör bu yüzden ok bazında siler/taşır).
- Zamanlamalar prototip birebir (HexaGameplayManager/SegmentView sabitleri): dönüş 160ms, kontrol 170ms, fırlatma 240ms, bounce 560ms, zincir 180+260ms, uçuş 16.18·S/sn. Değiştirmeden önce bu dosyaya yaz.
- MeshRenderer renkleri MPB ile ve **linear dönüşümlü** set edilir (`MeshFactory.SetColor`) — proje Linear color space.
- Framework koduna (`Assets/Gamebrain/`) dokunulmaz; genişletme yalnız subclass ile.

**Oyun event'leri** (`Scripts/Integration/Events/HexaEvents.cs`): `HexaLevelStartedEvent` (çip verisi `HexaArrowChipInfo[]` taşır), `HexaRotateEvent`, `HexaArrowConnectedEvent`, `HexaArrowExitedEvent`, `HexaArrowBlockedEvent`, `HexaIceBrokenEvent`, `HexaLevelWonEvent`, `HexaTutorialEvent`. Ses/haptik yalnız `FxRequestEvent` (dönüş=Drag, çıkış=RocketLaunch, engel=InvalidDrop, buz=Ice_1; klipler Feedbacks_SO'da henüz atanmadı).

### Bilinen notlar / tuzaklar

- **Prototipten bilinçli sapma (2026-07-13):** taş saydamlık akışı ters çevrildi. Prototipte taşlar bounce SONRASI kalıcı görünmez olur (`pulseWaiting`/`.tile.clear`); bizde ok BAĞLANINCA taşlar saydamlaşır (`TileView.FadeOut`), çarpıp geri dönerse bounce bitiminde GERİ AÇILIR (`FadeIn`); zincirleme yeniden fırlatmada tekrar saydamlaşır. `TileView.Vanish` mevcut alfadan başlar (saydam taş uçuşta geri parlamaz).
- Tutorial yalnız level index 0'da tetiklenir (`HexaGameState_Gameplay`); kayıt sıfırlamak için `~/Library/Application Support/DefaultCompany/Arrow Rot/Game Data.json` silinir.
- İlk N level'da ana menü atlanır — template özelliği (`GameData.InstantStartLevelWithoutMainMenu`), bug değil.
- TMP varsayılan fontunda ❄/✓ glifleri yok — HUD şimdilik sayı/soluk çip kullanıyor; art pass'te sprite ikon.
- Win panelindeki sayılar (level/ödül) template placeholder'ı — Faz 8'de bağlanacak.
- Editör odak dışıyken play modu kendiliğinden pause olabiliyor (MCP/arka plan testinde görüldü); test otomasyonunda `EditorApplication.isPaused=false` watchdog'u kullanılıyor, runtime'ı etkilemez.
- MCP ile script yazımından sonra Unity bazen import etmiyor — `AssetDatabase.ImportAsset(..., ImportRecursive|ForceUpdate)` ile zorla.

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