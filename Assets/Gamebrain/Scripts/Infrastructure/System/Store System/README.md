# Store System (`GameBrain.Store`)

A SOLID, modular, generic alternative to the legacy `Shop System` (`GameBrain.Casual`).
The two live side by side; this one does not touch or depend on the legacy shop. It is **not wired
into the game** — it is a library you instantiate when needed. Correctness when instantiated is proven
by `ShopVerification` (see *Verifying* below).

## Why a separate namespace

The legacy shop already defines `CurrencyType`, `ShopItemData`, etc. in `GameBrain.Casual`. This system
uses `GameBrain.Store` so the names never collide. Where a Store file needs a Casual type
(`CurrencyManager`, `GameData`, `BoosterGameData`, `BoosterType`) it either uses `using GameBrain.Casual;`
(when it does not reference `CurrencyType`) or fully qualifies the type (`GameBrain.Casual.CurrencyManager`).

## Layers

```
Core/        Contracts + orchestrator (no Unity-specific logic in the flow)
  IShopItem / IShopReward / IShopCatalog / IWallet / IPaymentProcessor / IAsyncPaymentProcessor /
  IRewardGranter / IPurchaseValidator / IPurchaseHistory / IShopLogger / IShopService
  ShopItem, ShopCatalog, ShopService          <- the engine
Wallet/      InMemoryWallet (tests/demo) · CurrencyManagerWallet (production adapter)
Payment/     WalletPaymentProcessor (Coin/Gem) · RealMoneyPayment (async IAP seam + stub) ·
             UgsIapService (Unity Gaming Services IAP)
Rewards/     CurrencyReward(+granter) · BoosterReward(+granter)
Validators/  MaxPurchase · Ownership · DelegateRequirement (closure-based, generic)
History/     InMemoryPurchaseHistory
Events/      ShopPurchaseRequested / Succeeded / Failed (EventBus, IEvent)
Authoring/   ShopItemDefinition · ShopCatalogDefinition (ScriptableObjects)
Composition/ ShopServiceBuilder (fluent wiring) · StoreInstaller (live service for a scene)
GUI/         StorePanel · StoreItemView · StoreBundleView · StoreRewardEntryView ·
             CountdownView · StoreOfferPopup · StoreMessagePopup · StoreRewardIconSet
Demo/        ShopVerification (scenario asserts) · ShopDemoBootstrap (MonoBehaviour runner)
```

## Purchase flow (`ShopService.Purchase`)

1. **Lookup** in the catalog → `ItemNotFound`.
2. **Validate** — ordered `IPurchaseValidator` chain; first block wins.
3. **Capability check** — a payment processor for the currency *and* a granter for every reward must
   exist, otherwise `NoPaymentProcessor` / `NoRewardHandler`. This guarantees we never charge without
   being able to deliver.
4. **Charge** via the processor (`InsufficientFunds` / `PaymentFailed`). If the processor is an
   `IAsyncPaymentProcessor` (IAP), `Purchase` returns `PurchaseOutcome.AsPending` and the charge resolves
   later through a callback.
5. **Grant** every reward through its registered granter.
6. **Record** to history and **publish** `ShopPurchaseSucceededEvent` / `ShopPurchaseFailedEvent`.

Steps 5–6 live in `ShopService.Fulfill`, called immediately for synchronous purchases and from the IAP
callback for async ones. Failure reasons are a machine-readable enum (`PurchaseFailureReason`); the core
produces **no UI strings**.

## Wiring it (production)

```csharp
IShopCatalog catalog = catalogDefinition.BuildCatalog();          // from a ShopCatalogDefinition asset

IShopService shop = new ShopServiceBuilder(catalog)
    .WithWallet(new CurrencyManagerWallet(currencyManager, gameData))
    .WithBoosters(boosterGameData)                                // enables BoosterReward granting
    .WithLogger(new UnityShopLogger())
    // .WithIap(new MyUnityIapService())                          // enables RealMoney items
    // .AddValidator(new DelegateRequirementValidator(i => playerLevel >= 5))
    .Build();

// UI raises this; the service is subscribed via EventBus:
EventBus<ShopPurchaseRequestedEvent>.Raise(new ShopPurchaseRequestedEvent("coin_pack_small"));

// ... and on teardown:
shop.Dispose();   // deregisters the EventBus binding
```

## In-App Purchases (Unity Gaming Services)

