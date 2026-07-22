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
    /// GUI sahnesindeki Gameplay Panel prefab'ı içinde yaşar; UI hiyerarşisi ELLE kurulur (Bar > Timer,
    /// Moves, Chips) ve inspector'dan bağlanır — runtime UI ÜRETİMİ YOK. Yalnızca EventBus'a abonedir
    /// (gameplay'e doğrudan referans yok). Süre oyuncunun İLK gerçek döndürmesiyle başlar, kazanınca durur.
    ///
    /// Bağlanacak alanlar: Bar (göster/gizle kökü), Timer/Moves (TMP), Chips (GridLayoutGroup container).
    /// Chip Template opsiyonel: verilirse ok başına klonlanır (stil şablonu); boşsa kod-içi daire çip üretilir.
    /// </summary>
    public class HexaHudPanel : MonoBehaviour
    {
        [Header("Bağlantılar (Gameplay Panel > Hexa HUD Panel > Bar)")]
        [Tooltip("Göster/gizle kökü — genelde Bar. Level başında açılır, kazanınca/menüde kapanır.")]
        [SerializeField] private GameObject _bar;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _movesText;
        [Tooltip("Çiplerin ekleneceği container (GridLayoutGroup'lu Chips objesi).")]
        [SerializeField] private RectTransform _chipsContainer;
        [Tooltip("Opsiyonel çip şablonu — verilirse ok başına klonlanır (Image kök + TMP alt etiket). Boşsa daire çip üretilir.")]
        [SerializeField] private GameObject _chipTemplate;
        [Tooltip("Ok çıkınca çip üstünde beliren onay işareti (Icon_WhiteIcon_check_m).")]
        [SerializeField] private Sprite _checkSprite;

        private readonly Dictionary<int, Chip> _chips = new Dictionary<int, Chip>();
        private bool _timerRunning;
        private float _timerStart;
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
            public Image Check;   // ok çıkınca görünen onay işareti (başta gizli)
            public int FreezeAt;
        }

        private void Awake()
        {
            if (_chipTemplate != null) _chipTemplate.SetActive(false); // şablon görünmez
            ShowBar(false);

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
            if (!_timerRunning || _finished || _timerText == null) return;
            _timerText.text = Format(Time.time - _timerStart);
        }

        private static string Format(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        private void ShowBar(bool on) { if (_bar != null) _bar.SetActive(on); }

        // ── event handlers ─────────────────────────────────────────────────────
        private void OnLevelStarted(HexaLevelStartedEvent e)
        {
            ShowBar(true);
            _finished = false;
            _timerRunning = false;
            if (_timerText != null) _timerText.text = "0:00";
            if (_movesText != null) _movesText.text = "0";
            BuildChips(e.Arrows);
        }

        private void OnRotate(HexaRotateEvent e)
        {
            if (!_timerRunning) { _timerRunning = true; _timerStart = Time.time; }
            if (_movesText != null) _movesText.text = e.MoveCount.ToString();
        }

        private void OnArrowExited(HexaArrowExitedEvent e)
        {
            if (!_chips.TryGetValue(e.ArrowId, out var chip)) return;
            if (chip.Label != null) chip.Label.text = string.Empty;
            // renk üstünde onay işareti belirir (çıktı sinyali); renk tam kalır, çip hafif küçülür
            if (chip.Check != null) chip.Check.enabled = true;
            if (chip.Bg != null) chip.Bg.rectTransform.localScale = Vector3.one * 0.9f;
        }

        private void OnIceBroken(HexaIceBrokenEvent e)
        {
            if (!_chips.TryGetValue(e.ArrowId, out var chip)) return;
            chip.FreezeAt = 0;
            if (chip.Label != null) chip.Label.text = string.Empty;
        }

        private void OnWon(HexaLevelWonEvent e)
        {
            _finished = true;
            if (_timerText != null) _timerText.text = Format(e.ElapsedSeconds);
            ShowBar(false);
        }

        private void OnMainMenu() => ShowBar(false);

        // ── çip inşası (sahne container'ına; şablon varsa klon, yoksa daire) ────
        private void BuildChips(HexaArrowChipInfo[] arrows)
        {
            _chips.Clear();
            if (_chipsContainer == null)
            {
                Debug.LogWarning("[HexaHudPanel] Chips container bağlı değil — çipler çizilemez.");
                return;
            }

            // container'ı temizle (şablon HARİÇ; örnek/eski çipler runtime'da otomatik silinir)
            for (int i = _chipsContainer.childCount - 1; i >= 0; i--)
            {
                var child = _chipsContainer.GetChild(i);
                if (_chipTemplate != null && child == _chipTemplate.transform) continue;
                Destroy(child.gameObject);
            }
            if (arrows == null) return;

            foreach (var info in arrows)
            {
                Image img;
                TextMeshProUGUI label;

                if (_chipTemplate != null)
                {
                    var clone = Instantiate(_chipTemplate, _chipsContainer);
                    clone.name = "Chip_" + info.ArrowId;
                    clone.SetActive(true);
                    img = clone.GetComponent<Image>();
                    label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                else
                {
                    var go = new GameObject("Chip_" + info.ArrowId, typeof(RectTransform));
                    go.transform.SetParent(_chipsContainer, false);
                    img = go.AddComponent<Image>();
                    img.sprite = UiSprites.Circle;
                    var lgo = new GameObject("Label", typeof(RectTransform));
                    lgo.transform.SetParent(go.transform, false);
                    var lr = (RectTransform)lgo.transform;
                    lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                    lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
                    label = lgo.AddComponent<TextMeshProUGUI>();
                    label.alignment = TextAlignmentOptions.Center;
                    label.fontSize = 26;
                    label.fontStyle = FontStyles.Bold;
                    label.color = Color.white;
                }

                if (img != null) img.color = HexaPalette.ForPalette(info.Palette);
                if (label != null) label.text = info.FreezeAt > 0 ? info.FreezeAt.ToString() : string.Empty;

                // onay işareti overlay'i (başta gizli; ok çıkınca enabled). Chip kökünün üstünde tam kaplar.
                Image check = null;
                var host = img != null ? img.rectTransform : (label != null ? label.rectTransform : null);
                if (host != null)
                {
                    var cgo = new GameObject("Check", typeof(RectTransform));
                    cgo.transform.SetParent(host, false);
                    var cr = (RectTransform)cgo.transform;
                    cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
                    cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;
                    cr.SetAsLastSibling();
                    check = cgo.AddComponent<Image>();
                    check.sprite = _checkSprite;
                    check.color = Color.white;
                    check.raycastTarget = false;
                    check.preserveAspect = true;
                    check.enabled = false; // ok çıkınca açılır
                }

                _chips[info.ArrowId] = new Chip { Bg = img, Label = label, Check = check, FreezeAt = info.FreezeAt };
            }
        }
    }

    /// <summary>Runtime üretilen UI sprite'ları (asset bağımlılığı yok — build'de güvenli).
    /// Yalnızca Chip Template atanmadığındaki daire çip fallback'i için kullanılır.</summary>
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
