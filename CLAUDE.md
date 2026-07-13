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

Hexagon döndürme bulmacası. **Sürükleme yok** — tek etkileşim taşa dokunmak (60° saat yönü dönüş). Renkli ok segmentlerini (tail/mid/head) kuyruktan uca bağla; bağlanan ok otomatik uçar, önü tıkalıysa çarpıp bekler. Buz mekaniği: 3 ok buzlu başlar, toplam çıkış sayısı eşiğe (1/2/3) ulaşınca kırılır. Tüm oklar çıkınca level biter.

**Bağlayıcı kaynaklar (bu sırayla):**
1. `.claude/skills/hexa-arrows-unity/SKILL.md` — port spesifikasyonu (veri modeli, algoritmalar, zorluk tablosu, görsel oranlar, zorunlu testler)
2. `.claude/skills/hexa-arrows-unity/reference/hexa-arrows-prototype.html` — davranışta nihai kaynak (source of truth)
3. `PLAN.md` — faz planı ve yürütme kararları

**Değişmez kurallar:**
- `arrowId` (mantık kimliği) ≠ `palette` (görsel renk). TÜM mantık arrowId üzerinden; palette yalnız boyama. Aynı paletteki iki ok hiçbir hücrede komşu olamaz.
- Mantık katmanı (`Scripts/Core`, `Scripts/Logic`, `Scripts/Generation`) MonoBehaviour'suz saf C#; Unity API kullanmaz, EditMode testlidir. SKILL.md §10'daki 6 test sınıfı her zaman yeşil kalmalı.
- Mid segment görseli DAİMA merkezden kırık çizgi (kenar ortalarını düz bağlamak yasak).
- Uçuşa başlayan okun hücreleri veri katmanından ANINDA silinir (uçan ok engel sayılmaz).
- Hücre erişimi `Dictionary<(int q,int r), Cell>`; string key yasak.
- Level = seed + config (`HexaLevelData : LevelData`); grid runtime'da deterministik üretilir. Hata raporlarında seed loglanır.

**Oyun kodu yerleşimi:** her şey `Assets/_Arrow Rotate/` altında; Gamebrain'e yalnızca `Scripts/Integration/` içinden subclass ile bağlanılır (`HexaGameManager : GameManager`, `HexaGameState_Gameplay : GameState_Gameplay`, `HexaLevelData : LevelData`). Framework koduna dokunulmaz.

**Oyun event'leri** (`Scripts/Integration/Events/`): `HexaLevelStartedEvent`, `HexaRotateEvent`, `HexaArrowConnectedEvent`, `HexaArrowExitedEvent`, `HexaArrowBlockedEvent`, `HexaIceBrokenEvent`, `HexaLevelWonEvent`, `HexaTutorialEvent`. Ses/haptik yalnız `FxRequestEvent` ile.

**Sahiplik ayrımı:** Dev A = Core/Logic/Generation/testler/Editor tooling/level içeriği · Dev B = Board/Input/Animation/GUI/Gameplay/Integration + sahne-prefab sahipliği. `Cell`/`Arrow` modeli ve event imzaları ortak sözleşmedir — değişiklik önce bu dosyaya yazılır.

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