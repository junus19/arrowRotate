# Bottom Nav Bar Module

Main-menu bottom navigation bar for Gamebrain Base projects. **Two components, that's the whole module:**

| Component | Where | Owns |
|---|---|---|
| `NavBar` | bar root | which button is selected |
| `NavBarButton` | one per button | its visuals AND what a tap does (inspector data) |

A new tab needs no code: duplicate a button GameObject, pick its **Mode**, done. Buttons are addressed by
their **child index** — no ids to keep in sync.

```
Bottom Nav Bar/
├── Runtime/
│   ├── NavBar.cs          selection
│   └── NavBarButton.cs    visuals + action + level lock + tap feedback
├── Editor/                NavBarBuilder — generator + "Add Button To Selected Bar"
├── Prefab/                BottomNavBar.prefab — Challenge | Home | Coming Soon
└── README.md
```

Runtime namespace `GameBrain.Casual`, editor tools `GameBrain.Navigation.EditorTools` — same as the rest of
Gamebrain. Compiles into Assembly-CSharp (no asmdefs).

## Dependencies

- **Gamebrain Base**: `UIPanel`, `GameData`, TMPro/UGUI, DOTween (`DG.Tweening`, already in the project).
- **No events at all** — the bar needs nothing subscribed anywhere; feedback is drawn by the button itself.
- **No ad/SDK dependency** — nothing references `GameBrain.SDK.*`, and the bar never moves in response to a
  banner. Its layout is whatever the RectTransforms say.
- No dependency on any game feature: a feature is something a button *opens*, not something the bar knows.

---

## `NavBarButton` — one component per button

**Address** — a button has no id; it *is* its child index (`button.Index`, `NavBar.Select(1)`). Reorder the
row and the indices follow. The button the bar returns to is an explicit reference: `NavBar.Default Button`.

**Action** (`Mode`)
- `SelectOnly` — plain selection, opens nothing (Home). Selecting it closes whatever the previous button opened.
- `OpenPanel` — activates `Panel` while selected; `Close Panel On Deselect` puts it away again.
- `Placeholder` — always renders locked, a tap only shows `Placeholder Feedback`, selection never moves.

**Level lock** — `Unlock At Level` (0 = never locked) + the `GameData` asset. Below that level the button shows
its **locked state** and a tap floats `Unlocks at Level {0}!` above it. Locked buttons deliberately stay
clickable: that is how the message gets out.

**Two state roots** — the locked and unlocked looks are two mutually exclusive containers inside the button:

```
Navbar Button
├── Image · Image (1) · Background · Inline    shared chrome
├── Unlocked      Icon (+ Bullet) · Label      ← active while usable
├── Locked        Icon · Label                 ← active while locked
└── Feedback                                   shared, draws on top
```

The component only decides **which container is live** — it swaps no sprites and tints nothing, so both looks are
art-directed freely (different icon, different background, extra badges…). Wire `Unlocked State` / `Locked State`
to the containers, `Icon`/`Label` to the unlocked ones (the emphasis grows that icon, and that label shows only
while selected), and `Locked Label` to the label that should receive the `Level 20` text. With no locked state
wired, the unlocked label carries the requirement instead.

**Tap feedback (local, no events)** — tapping a locked or placeholder button fades its message in and floats it
upward over `Feedback Duration` (DOTween), then fades out and deactivates. Wire `Feedback Text` to a TMP child
above the button (the prefab and the builder both create one, inactive, at the button's top edge) and tune
`Feedback Rise` / `Feedback Duration`. Tapping again restarts the animation from the bottom; deactivating the
button mid-flight resets it. `button.ShowFeedback("…")` triggers it from code.

**Visuals** — Button, the unlocked icon/label, the locked label and the badge dot (+ optional counter text).
`button.Badge = 1` shows the dot, `0` hides it. `Background` + `Selected Color` / `Unselected Color` are the only
tint the component owns (shipped: white at rest, 0.753 grey when selected); everything else is left to the art.

**Selected emphasis** — the selected button grows as a whole, driven from **one** 0..1 tween so the parts can
never drift apart (not even with an overshooting ease):

| Part | How | Why that way |
|---|---|---|
| Width | `Layout Element.preferredWidth = baseWidth × factor` | the layout group re-splits the row, so neighbours make ROOM instead of being covered; the stretched art (`Background`, `Inline`…) follows the rect for free |
| Height | own `sizeDelta.y = baseHeight + Selected Rise × weight` | the row does not control height, so this axis is ours; it extends **upward** |
| Icon | own `sizeDelta` → `Icon Selected Size`, `anchoredPosition.y` += `Icon Rise × weight` | driven through the RECT, not a scale: the selected size is an exact number (no blurry upscale) and the same weight lifts it, which opens the gap to the label |
| Background | `Background.color = Lerp(Unselected Color, Selected Color, weight)` | the one graphic the component tints; `Color.Lerp` clamps, so an overshooting ease cannot push the colour past either end |
| Extra visual | `Visual.localScale = authoredScale × factor` | optional; leave EMPTY when the icon is rect-driven, or it grows twice |

Only the selected button shows its unlocked **label**; the others are icon-only. A level-locked button reads its
requirement from the locked state instead. A locked button never grows, even if something selects it
programmatically — it stays at the rest size and answers a tap with its floating message.

The icon's **authored** rect is its resting state (the prefab rests at 128×128), so `Icon Selected Size` 160
means "128 at rest, 160 when selected". Deselecting reverses it: the icon shrinks and drops back down while the
label switches off. `Icon Rise` 0 relies purely on the button's height growth.

`factor` interpolates `Rest Scale` → `Selected Scale` (shipped 0.9 → 1.1, so the unselected buttons read as
visibly smaller). `Base Width` 0 means "whatever the layout group hands the button at startup". `Selected Rise`
0 disables the height growth. Multipliers apply to the *authored* scale, so art shipped at a non-1 scale keeps
working.

⚠ Upward growth needs the row's **`Child Alignment` set to one of the Lower ones** (the prefab uses
`LowerCenter`). With an Upper alignment the extra height extends DOWNWARD instead.

