# Level Progression Module

The main-menu level path: the level the player is on plus the ones ahead of it, stacked on a vertical line with
the current one at the **bottom** and the future climbing upwards. **Two components, that's the whole module:**

| Component | Where | Owns |
|---|---|---|
| `LevelPath` | path root | which numbers the nodes show |
| `LevelPathNode` | one per node | its own look: current vs upcoming state, number, hard tag |

```
Level Progression/
├── Runtime/
│   ├── LevelPath.cs       fixed window of nodes, refreshed on enable
│   └── LevelPathNode.cs   one node: two state roots + number + hard tag
├── Editor/                LevelPathBuilder — "GameBrain → Level Path → Build Level Path"
└── README.md
```

Runtime namespace `GameBrain.Casual`, editor tools `GameBrain.LevelProgression.EditorTools` — same convention as
the Bottom Nav Bar module. Compiles into Assembly-CSharp (no asmdefs).

## Dependencies

- **Gamebrain Base**: `GameData` (current level), `GameConfig` (per-level `Difficulty`), TMPro/UGUI, DOTween
  (`DG.Tweening`, for the current node's pulse).
- **No events, no managers.** The path reads two assets and writes text; nothing subscribes to it and it
  subscribes to nothing.

---

## How it works

`LevelPath` shows a **fixed window**: `Node Count` levels, starting at the player's current level and counting
upwards. With `Node Count = 3` and the player on level 10 it shows 10 (current), 11, 12.

- The **child order runs downwards**: the first child is the farthest level, the last child is the current one. A
  plain `VerticalLayoutGroup` with `Lower Center` alignment then produces the design's layout — there is no
  reverse-arrangement flag to remember.
- Nodes are built **once** (`Awake`) and only re-labelled afterwards, so opening the menu allocates nothing.
- `Refresh()` runs on `OnEnable`, which is exactly when the panel is re-activated after a level — that is what
  keeps the path in step with the player's progress. Call it manually if you change the level while the menu is
  already open.
- **Hand-authored nodes are adopted**: any `LevelPathNode` already under `Nodes Root` is used as-is instead of
  being duplicated, so the whole path can be laid out by hand. `Node Prefab` is only needed to make up the
  difference; extra nodes beyond `Node Count` are switched off, never destroyed.
- **Circles, ring art and the "Hard" pill are pure art.** The component writes exactly three things: the numbers,
  which state root is live, and two geometry values — the line's height (see below) and the ring's pulse scale.
- The **line's height is driven**, because the node column grows with `Node Count`: it is stretched from the bottom
  node's centre to the top node's centre plus `Line Overshoot` (0 = stop at the centres, higher = run off-screen
  like the design). Its width, colour and sprite stay yours. Leave `Line` unassigned to keep it fully art-driven.

`LevelPathNode` follows the nav bar button's pattern: **two mutually exclusive state roots** (`Current State` /
`Upcoming State`), each art-directed independently, plus the number text of each and the `Hard Tag` object. The
component only decides which state is live and what the numbers read.

**Pulse** — while a node is the current level its `Ring` breathes: a looping yoyo scale to `Pulse Scale` over
`Pulse Duration` (DOTween, `InOutSine` by default). The multiplier applies to the ring's *authored* scale, an
already-running pulse is never restarted by a refresh, and the tween is killed and the scale restored when the
node stops being current or is deactivated. It never runs outside play mode — the editor has no player loop
driving DOTween, and a half-applied scale would end up saved in the scene.

The hard tag comes from the config: `GameConfig.Levels[level - 1].Difficulty is Hard`. Levels past the end of
that array (the random-level loop) report `false` rather than guessing, and with no `GameConfig` assigned the tag
simply never shows.

---

## Integration — step by step

### 1. Generate the hierarchy
**GameBrain → Level Path → Build Level Path**. It parents the path under the scene's `HomePanel` (or the selected
RectTransform), wires `GameData` + `GameConfig`, and creates `Node Count` nodes with placeholder art:

```
Level Path            [LevelPath]
├── Line                          ← stretched image behind everything, pure art
└── Nodes             [VerticalLayoutGroup, Lower Center]
    ├── Node                      ← farthest level (top)
    ├── Node
    └── Node (Current)            ← current level (bottom)
```

### 2. Swap the placeholder art
Per node: restyle the `Current` / `Upcoming` containers and the `Hard Tag` pill. Sizes, spacing and the line are
plain RectTransform work; the component reads none of it.

### 3. Save it
Either leave it in the menu prefab, or drag the `Level Path` object out as its own prefab and keep a single
`Node Prefab` reference — both paths work.

---

## Gotchas

- `Node Count` is the window size, not a level count: it always starts at the current level.
- The last child is the current level. If you reorder the nodes by hand, the bottom one is the one that gets the
  ring.
- The row's `VerticalLayoutGroup` needs `Child Control Width/Height` **off** — the nodes carry their own size.
- Right-click the component → **Preview In Editor** to relabel the nodes from the saved level without entering
  play mode.
- The pulse tweens `Ring.localScale`. Don't also animate that transform from elsewhere, and don't hand-set a
  scale on it expecting it to stick — the component restores the authored value when the pulse stops.
- Move files **with their .meta files** to keep GUIDs and scene references intact.
