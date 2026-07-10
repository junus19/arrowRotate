using TMPro;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class NavigationButton : MonoBehaviour
    {
        [Header("Unlocked State")]
        [SerializeField] private GameObject _unlockedState;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private Button _button;
        
        [Header("Locked State")]
        [SerializeField] private GameObject _lockedState;
        [SerializeField] private TMP_Text _requiredLevelText;

        [Header("Selection State")]
        [SerializeField] private Color _normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color _selectedColor = Color.white;

        public event Action<int> OnSelect; 
        
        private void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            OnSelect?.Invoke(transform.GetSiblingIndex());
        }

        public void SetBadge(int count) => _badgeText.text = count.ToString();

        public void SetIcon(Sprite sprite) => _iconImage.sprite = sprite;

        public void SetLabel(string text) => _labelText.text = text;
        
        public void SetSelected(bool selected, bool animate = true)
        {
            _button.image.color = selected ? _selectedColor : _normalColor;
            if (animate)
            {
                _iconImage.rectTransform.DOKill();
                _iconImage.rectTransform.DOAnchorPosY(selected ? 128 : 80, .25f);
                _iconImage.rectTransform.DOSizeDelta(Vector2.one * (selected ? 256 : 160), .25f);
            }
            else
            {
                _iconImage.rectTransform.anchoredPosition = Vector2.up * (selected ? 128 : 80);
                _iconImage.rectTransform.sizeDelta = Vector2.one * (selected ? 256 : 160);
            }
        }

        public void SetLocked(bool status)
        {
            _lockedState.gameObject.SetActive(status);
            _unlockedState.gameObject.SetActive(!status);
        }
        
        public void SetRequiredLevelText(string text)
        {
            _labelText.text = text;
        }
    }
}