To hide a button completely (feature not shipped), deactivate its GameObject — it then also leaves the layout
group and the rest re-centre.

## `NavBar` — the bar

- **Buttons**: `Buttons` is a plain `List<NavBarButton>` — the `NavBarButton` children of `Buttons Root`
  (inactive ones included), collected on Awake when the list is left empty. Context menu: *Collect Buttons From
  Children*.
- **Selection**: `Select(index)`, `Select(button)`, `SelectDefault()`, `GetButton(index)`, `Current`,
  `SelectionChanged`. The outgoing button's panel closes **before** the incoming one opens. A button that locks
  itself while selected hands the selection back to the default.
- **Visibility is not its job**: the bar lives inside the main-menu panel, so it appears and disappears with its
  parent. Every re-activation runs `OnEnable` → repaint + (with `Select Default On Enable`) back to the default
  button, which is exactly the "each menu visit starts at Home" behaviour.
- A panel closed from the outside (its own X button, a back gesture) is noticed and the selection returns to
  the default button.

---

## Integration — step by step

### 1. Parent the prefab under the main-menu panel
`Prefab/BottomNavBar.prefab`, or **GameBrain → Navigation → Build Bottom Nav Bar** for a fresh hierarchy with
placeholder sprites. Because it is a child of the menu panel, it shows and hides with it — there is nothing to
subscribe to and nothing to raise.

### 2. Configure the buttons
Per button: pick the Mode, assign the panel (OpenPanel), set `Unlock At Level` + `GameData` if it is gated,
then art-direct the `Unlocked` / `Locked` containers. Drag the Home button into the bar's `Default Button`.

---

## Gotchas

- Indices are CHILD indices, so reparenting or reordering buttons shifts them — keep any `Select(n)` call sites
  in step with the row. `Default Button` is a reference, so reordering never breaks it.
- A `Default Button` that is not part of this bar is dropped with a warning at startup, and the bar falls back to
  the first usable button.
- Buttons are bound from `NavBar.Awake`, not their own — a deactivated button has no `Awake` yet must be fully
  wired the moment it is switched on again.
- Don't disable a locked button's `Button` component; that would swallow the feedback tap.
- The selected look is size + the background tint (no pill). Anything else the design needs is built in the
  prefab and can be driven from `NavBar.SelectionChanged`.
- Don't hand-override the background colour per instance any more — the component writes it every refresh, so a
  serialized override only misleads the editor view.
- `Child Control Width` must stay ON on the row's layout group, otherwise `preferredWidth` is ignored. With
  `Child Force Expand Width` also on, the leftover space keeps being shared equally, so the width delta lands
  diluted (~2/3 of it on the selected button); turn force-expand OFF for a 1:1 accordion.
- `Child Control Height` must stay OFF — the height growth writes the button's own `sizeDelta.y`, which the
  group would otherwise overwrite every rebuild.
- Growing taller pushes the button OUT of the bar's rect. That is the intended "pops above the bar" look and
  works because nothing masks the bar; add a `Mask`/`RectMask2D` and it gets clipped.
- Keep the icon's anchoring identical on every button; a middle-anchored icon rises half as much as a
  top-anchored one when the button grows, so mixed anchors make the buttons behave differently.
- Never leave a hand-set `localScale` on a button root: the layout group ignores scale, so it silently overlaps
  the neighbours forever and makes the row look uneven. Let the emphasis drive the size.
- The emphasis tween rebuilds the row's layout every frame it runs — fine for a handful of buttons, worth a
  thought for long lists.
- `NavBarButton` captures its default label lazily and never overwrites it with an empty value: the bar can
  drive the button before its `Awake` ran.
- A button with no `Feedback Text` assigned logs a warning instead of silently dropping the message.
- Move files **with their .meta files** to keep GUIDs and prefab references intact.
