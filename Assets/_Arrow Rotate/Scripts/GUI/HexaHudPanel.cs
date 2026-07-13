using System.Collections.Generic;
using ArrowRotate.Integration;
using ArrowRotate.View;
using GameBrain.Casual;
using GameBrain.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowRotate.UI
{
    /// <summary>
    /// Oyun HUD'u (SKILL.md §8): ⏱ süre · ok başına renk çipi (✓ çıktı, ❄+kalan buzlu) · hamle.
    /// GUI sahnesinde yaşar, yalnızca EventBus'a abonedir — gameplay'e doğrudan referans yok.
    /// Süre oyuncunun İLK gerçek döndürmesiyle başlar, kazanınca durur; format m:ss.
    /// </summary>
    public class HexaHudPanel : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _timerText;
        private TextMeshProUGUI _movesText;
        private RectTransform _chipRow;

        private readonly Dictionary<int, Chip> _chips = new Dictionary<int, Chip>();
        private bool _timerRunning;
        private float _timerStart;
        private float _finalTime;
        private bool _finished;

        private EventBinding<HexaLevelStartedEvent> _startedBinding;
        private EventBinding<HexaRotateEvent> _rotateBinding;
        private EventBinding<HexaArrowExitedEvent> _exitedBinding;
        private EventBinding<HexaIceBrokenEvent> _iceBinding;
        private EventBinding<HexaLevelWonEvent> _wonBinding;
        private EventBinding<MainMenuRequestedEvent> _menuBinding;

        private class Chip
        {
            public Image Bg;
            public TextMeshProUGUI Label;
            public int FreezeAt;
        }

        private void Awake()
        {
            BuildUi();
            _root.SetActive(false);

            _startedBinding = new EventBinding<HexaLevelStartedEvent>(OnLevelStarted);
            _rotateBinding = new EventBinding<HexaRotateEvent>(OnRotate);
            _exitedBinding = new EventBinding<HexaArrowExitedEvent>(OnArrowExited);
            _iceBinding = new EventBinding<HexaIceBrokenEvent>(OnIceBroken);
            _wonBinding = new EventBinding<HexaLevelWonEvent>(OnWon);
            _menuBinding = new EventBinding<MainMenuRequestedEvent>(OnMainMenu);
        }

        private void OnEnable()
        {
            EventBus<HexaLevelStartedEvent>.Register(_startedBinding);
            EventBus<HexaRotateEvent>.Register(_rotateBinding);
            EventBus<HexaArrowExitedEvent>.Register(_exitedBinding);
            EventBus<HexaIceBrokenEvent>.Register(_iceBinding);
            EventBus<HexaLevelWonEvent>.Register(_wonBinding);
            EventBus<MainMenuRequestedEvent>.Register(_menuBinding);
        }

        private void OnDisable()
        {
            EventBus<HexaLevelStartedEvent>.Deregister(_startedBinding);
            EventBus<HexaRotateEvent>.Deregister(_rotateBinding);
            EventBus<HexaArrowExitedEvent>.Deregister(_exitedBinding);
            EventBus<HexaIceBrokenEvent>.Deregister(_iceBinding);
            EventBus<HexaLevelWonEvent>.Deregister(_wonBinding);
            EventBus<MainMenuRequestedEvent>.Deregister(_menuBinding);
        }

        private void Update()
        {
            if (!_timerRunning || _finished) return;
            _timerText.text = Format(Time.time - _timerStart);
        }

        private static string Format(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        // ── event handlers ─────────────────────────────────────────────────────
        private void OnLevelStarted(HexaLevelStartedEvent e)
        {
            _root.SetActive(true);
            _finished = false;
            _timerRunning = false;
            _timerText.text = "0:00";
            _movesText.text = "0";
            BuildChips(e.Arrows);
        }

        private void OnRotate(HexaRotateEvent e)
        {
            if (!_timerRunning) { _timerRunning = true; _timerStart = Time.time; }
            _movesText.text = e.MoveCount.ToString();
        }

        private void OnArrowExited(HexaArrowExitedEvent e)
        {
            if (!_chips.TryGetValue(e.ArrowId, out var chip)) return;
            // Not: TMP varsayılan fontunda ✓/❄ glifleri yok — art pass'te ikonlaşacak.
            chip.Label.text = string.Empty;
            var c = chip.Bg.color;
            chip.Bg.color = new Color(c.r, c.g, c.b, 0.35f); // soluk = çıktı
            chip.Bg.rectTransform.localScale = Vector3.one * 0.8f;
        }

        private void OnIceBroken(HexaIceBrokenEvent e)
        {
            if (!_chips.TryGetValue(e.ArrowId, out var chip)) return;
            chip.FreezeAt = 0;
            chip.Label.text = string.Empty;
        }

        private void OnWon(HexaLevelWonEvent e)
        {
            _finished = true;
            _timerText.text = Format(e.ElapsedSeconds);
            _root.SetActive(false);
        }

        private void OnMainMenu() => _root.SetActive(false);

        // ── ui inşası (runtime, prefab'sız) ────────────────────────────────────
        private void BuildUi()
        {
            var canvasGo = new GameObject("Hexa HUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var group = canvasGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var bar = NewRect("Bar", canvasGo.transform);
            bar.anchorMin = new Vector2(0.5f, 1f);
            bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0f, -150f);
            bar.sizeDelta = new Vector2(960f, 100f);
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(1f, 1f, 1f, 0.06f);
            _root = bar.gameObject;

            _timerText = NewText("Timer", bar, "0:00", 44, TextAlignmentOptions.Left);
            var tr = _timerText.rectTransform;
            tr.anchorMin = new Vector2(0f, 0f); tr.anchorMax = new Vector2(0.2f, 1f);
            tr.offsetMin = new Vector2(30f, 0f); tr.offsetMax = Vector2.zero;

            _movesText = NewText("Moves", bar, "0", 44, TextAlignmentOptions.Right);
            var mr = _movesText.rectTransform;
            mr.anchorMin = new Vector2(0.8f, 0f); mr.anchorMax = new Vector2(1f, 1f);
            mr.offsetMin = Vector2.zero; mr.offsetMax = new Vector2(-30f, 0f);

            _chipRow = NewRect("Chips", bar);
            _chipRow.anchorMin = new Vector2(0.2f, 0f);
            _chipRow.anchorMax = new Vector2(0.8f, 1f);
            _chipRow.offsetMin = Vector2.zero;
            _chipRow.offsetMax = Vector2.zero;
            var layout = _chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void BuildChips(HexaArrowChipInfo[] arrows)
        {
            foreach (Transform child in _chipRow) Destroy(child.gameObject);
            _chips.Clear();
            if (arrows == null) return;

            foreach (var info in arrows)
            {
                var rect = NewRect("Chip_" + info.ArrowId, _chipRow);
                rect.sizeDelta = new Vector2(44f, 44f);
                var img = rect.gameObject.AddComponent<Image>();
                img.sprite = UiSprites.Circle;
                img.color = HexaPalette.ForPalette(info.Palette);

                var label = NewText("Label", rect, string.Empty, 26, TextAlignmentOptions.Center);
                var lr = label.rectTransform;
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
                label.fontStyle = FontStyles.Bold;

                if (info.FreezeAt > 0)
                    label.text = info.FreezeAt.ToString(); // kalan eşik (❄ ikonu art pass'te)

                _chips[info.ArrowId] = new Chip { Bg = img, Label = label, FreezeAt = info.FreezeAt };
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var rect = NewRect(name, parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }
    }

    /// <summary>Runtime üretilen UI sprite'ları (asset bağımlılığı yok — build'de güvenli).</summary>
    public static class UiSprites
    {
        private static Sprite _circle;

        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                float r = size * 0.5f - 1f;
                var c = new Vector2(size * 0.5f, size * 0.5f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                        float a = Mathf.Clamp01(r - d + 0.5f); // 1px yumuşak kenar
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _circle;
            }
        }
    }
}