Real money is asynchronous, so RealMoney items take a separate path: the processor implements
`IAsyncPaymentProcessor`. `ShopService.Purchase` starts the charge via `BeginPay`, returns
`PurchaseOutcome.AsPending`, and grants rewards + publishes `ShopPurchaseSucceededEvent` only when the
store confirms (callback on Unity's main thread). Virtual-currency (Coin/Gem) purchases stay fully
synchronous and unchanged.

Pieces:
- `IIapService` — async seam: `bool IsAvailable(id)` + `void Purchase(id, Action<bool> onComplete)`.
- `StubIapService` — deterministic test/demo IAP (no store, no real charge).
- `UgsIapService` — Unity IAP (UGS) implementation; requires the `com.unity.purchasing` package.
- `RealMoneyPaymentProcessor : IPaymentProcessor, IAsyncPaymentProcessor` — bridges the shop to the IAP
  service. The item id **is** the store product id.

### Setup

1. **Package**: `com.unity.purchasing` is listed in `Packages/manifest.json` (it pulls
   `com.unity.services.core`). Let Unity restore it — `UgsIapService` only compiles afterwards.
2. **Project Settings → Services**: link the project to Unity Gaming Services.
3. **UGS dashboard**: create one product per RealMoney item, with the **product id equal to the item id**
   (`starter_pack`, `value_pack`, `offer_1plus1`, `no_ads`, `coins_150` …). `no_ads` is **Non-Consumable**;
   the rest are **Consumable**.
4. **Enable it**: on the scene's `StoreInstaller`, tick **Use Real Iap** (off = stub). It builds a
   `UgsIapService` from the catalog's RealMoney products automatically.

Manual wiring (without `StoreInstaller`):

```csharp
var iap = new UgsIapService(new[] {
    new UgsProduct("coins_150", consumable: true),
    new UgsProduct("no_ads",    consumable: false),
    // …one per RealMoney item, ids matching the dashboard
});
var shop = new ShopServiceBuilder(catalog).WithWallet(wallet).WithIap(iap).Build();
```

### Restore purchases

Non-consumables (e.g. No Ads) must be restorable — Apple requires a visible button. Add a
`StoreRestoreButton` to a button (it auto-finds the `Button` on the same GameObject); on click it raises
`ShopRestoreRequestedEvent`. The shop asks each `IRestorablePaymentProcessor` to restore:

- **Apple**: `RestoreTransactions` re-delivers owned products.
- **Google / others**: owned non-consumables are re-delivered automatically during initialization.

Each owned product id comes back through `ProcessPurchase`; with no in-flight purchase the IAP service
raises `PurchaseRestored`, the shop re-grants it via `Fulfill` (records ownership + publishes
`ShopPurchaseSucceededEvent`, so the No-Ads view flips to *owned*), and on completion publishes
`ShopPurchasesRestoredEvent(success)`. You can also call `IShopService.RestorePurchases(onComplete)` directly.

The stub (`StubIapService`) reports restore success but restores nothing — fine for editor-flow testing.

> Compatibility: `UgsIapService` targets Unity IAP 5.x — the `UnityIAPServices.StoreController()` API
> (`Connect` / `FetchProducts` / `PurchaseProduct` / `ConfirmPurchase` + order events). In the editor,
> purchases hit Unity's fake store; real-store testing needs a device + sandbox account.

## Extending without touching the core

| Need | Do this |
|---|---|
| New reward kind (lives, skins…) | New `[Serializable] IShopReward` + new `IRewardGranter`; register the granter |
| New currency | Add to `CurrencyType`; register an `IPaymentProcessor` for it |
| New purchase rule | New `IPurchaseValidator` (or a `DelegateRequirementValidator`); add it to the chain |
| Persisted history | Implement `IPurchaseHistory` over DataManager; pass via `WithHistory(...)` |
| Real IAP | Use `UgsIapService` (Unity IAP), or implement `IIapService` yourself; pass via `WithIap(...)` — see *In-App Purchases* |

## UI layer (`GUI/`)

The UI mirrors the framework conventions: `StorePanel` extends `GameBrain.Casual.UIPanel`, popups extend
`GameBrain.Casual.UIPopup`, item views are plain MonoBehaviours like the legacy `ShopItemView`. UI files
reference Casual types through a `using Casual = GameBrain.Casual;` **namespace alias** so this system's
`CurrencyType` never clashes with the legacy one. Views are fully decoupled from the engine: a tap raises
`ShopPurchaseRequestedEvent`; views/panels react to `ShopPurchaseSucceededEvent` / `ShopPurchaseFailedEvent`.

| Component | Role |
|---|---|
| `StorePanel : UIPanel` | Builds views from a `ShopCatalogDefinition` into sections by `Layout`; shows the failure popup |
| `StoreCarouselView` | Horizontal **paged** carousel for bundles/starter packs; snaps to the nearest page on drag end |
| `PageIndicator` / `PageIndicatorItem` | Page dots under the carousel; hidden automatically when there is only one page |
| `StoreItemView` | One item: icon, label, price (+currency icon or real-money label), badge, buy button |
| `StoreBundleView : StoreItemView` | Starter-pack/offer card: reward chips + "70% EXTRA" + countdown |
| `StoreRewardEntryView` | One reward chip (icon + "xN") inside a bundle/offer |
| `CountdownView` | Ticking timer display ("2D 23H", "23h 23m"); raises `Expired` |
| `StoreOfferPopup : UIPopup` | "Buy 1, Get 1 Free" two-column offer popup |
| `StoreMessagePopup : UIPopup` | Maps a `PurchaseFailureReason` to text and shows it |
| `StoreRewardIconSet` (SO) | currency/booster → sprite, and `(icon, amount)` extraction from a reward |
| `StoreRestoreButton` | "Restore Purchases" button → raises `ShopRestoreRequestedEvent`; required on iOS |

### Authoring data (per `ShopItemDefinition`)

Presentation-only fields drive the look without touching the domain: `Layout` (CoinTile / Bundle /
Generic / **Hidden**), `Badge` (None / BestValue / Popular / Custom + custom text), `BonusLabel`
("70% EXTRA"), `PriceLabelOverride` ("EUR 2,29" for real-money items), `OfferDurationSeconds` (countdown
length). Bundle/offer reward chips are generated automatically from the item's **rewards** list.

`Layout` routing: **Bundle** items go into the top paged carousel (multiple packs, swipeable, with page
dots); **CoinTile** items into the coin grid; **Generic** into the row list (e.g. No Ads); **Hidden**
items are not shown in the panel but stay in the catalog so they can be purchased from a popup (the 1+1
offer uses this).

### Fastest path: one-click generator

`Editor/StoreUIBuilder.cs` adds menu items that create everything for you through the Unity API:

1. **GameBrain → Store → 1. Build Store UI Assets** — generates, under `Store System/Generated/`, the
   example catalog + item definitions (starter pack, no-ads, six coin packs, 1+1 offer), a
   `StoreRewardIconSet`, and all fully-wired prefabs (chip, coin tile, bundle card, generic row, store
   panel, offer popup, message popup).
2. **GameBrain → Store → 2. Build Demo Scene** — creates `Generated/StoreDemo.unity` with a Canvas, the
   store panel, an `EventSystem`, and a `StoreInstaller` (live in-memory shop). Press Play and buy.

Visuals are plain coloured placeholders (no art shipped) — assign your sprites on the `StoreRewardIcons`
asset and the view prefabs afterwards. Delete the `Generated/` folder to remove all of it.

### Manual prefab wiring (if you prefer to build them yourself)

1. **Reward icons:** create a `StoreRewardIconSet` asset (right-click → *Create → GameBrain → Store →
   Reward Icon Set*), assign coin/gem sprites and booster sprites.
2. **Item prefabs:** build three prefabs and put the matching script on each root:
   - *Coin tile* → `StoreItemView` (icon, label = coin amount, price text + currency icon, buy button, optional badge root/text).
   - *Bundle card* → `StoreBundleView` (base fields + reward-entries container, `StoreRewardEntryView` prefab, bonus label, `CountdownView`).
   - *Generic row* (No Ads) → `StoreItemView`.
   - *Reward chip* → `StoreRewardEntryView` (icon + amount text).
3. **Popups:** `StoreOfferPopup` and `StoreMessagePopup` prefabs — assign the base UIPopup `_closeButton`
   (for the offer popup, wire it to the "No Thanks" button) and `_titleText`, then the subclass fields.
4. **Panel:** put `StorePanel` on the shop panel root; assign the catalog, icon set, the two containers
   (featured + coin grid), the three view prefabs, and the message popup. Toggle it with `SetActive`
   like the legacy `ShopPanel`, or drop it under a navigation bar page.
5. **Make it live:** add `StoreInstaller` to a GameObject in the scene and assign the catalog. It builds a
   `ShopService` (in-memory wallet by default) and keeps it subscribed, so the buttons actually work.

## Verifying

`ShopVerification.RunAll()` builds in-memory shops and asserts side effects for 9 scenarios (success,
insufficient funds, unknown item, purchase cap, ownership, reward dispatch, atomicity, custom
requirement, alternate currency). To run it:

- Add a `ShopDemoBootstrap` component to a GameObject and press **Play** (or right-click the component →
  **Run Shop Verification**). Results print to the Console as `PASS` / `FAIL` with a summary line.

`ShopVerification` is plain C# with no Unity Test Framework dependency, so it can later be wrapped in an
NUnit edit-mode test if the project adopts assembly definitions.

## Notes / not yet done

- **Gem** is fully supported by the system, but `CurrencyManagerWallet` cannot back it until `GameData`
  and `CurrencyManager` gain a Gem balance — the Gem branches there currently warn and refuse.
- **RealMoney / IAP** is implemented via Unity Gaming Services (`UgsIapService`) on an async payment path;
  the in-editor `StubIapService` lets you test the flow without a store. You still need to link the
  project to UGS and configure matching products in the dashboard (see *In-App Purchases*).
- No GUI is included. UI integration is a thin layer: a view raises `ShopPurchaseRequestedEvent` and
  subscribes to `ShopPurchaseSucceededEvent` / `ShopPurchaseFailedEvent`.
