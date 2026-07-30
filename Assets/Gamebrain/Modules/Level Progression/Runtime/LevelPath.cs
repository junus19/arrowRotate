using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    /// <summary>
    /// The main-menu level path: a fixed window of <see cref="LevelPathNode"/>s showing the level the player is
    /// on plus the ones ahead of it — the current node at the BOTTOM, the future climbing upwards.
    ///
    /// It owns one thing: which numbers the nodes show. The connecting line, the circles, the ring and the
    /// "Hard" pill are all art in the prefab; this component never draws or tints anything.
    ///
    /// Nodes are built once and then only re-labelled, so opening the menu allocates nothing. Refreshing on
    /// <see cref="OnEnable"/> is what keeps it in step: the panel is re-activated on every menu visit, which is
    /// exactly when the player's level may have changed.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelPath : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("GameData asset — the source of the player's current (visual) level.")]
        [SerializeField] private GameData _gameData;

        [Tooltip("Game config — read only to tell which upcoming levels are flagged Hard. Optional: without it " +
                 "no node shows the hard tag.")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Nodes")]
        [Tooltip("Container the nodes are parented to. Its layout group decides the spacing. Defaults to this " +
                 "GameObject.")]
        [SerializeField] private Transform _nodesRoot;

        [Tooltip("Node prefab, instantiated Node Count times on the first build.")]
        [SerializeField] private LevelPathNode _nodePrefab;

        [Tooltip("How many levels the path shows at once: the current one plus (count - 1) ahead of it.")]
        [Min(1)]
        [SerializeField] private int _nodeCount = 3;

        [Header("Line")]
        [Tooltip("The connecting line. Its HEIGHT is driven to span the node column, because the column grows " +
                 "with Node Count and would otherwise outrun a hand-sized line. Optional: leave empty to keep " +
                 "the line purely art-driven.")]
        [SerializeField] private RectTransform _line;

        [Tooltip("How far the line reaches past the outermost node centres, in canvas units. Lets it run off " +
                 "the top of the screen like the design does.")]
        [SerializeField] private float _lineOvershoot;

        private readonly List<LevelPathNode> _nodes = new List<LevelPathNode>();
        private bool _built;

        /// <summary>Level the player is on, 1-based exactly as the player sees it.</summary>
        public int CurrentLevel => _gameData != null ? _gameData.GetVisualLevelIndex() : 1;

        public List<LevelPathNode> Nodes => _nodes;

        private void Awake() => Build();

        private void OnEnable() => Refresh();

        private void Build()
        {
            if (_built) return;
            _built = true;

            if (_nodesRoot == null) _nodesRoot = transform;

            // Nodes already sitting under the root (hand-placed in the prefab) are adopted instead of duplicated,
            // so the path can be fully authored by hand and still work.
            _nodesRoot.GetComponentsInChildren(true, _nodes);

            if (_nodePrefab == null)
            {
                if (_nodes.Count == 0)
                    Debug.LogWarning("[LevelPath] No node prefab and no nodes under the root — nothing to show.",
                                     this);
                return;
            }

            while (_nodes.Count < _nodeCount)
                _nodes.Add(Instantiate(_nodePrefab, _nodesRoot));

            // Extra hand-placed nodes are switched off rather than destroyed: the author may be keeping them.
            for (int i = _nodeCount; i < _nodes.Count; i++)
                if (_nodes[i] != null) _nodes[i].gameObject.SetActive(false);
        }

        /// <summary>Re-labels every node from the current player level. Safe to call any time.</summary>
        public void Refresh()
        {
            Build();

            int current = CurrentLevel;

            // The current level sits at the BOTTOM of the path, so the child order runs downwards: the first
            // child is the farthest level. A plain VerticalLayoutGroup then produces the design's layout with no
            // reverse-arrangement flag to remember.
            for (int i = 0; i < _nodes.Count && i < _nodeCount; i++)
            {
                LevelPathNode node = _nodes[i];
                if (node == null) continue;

                int level = current + (_nodeCount - 1 - i);
                if (!node.gameObject.activeSelf) node.gameObject.SetActive(true);
                node.Setup(level, level == current, IsHard(level));
            }

            UpdateLine();
        }

        /// <summary>
        /// Stretches the line from the bottom node's centre to the top node's centre (plus the overshoot).
        ///
        /// The node column is `count * nodeHeight + (count - 1) * spacing` tall, so it grows every time Node Count
        /// changes and quickly outruns the path's own rect — a line sized to that rect then stops halfway up the
        /// column. Measuring the nodes instead keeps the two in step whatever the count, spacing or node size.
        /// </summary>
        private void UpdateLine()
        {
            if (_line == null || _nodesRoot == null) return;
            if (!(_line.parent is RectTransform lineParent)) return;

            // The layout group only repositions at the end of the frame, so the nodes' current positions would be
            // one refresh stale without this.
            if (_nodesRoot is RectTransform nodesRect) LayoutRebuilder.ForceRebuildLayoutImmediate(nodesRect);

            RectTransform top = null;
            RectTransform bottom = null;
            for (int i = 0; i < _nodes.Count && i < _nodeCount; i++)
            {
                LevelPathNode node = _nodes[i];
                if (node == null || !node.gameObject.activeSelf) continue;

                RectTransform rect = (RectTransform)node.transform;
                if (top == null) top = rect; // child order runs downwards, so the first one is the top node
                bottom = rect;
            }
            if (top == null) return;

            // World space, so it does not matter how the line and the nodes are nested.
            float topY = lineParent.InverseTransformPoint(top.position).y;
            float bottomY = lineParent.InverseTransformPoint(bottom.position).y;

            _line.anchorMin = new Vector2(0.5f, 0.5f);
            _line.anchorMax = new Vector2(0.5f, 0.5f);
            _line.pivot = new Vector2(0.5f, 0.5f);
            _line.sizeDelta = new Vector2(_line.sizeDelta.x, Mathf.Abs(topY - bottomY) + _lineOvershoot * 2f);
            _line.anchoredPosition = new Vector2(_line.anchoredPosition.x, (topY + bottomY) * 0.5f);
        }

        /// <summary>
        /// Is the level at this 1-based number flagged Hard? Levels beyond the configured list (the random-level
        /// loop) simply report false rather than guessing.
        /// </summary>
        public bool IsHard(int level)
        {
            if (_gameConfig == null) return false;

            LevelData[] levels = _gameConfig.Levels;
            int index = level - 1;
            if (levels == null || index < 0 || index >= levels.Length) return false;

            LevelData data = levels[index];
            return data != null && data.Difficulty is LevelDifficulty.Hard;
        }

#if UNITY_EDITOR
        // Lets the designer see a realistic path without entering play mode.
        [ContextMenu("Preview In Editor")]
        private void PreviewInEditor()
        {
            _built = false;
            _nodes.Clear();
            Refresh();
        }
#endif
    }
}
